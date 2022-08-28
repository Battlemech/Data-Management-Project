using System;
using System.Threading.Tasks;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public delegate T ModifyValueDelegate<T>(T currentValue);
    
    public partial class Database
    {
        /// <summary>
        /// The modify operation considered previous values during modification of current value.
        /// Necessary for synchronised collections: If multiple adds will be executed at the same time,
        /// the Set() function will overwrite the other values. The Modify() function will keep them during set.
        /// </summary>
        /// <remarks>
        /// This function creates inconsistent values during execution if multiple clients
        /// start modifying the same value at the same time: Each client assumes they may modify the value and
        /// add it locally. After a few ms, the values will be synchronised and in the same order globally.
        /// If you need to avoid inconsistent states, use SafeModify() instead!
        /// </remarks>
        public void Modify<T>(string id, ModifyValueDelegate<T> modify)
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
                
                if(_isSynchronised) OnModifyValueSynchronised(id, serializedBytes, modify);
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            Scheduler.QueueTask(id, internalTask);
        }

        private void OnModifyValueSynchronised<T>(string id, byte[] value, ModifyValueDelegate<T> modify)
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

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                uint expectedModCount = reply.ExpectedModCount;
                
                //modCount was like client expected
                bool success = expectedModCount == modCount;

                Console.WriteLine(success);
                
                //modCount wasn't like client expected, but client updated modCount while waiting for a reply
                if (!success && TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount + 1 >= expectedModCount)
                {
                    //todo: failed to reproduce this corner case randomly. Design test?
                    
                    //repeat operation
                    byte[] serializedBytes;
                    //make sure the value isn't changed while modifying it
                    lock (_values)
                    {
                        T newValue = modify.Invoke(Serialization.Deserialize<T>(GetConfirmedValue(id)));
                        serializedBytes = Serialization.Serialize(newValue);
                    }
                    
                    ExecuteDelayedSet(id, serializedBytes, expectedModCount, false);
                    success = true;
                }
                
                
                //bytes no longer need to be saved for this request
                DecrementPendingCount(id);
                
                if(success) return;
                
                //update queue with expected modification count
                request.ModCount = reply.ExpectedModCount;

                //enqueue the request: It will be processed later
                EnqueueFailedRequest(new FailedModifyRequest<T>(request, modify));
            });
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