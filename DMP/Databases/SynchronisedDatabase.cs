using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Networking;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Saves all data which could not be deserialized on a remote set
        /// </summary>
        private readonly Dictionary<string, byte[]> _serializedData = new Dictionary<string, byte[]>();

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

                //try resolving HostId
                ConfigureSynchronisedPersistence();

                //return if there are no values to synchronise
                if(_values.Count == 0) return;

                Task synchronisationTask = new Task((() =>
                {
                    //return if there are no values to synchronise
                    if (_values.Count == 0) return;

                    foreach (var vs in _values.Values)
                    {
                        vs.BlockingGetObject((o => OnOfflineModification(vs.Id, Serialization.Serialize(vs.GetEnclosedType(), o))));
                    }
                }));
                
                Scheduler.QueueTask(synchronisationTask);
            }
        }
        private bool _isSynchronised;

        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value)
        {
            uint modCount = IncrementModCount(id);
            
            SetValueRequest request = new SetValueRequest()
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount,
                Value = value
            };
            
            //no need to increment pending request count: previous data is simply overwritten by operation

            bool success = Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                uint expectedModCount = reply.ExpectedModCount;
                
                //modCount was like client expected
                if(expectedModCount == modCount) return;

                //modCount wasn't like client expected, but client updated modCount while waiting for a reply
                if (TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount + 1 >= expectedModCount)
                {
                    //todo: this corner-case is difficult to reproduce and almost never appears. Design test?
                    //repeat operation with last confirmed value
                    ExecuteDelayedSet(id, value, expectedModCount, false);
                    return;
                }

                //update queue with expected modification count
                request.ModCount = reply.ExpectedModCount;

                //enqueue the request: It will be processed later
                EnqueueFailedRequest(request);
            });

            if (success) return;

            throw new NotConnectedException();
        }

        protected internal void OnRemoteSet(string id, byte[] value, uint modCount, bool incrementModCount)
        {
            //during synchronisation, multiple setValueMessages will be broadcast. This will filter duplicates
            if(TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount > modCount) return;

            //update local value
            if (_values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage))
            {
                //value exists locally. Update it
                valueStorage.UnsafeSet(Serialization.Deserialize(value, valueStorage.GetEnclosedType()));
            }
            else
            {
                //value doesn't exist locally. Save bytes for later deserialization
                _serializedData[id] = value;

                //if value was created by other thread during modification
                if (_values.TryGetValue(id, out valueStorage))
                {
                    //update newly created value
                    valueStorage.UnsafeSet(Serialization.Deserialize(value, valueStorage.GetEnclosedType()));
                    _serializedData.Remove(id);
                }
            }
            
            //increase modification count after updating local value
            if (incrementModCount) UpdateModCount(id, modCount);
            
            //save current value in case it is required for a pending modification
            if (RepliesPending(id))
            {
                _confirmedValues[id] = value;
            }
            
            //track remotely confirmed mod count. Increment after byte value was saved
            lock (_confirmedModCount) _confirmedModCount[id] = modCount;

            //invoke callbacks
            _callbackHandler.InvokeCallbacks(id, value);
            
            //save data persistently if necessary
            if(_isPersistent) OnSetPersistent(id, value);

            /*
             * try processing a local delayed modification request.
             * The request saved the modification count it requires.
             * If the current mod count is 4 and the modification request has mod count 5,
             * the network expects it to be executed next
             */

            if (!TryDequeueFailedRequest(id, modCount + 1, out SetValueRequest request)) return;

            bool incrementNext = false;
            
            //if the request is a failed modify request:
            if (request is FailedModifyRequest modifyRequest)
            {
                //repeat the operation with the up to date value
                Type type = modifyRequest.GetDelegateType();
                
                //deserialize value again because the locally saved remote value might have been modified in the meantime
                request.Value = Serialization.Serialize(type,modifyRequest.RepeatModification(Serialization.Deserialize(value, type)));
                
                //check if modCount needs to be increased with delayed request
                incrementNext = modifyRequest.IncrementModCount;
            }

            //send previously delayed request to server
            Client.SendMessage(new SetValueMessage(request));

            //execute delayed set locally
            OnRemoteSet(id, request.Value, request.ModCount, incrementNext);
        }
    }
}