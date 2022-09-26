using System;
using System.Threading;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Networking;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases
{
    public delegate void OnSafeModification<T>(T value);
    
    public partial class Database
    {
        /// <summary>
        /// Waits until the server grants access to the value until the operation is executed.
        /// This ensures that the "modify" delegate is executed exactly once.
        /// Slower than Modify() because current modCount is requested from server. Use Modify() if possible
        /// <list type="bullet"> <item>
        /// Warning: Operation can produce inconsistent values if any operation except SafeModify() is used
        /// for the same id while the modification process is ongoing</item> </list> </summary>
        /// <remarks>
        /// The value will not be set locally after this function was called, only once the server allowed the operation.
        /// If you want to make sure that the logic you execute happens after the value was set
        /// include it in the "modify" delegate or use SafeModifySync().
        /// </remarks>
        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify)
        {
            //if client isn't connected: No need to request access
            if (!_isSynchronised || !Client.IsConnected)
            {
                ExecuteModification(id, modify);
                return;
            }

            //serialize bytes to save current value (safe from modification)
            byte[] bytes = Get<T>(id).BlockingGet((Serialization.Serialize));

            //wait for access from server
            uint modCount = GetModCount(id);
            LockValueRequest request = new LockValueRequest
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount
            };

            //start saving bytes which arrive from network in case they are required later
            IncrementPendingCount(id);
            
            bool success = Client.SendRequest<LockValueRequest, LockValueReply>(request, lockReply =>
            {
                if(lockReply == null) throw new TimeoutException($"Received no reply from server within {Options.DefaultTimeout} ms!");

                uint expectedModCount = lockReply.ExpectedModCount;
                    
                //modCount was like client expected
                bool success = modCount == expectedModCount;
                    
                //modCount wasn't like client expected, but client updated modCount while waiting for a reply
                if (!success && TryGetConfirmedModCount(id, out uint confirmedModCount) && confirmedModCount + 1 >= expectedModCount)
                {
                    bytes = GetConfirmedValue(id);
                    success = true;
                }
                    
                //bytes no longer need to be saved for this request
                DecrementPendingCount(id);
                    
                //if request was successful: execute modify now
                if (success)
                {
                    T newValue = modify.Invoke(Serialization.Deserialize<T>(bytes));
                    ExecuteDelayedSet(id, Serialization.Serialize(newValue), expectedModCount, true);
                    return;
                }
                    
                //request failed. Execute modify later
                //update failed get to allow deserialization of later remote set messages
                if(!TryGetType(id)) _failedGets[id] = typeof(T);

                //enqueue failed request
                EnqueueFailedRequest(new FailedModifyRequest<T>(Id, id, lockReply.ExpectedModCount, modify, true));
            });

            if(!success) throw new NotConnectedException();
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

            throw new TimeoutException($"Failed to execute modify operation within {timeout} ms!");
        }

        private void ExecuteModification<T>(string id, ModifyValueDelegate<T> modify)
        {
            byte[] serializedBytes = Get<T>(id).InternalSet(modify);
            
            //process the set if database is synchronised or persistent
            Task internalTask = new Task((() =>
            {
                //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
                //allowing the delegation of callbacks to a task
                _callbackHandler.InvokeCallbacks(id, serializedBytes);
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            Scheduler.QueueTask(id, internalTask);
        }
    }
}