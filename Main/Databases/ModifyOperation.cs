using System;
using System.Threading.Tasks;
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
        /// the Set() function will overwrite the other values. The Modify() function will keep them during set
        /// </summary>
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
            internalTask.Start(Scheduler);
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

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                bool success = reply.ExpectedModCount == modCount;
                
                if(success) return;

                //update queue with expected modification count
                request.ModCount = reply.ExpectedModCount;

                //enqueue the request: It will be processed later
                EnqueueFailedRequest(new FailedModifyRequest<T>(request, modify));
            });
        }
    }
}