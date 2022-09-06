using System;
using System.Threading;
using System.Threading.Tasks;
using Main.Networking;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public delegate T ModifyValueDelegate<T>(T currentValue);
    
    public partial class Database
    {
        
        /// <summary>
        /// The modify operation considers current value during modification action.
        /// Necessary for synchronised collections: If multiple adds will be executed at the same time,
        /// the Set() function will overwrite the other values. The Modify() function will keep them during set.
        /// </summary>
        /// <remarks>
        /// This function creates inconsistent values during execution if multiple clients
        /// start modifying the same value at the same time. If you need to avoid inconsistent states
        /// or want to make sure ModifyValueDelegate() is executed exactly once, use SafeModify() instead!
        /// </remarks>
        /// <param name="id">Id of value being modified</param>
        /// <param name="modify"> Modification of value. Can be executed a second time if client falsely assumes
        /// to be up to date</param>
        /// <param name="onResultConfirmed">Delegate called once the current value was confirmed by server</param>
        /// <typeparam name="T">Type of value being modified</typeparam>
        public void Modify<T>(string id, ModifyValueDelegate<T> modify, Action<T> onResultConfirmed = null)
        {
            byte[] serializedBytes;

            //set value in dictionary
            lock (_values)
            {
                T value = modify.Invoke(Get<T>(id));
                _values[id] = value;
                serializedBytes = Serialization.Serialize(value);
            }
            
            //process the set if database is synchronised or persistent
            Task internalTask = new Task((() =>
            {
                //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
                //allowing the delegation of callbacks to a task
                _callbackHandler.InvokeCallbacks(id, serializedBytes);
                
                if(_isSynchronised) OnModifyValueSynchronised(id, serializedBytes, modify, onResultConfirmed);
                else onResultConfirmed?.Invoke(Serialization.Deserialize<T>(serializedBytes));
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            Scheduler.QueueTask(id, internalTask);
        }

        public void Modify<T>(string id, ModifyValueDelegate<T> modify, out T result)
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            T value = default;
            
            //start modification action
            Modify<T>(id, modify, valueConfirmed =>
            {
                //save result locally
                value = valueConfirmed;
                
                //signal waiting thread to continue
                resetEvent.Set();
            });

            if (!resetEvent.WaitOne(Options.DefaultTimeout))
                throw new TimeoutException($"Failed to modify {id} within {Options.DefaultTimeout} ms!");

            //assign value resulting from modification to out parameter
            result = value;
        }

        private void OnModifyValueSynchronised<T>(string id, byte[] value, ModifyValueDelegate<T> modify, Action<T> onResultConfirmed)
        {
            uint modCount = IncrementModCount(id);

            SetValueRequest request = new SetValueRequest()
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount,
                Value = value
            };

            //start saving bytes which arrive from network in case they are required later
            IncrementPendingCount(id);

            bool success = Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                uint expectedModCount = reply.ExpectedModCount;
                
                //modCount was like client expected
                bool success = expectedModCount == modCount;

                //result got confirmed by remote. Invoke delegate
                if (success) onResultConfirmed?.Invoke(Serialization.Deserialize<T>(value));
                //modCount wasn't like client expected, but client updated modCount while waiting for a reply
                else if (TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount + 1 >= expectedModCount)
                {
                    //todo: failed to reproduce this corner case randomly. Design test?
                    
                    //repeat operation
                    //make sure the value isn't changed while modifying it //todo: why is this necessary?
                    lock (_values)
                    {
                        T newValue = modify.Invoke(Serialization.Deserialize<T>(GetConfirmedValue(id)));
                        
                        //result got confirmed by remote. Invoke delegate
                        onResultConfirmed?.Invoke(newValue);
                        
                        value = Serialization.Serialize(newValue);
                    }
                    
                    ExecuteDelayedSet(id, value, expectedModCount, false);
                    success = true;
                }
                
                
                //bytes no longer need to be saved for this request
                DecrementPendingCount(id);

                if (success) return;
                
                //update queue with expected modification count
                request.ModCount = reply.ExpectedModCount;

                //enqueue the request: It will be processed later
                EnqueueFailedRequest(new FailedModifyRequest<T>(request, (currentValue =>
                {
                    //invoke modify delegate, generating new value
                    T newValue = modify.Invoke(currentValue);
                    
                    //call onResultConfirmed if necessary
                    onResultConfirmed?.Invoke(newValue);

                    //return newValue to Callee (OnRemoteSet())
                    return newValue;
                })));
            });

            if (success) return;

            throw new NotConnectedException();
        }

        private void ExecuteDelayedSet(string id, byte[] serializedBytes, uint modCount, bool incrementModCount)
        {
            //notify peers of new value
            Client.SendMessage(new SetValueMessage(Id, id, modCount, serializedBytes));

            //update values locally
            OnRemoteSet(id, serializedBytes, modCount, incrementModCount);
        }
    }
}