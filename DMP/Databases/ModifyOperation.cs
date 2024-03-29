﻿using System;
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
            uint modCount = IncrementModCount(id);

            Client.SendRequest<SetValueRequest, SetValueReply>(new SetValueRequest(Id, id, modCount, value, type),
                reply =>
                {
                    //client had the most up-to-date data when request was sent
                    bool success = modCount == reply.ExpectedModCount ||
                                   //client received up-to-date data while waiting for reply
                                   (TryGetConfirmedModCount(id, out uint confirmedCount) &&
                                    confirmedCount + 1 >= reply.ExpectedModCount);

                    //value was synchronised successfully
                    if (success)
                    {
                        onResultConfirmed?.Invoke((T)Serialization.Deserialize(value, type));
                        return;
                    }
                    
                    //modify request failed. Repeating operation once up to date value exists locally
                    EnqueueFailedRequest(new FailedModifyRequest<T>(Id, id, reply.ExpectedModCount, (currentValue =>
                    {
                        //invoke modify delegate
                        T newValue = modify.Invoke(currentValue);
                        
                        //invoke onResultConfirmed, if necessary
                        onResultConfirmed?.Invoke(newValue);

                        //return updated value
                        return newValue;
                    })));
                });
        }
    }
}