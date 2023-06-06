using System;
using DMP.Callbacks;

namespace DMP.Networking.Messaging.Client
{
    public partial class MessageClient
    {
        private readonly CallbackHandler<Type> _callbackHandler = new CallbackHandler<Type>();

        /// <summary>
        /// Add a function to be executed when a message of a certain type was received asynchronously
        /// </summary>
        public void AddCallback<T>(Action<T> onValueChange, string name = "") where T : Message
        {
            _callbackHandler.AddCallback(typeof(T), onValueChange, name);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            return _callbackHandler.RemoveCallbacks(typeof(T), name);
        }
    }
}