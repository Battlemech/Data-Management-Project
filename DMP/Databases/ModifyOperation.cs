using System;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Networking;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases
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
            => Get<T>(id).Modify(modify, onResultConfirmed);

        public void Modify<T>(string id, ModifyValueDelegate<T> modify, out T result)
            => Get<T>(id).Modify(modify, out result);

        public Task<T> ModifyAsync<T>(string id, ModifyValueDelegate<T> modify)
            => Get<T>(id).ModifyAsync(modify);
        
        protected internal void OnModify<T>(string id, byte[] serializedBytes, Type type, ModifyValueDelegate<T> modify, Action<T> onResultConfirmed)
        {
            //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
            //allowing the delegation of callbacks to a task
            if(_isSynchronised && Client.IsConnected) OnModifyValueSynchronised(id, serializedBytes, type, modify, onResultConfirmed);
            else onResultConfirmed?.Invoke((T) Serialization.Deserialize(serializedBytes, type));
            if(_isPersistent) OnSetPersistent(id, serializedBytes, type);
        }

        private void OnModifyValueSynchronised<T>(string id, byte[] value, Type type, ModifyValueDelegate<T> modify, Action<T> onResultConfirmed)
        {
            throw new NotImplementedException();
        }
    }
}