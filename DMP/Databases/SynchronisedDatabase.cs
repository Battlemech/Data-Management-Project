using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Databases.ValueStorage;
using DMP.Networking;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Objects;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Saves all data which could not be deserialized on a remote set
        /// </summary>
        private readonly Dictionary<string, Tuple<byte[], Type>> _serializedData =
            new Dictionary<string, Tuple<byte[], Type>>();

        public bool IsSynchronised
        {
            get => _isSynchronised;
            set
            {
                _isSynchronised = value;
                
                //enable synchronisation if necessary
                if(!value) return;
                
                //if no client was set, use the default instance
                if (Client == null)
                {
                    if (SynchronisedClient.Instance == null) throw new InvalidOperationException(
                        $"No synchronised Client exists which could manage synchronised database {Id}");
                
                    Client = SynchronisedClient.Instance;
                }

                //if client is not connected: Mark this database for later synchronisation
                if (!Client.IsConnected)
                {
                    Client.OnFailedSynchronise(this);
                    return;
                }
                
                OnConnectionEstablished();
            }
        }
        private bool _isSynchronised;

        protected internal void OnConnectionEstablished()
        {
            //try resolving HostId
            ConfigureSynchronisedPersistence();

            Delegation.DelegateAction((() =>
            {
                lock (_values)
                {
                    //return if there are no values to synchronise
                    if (_values.Count == 0) return;
                    
                    foreach (var vs in _values.Values)
                    {
                        OnOfflineModification(vs.Id, vs.Serialize(out Type type), type);
                    }   
                }
            }));
        }
        
        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value, Type type)
        {
            uint modCount = IncrementModCount(id);
            SetValueRequest request = new SetValueRequest(Id, id, modCount, value, type);

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                //value was synchronised successfully
                if(modCount == reply.ExpectedModCount) return;

                request.ModCount = reply.ExpectedModCount;

                //repeat sending the request once updated value is available locally
                EnqueueFailedRequest(request);
            });
        }

        protected internal void OnRemoteSet(string id, byte[] value, Type type, uint modCount, bool incrementModCount)
        {
            //during synchronisation, multiple setValueMessages will be broadcast. This will filter duplicates
            if(TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount > modCount) return;
            
            lock (_values)
            {
                //update local value
                if (_values.TryGetValue(id, out ValueStorage.ValueStorage storage))
                    //this will invoke callbacks internally
                    storage.UnsafeSet(value, type);
                //or save it for later deserialization
                else
                    _serializedData[id] = new Tuple<byte[], Type>(value, type);
            }
            
            //increment local mod count, if necessary
            if(incrementModCount) UpdateModCount(id, modCount);
            
            //update remotely confirmed mod count
            lock (_confirmedModCount) _confirmedModCount[id] = modCount;
            
            //save persistently, if necessary
            if(_isPersistent) OnSetPersistent(id, value, type);

            //check if any locally attempted set requests exists which wait for the current mod count to be completed
            if (!TryDequeueFailedRequest(id, modCount + 1, out SetValueRequest request)) return;

            //update value and type of request if it resulted from a modify process
            if (request is FailedModifyRequest modifyRequest)
            {
                value = modifyRequest.RepeatModification(value, type, out type);
                incrementModCount = modifyRequest.IncrementModCount;
            }
            else
            {
                //extract value and type from delayed request
                value = request.Value;
                type = request.GetValueType();
                
                //local mod count was already increased, it doesn't need to be increased again
                incrementModCount = false;
            }
            modCount = request.ModCount;
            
            //send previously delayed request to others
            Client.SendMessage(new SetValueMessage(Id, id, value, type, modCount));
            
            //process the sent set value message locally
            OnRemoteSet(id, value, type, modCount, incrementModCount);
        }
    }
}