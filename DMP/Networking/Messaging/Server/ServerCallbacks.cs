using System;
using DMP.Callbacks;

namespace DMP.Networking.Messaging.Server
{
    public partial class MessageServer
    {
        private readonly CallbackHandler<Type> _callbackHandler = new CallbackHandler<Type>();

        public bool AddCallback<T>(Action<T, MessageSession> onValueChange, string name = "", bool unique = false,
            bool removeOnError = false) where T : Message
        {
            return _callbackHandler.AddCallback(typeof(T), onValueChange, name, unique, removeOnError);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            return _callbackHandler.RemoveCallbacks(typeof(T), name);
        }

        public void InvokeCallbacks(Type type, object message, MessageSession session)
        {
            _callbackHandler.UnsafeInvokeCallbacks(type, message, session);
        }
    }
}