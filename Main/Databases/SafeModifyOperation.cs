using System;
using System.Threading;
using System.Threading.Tasks;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Waits until the server grants access to the value until the operation is executed.
        /// This ensures that the "modify" delegate is executed exactly once
        /// </summary>
        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
        {
            //if client isn't connected: No need to request access
            if (!_isSynchronised)
            {
                SetValueLocally(id, modify);
                return;
            }
            
            //wait for access from server
            uint modCount = IncrementModCount(id);
            LockValueRequest request = new LockValueRequest
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount
            };

            if (!Client.SendRequest(request, out LockValueReply reply))
                throw new Exception($"Received no reply from server within {Options.DefaultTimeout} ms!");

            bool success = modCount == reply.ExpectedModCount;

            //request was successful. Start modification process
            if (success)
            {
                SetValueLocally(id, modify, modCount);
                return;
            }
                
            //request failed. Wait for it to be invoked
            ManualResetEvent executedModification = new ManualResetEvent(false);
            
            EnqueueFailedRequest(new FailedModifyRequest<T>(Id, id, reply.ExpectedModCount, (value =>
            {
                //signal that modify operation is currently being executed. Waiting thread may continue
                executedModification.Set();

                return modify.Invoke(value);
            })));

            //request was successful
            if(executedModification.WaitOne(timeout)) return;

            throw new Exception($"SafeModify Operation wasn't executed within {timeout} ms!");
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

        private void SetValueLocally<T>(string id, ModifyValueDelegate<T> modify, uint modCount)
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
                
                //notify peers of new value
                Client.SendMessage(new SetValueMessage(Id, id, modCount, serializedBytes));
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            internalTask.Start(Scheduler);
        }
    }
}