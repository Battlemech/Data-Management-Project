using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
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
        }

        /// <summary>
        /// Invoked when a value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            //todo: request change from server. Change instantly if host
            
            //todo: update syncRequired to false
        }

        protected internal void OnRemoteSet(string id, byte[] value, uint modCount, bool incrementModCount)
        {
            //during synchronisation, multiple setValueMessages will be broadcast. This will filter duplicates
            if(TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount > modCount) return;

            //update local value
            bool success;
            lock (_values)
            {
                //retrieve type from object
                success = TryGetType(id, out Type type);
                
                //save value locally
                if (success) _values[id] = Serialization.Deserialize(value, type);
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

            Console.WriteLine($"{this} dequeued failed request! value: {Serialization.Deserialize<string>(request.Value)}");
            
            //send previously delayed request to server
            Client.SendMessage(new SetValueMessage(request));

            //execute delayed set locally
            OnRemoteSet(id, request.Value, request.ModCount, incrementNext);
        }
    }
}