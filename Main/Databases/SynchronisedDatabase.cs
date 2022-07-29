using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Networking.Synchronisation;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Saves all data which could not be deserialized on a remote set
        /// </summary>
        private readonly Dictionary<string, byte[]> _serializedData = new Dictionary<string, byte[]>();
        
        //keeps track of all get attempts which failed to return an object
        private readonly Dictionary<string, Type> _failedGets = new Dictionary<string, Type>();

        public bool IsSynchronised
        {
            get => _isSynchronised;
            set
            {
                //do nothing if database is (not) synchronised already
                if (value == _isSynchronised) return;

                _isSynchronised = value;
                
                //enable synchronisation if necessary
                if(!value) return;
                
                //if no client was set, use the default instance
                if (Client == null)
                {
                    if (SynchronisedClient.Instance == null) throw new Exception(
                        $"No synchronised Client exists which could manage synchronised database {Id}");
                
                    Client = SynchronisedClient.Instance;
                }
                
                //return if there are no values to synchronise
                lock (_values)
                {
                    if(_values.Count == 0) return;
                }
                
                //delegate value synchronisation to new task
                Task synchronisationTask = new Task((() =>
                {
                    lock (_values)
                    {
                        if(_values.Count == 0) return;
                        
                        foreach (var kv in _values)
                        {
                            OnOfflineModification(kv.Key, Serialization.Serialize(kv.Value));
                        }   
                    }
                }));
                synchronisationTask.Start(Scheduler);
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

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                bool success = reply.ExpectedModCount == modCount;
                
                if(success) return;

                //update queue with expected modification count
                request.ModCount = reply.ExpectedModCount;

                //enqueue the request: It will be processed later
                EnqueueFailedRequest(request);
            });
        }

        /// <summary>
        /// Invoked when a value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            //todo: request change from server. Change instantly if host
            
            //todo: update syncRequired to false
        }

        protected internal void OnRemoteSet(string id, byte[] value, uint modCount)
        {
            bool success;
            lock (_values)
            {
                //retrieve type from object
                success = TryGetType(id, out Type type);
                
                if (success)
                {
                    object result = Serialization.Deserialize(value, type);
                    _values[id] = result;
                }
            }
            
            //increase modification count after updating local value
            IncrementModCount(id);
            
            //save data persistently if necessary
            if(_isPersistent) OnSetPersistent(id, value);

            //no callback or value with id exists
            if (!success)
            {
                //save data to be deserialized
                _serializedData[id] = value;
            }
            else
            {
                //invoke callbacks
                //invocation not necessary if no type could be found (success is false): There are no callbacks
                _callbackHandler.InvokeCallbacks(id, value);   
            }

            /*
             * try processing a local delayed modification request.
             * The request saved the modification count it requires.
             * If the current mod count is 4 and the modification request has mod count 5,
             * the network expects it to be executed next
             */
            
            if (!TryDequeueFailedRequest(id, modCount + 1, out SetValueRequest request)) return;
            
            //if the request is a failed modify request:
            if (request is FailedModifyRequest modifyRequest)
            {
                lock (_values)
                {
                    //repeat the operation with the same value
                    request.Value = Serialization.Serialize(modifyRequest.RepeatModification(GetInternal(id)));
                }
            }

            //send previously delayed request to server
            Client.SendMessage(new SetValueMessage(request));
            
            //execute delayed set locally
            OnRemoteSet(id, request.Value, modCount + 1);
        }
    }
}