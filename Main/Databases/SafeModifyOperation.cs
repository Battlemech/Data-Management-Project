using System;
using System.Threading;
using System.Threading.Tasks;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public delegate void OnSafeModification<T>(T value);
    
    public partial class Database
    {
        /// <summary>
        /// Waits until the server grants access to the value until the operation is executed.
        /// This ensures that the "modify" delegate is executed exactly once.
        /// Slower than Modify() because current modCount is requested from server. Use Modify() if possible
        /// </summary>
        /// <remarks>
        /// The value might not be set locally after this operation was executed. If you want to make sure that
        /// the logic you execute happens after the value was set include it in the "modify" delegate or use
        /// SafeModifySync()
        /// </remarks>
        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify)
        {
            //if client isn't connected: No need to request access
            if (!_isSynchronised)
            {
                SetValueLocally(id, modify);
                return;
            }

            //serialize bytes to save current value (safe from modification)
            byte[] bytes = Serialization.Serialize(Get<T>(id));

            //wait for access from server
            uint modCount = GetModCount(id);
            LockValueRequest request = new LockValueRequest
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount
            };

            Client.SendRequest<LockValueRequest, LockValueReply>(request, lockReply =>
            {
                if(lockReply == null) throw new Exception($"Received no reply from server within {Options.DefaultTimeout} ms!");
                
                bool success = modCount == lockReply.ExpectedModCount;

                //request was successful. Start modification process
                if (success)
                {
                    Console.WriteLine("Executing now!");
                    SetValueLocally(id, Serialization.Deserialize<T>(bytes), modify, modCount);
                    
                    //increment mod count after modify operation is complete
                    IncrementModCount(id);
                    return;
                }

                //update failed get to allow deserialization of later remote set messages
                if(!TryGetType(id)) _failedGets[id] = typeof(T);

                Console.WriteLine($"Delaying execution: {modCount}=>{lockReply.ExpectedModCount} !");
                
                //enqueue failed request
                EnqueueFailedRequest(new FailedModifyRequest<T>(Id, id, lockReply.ExpectedModCount, modify, true));
            });
        }

        public T SafeModifySync<T>(string id, ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
        {
            ManualResetEvent modificationExecuted = new ManualResetEvent(false);
            T current = default;
            
            SafeModify<T>(id, (value =>
            {
                //invoke modification operation
                current = modify.Invoke(value);
                
                //signal waiting thread that modification commenced
                modificationExecuted.Set();

                return current;
            }));
            
            if(modificationExecuted.WaitOne(timeout)) return current;

            throw new Exception($"Failed to execute modify operation within {timeout} ms!");
        }

        private void SetValueLocally<T>(string id, ModifyValueDelegate<T> modify)
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
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            internalTask.Start(Scheduler);
        }

        private void SetValueLocally<T>(string id, T current, ModifyValueDelegate<T> modify, uint modCount)
        {
            byte[] serializedBytes;

            //set value in dictionary
            lock (_values)
            {
                T value = modify.Invoke(current);
                _values[id] = value;
                serializedBytes = Serialization.Serialize(value);
            }
            
            //process the set if database is synchronised or persistent
            Task internalTask = new Task((() =>
            {
                //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
                //allowing the delegation of callbacks to a task
                _callbackHandler.InvokeCallbacks(id, serializedBytes);
                
                //notify peers of new value
                Client.SendMessage(new SetValueMessage(Id, id, modCount, serializedBytes));

                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            internalTask.Start(Scheduler);
        }
    }
}