using System;
using System.Collections.Generic;
using DMP.Utility;

namespace DMP.Networking.Messaging.Server
{
    public partial class MessageServer
    {
        private readonly Dictionary<string, List<ServerCallback>> _callbacks =
            new Dictionary<string, List<ServerCallback>>();
        
        /// <summary>
        /// Add a function to be executed when a message of a certain type was received asynchronously
        /// </summary>
        /// <remarks>The servers callbacks are not thread-save per default!</remarks>
        public void AddCallback<T>(Action<T, MessageSession> onValueChange, string name = "") where T : Message
        {
            ServerCallback<T> callback = new ServerCallback<T>(name, onValueChange);
            string id = typeof(T).FullName;
            
            lock (_callbacks)
            {
                bool success = _callbacks.TryGetValue(id, out List<ServerCallback> savedCallbacks);

                if (!success)
                {
                    savedCallbacks = new List<ServerCallback>();
                    _callbacks.Add(id, savedCallbacks);
                }
                
                savedCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// Remove previously added callbacks
        /// </summary>
        /// <returns>The number of callbacks removed</returns>
        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            int removedCallbacks = 0;
            string id = typeof(T).FullName;
            
            lock (_callbacks)
            {
                bool success = _callbacks.TryGetValue(id, out List<ServerCallback> callbacks);

                //no callbacks to remove
                if (!success || callbacks.Count == 0) return 0;

                //initialize new list. It will contain the callbacks which are not removed
                List<ServerCallback> callbacksToKeep = new List<ServerCallback>(callbacks.Count);

                foreach (var callback in callbacks)
                {
                    //if callbacks has the desired name: remove it
                    if (callback.Name == name) removedCallbacks++;
                    //if callback doesn't have desired name: keep it
                    else callbacksToKeep.Add(callback);
                }

                //overwrite callback list
                _callbacks[id] = callbacksToKeep;
            }
            
            //return number of removed callbacks
            return removedCallbacks;
        }

        protected internal void InvokeCallbacks(string id, byte[] serializedMessage, MessageSession session)
        {
            List<ServerCallback> callbacks;

            //retrieve callbacks
            lock (_callbacks)
            {
                //try retrieving list of callbacks
                bool success = _callbacks.TryGetValue(id, out callbacks);
                
                //no callbacks to invoke
                if(!success || callbacks.Count == 0) return;

                //copy callback list to allow modification
                callbacks = new List<ServerCallback>(callbacks);
            }

            //deserialize object by retrieving type
            object data = Serialization.Deserialize(serializedMessage, callbacks[0].GetCallbackType());
            
            //invoke callbacks
            foreach (var callback in callbacks)
            {
                callback.InvokeCallback(data, session);
            }
        }

        #region Callback definition

        private abstract class ServerCallback
        {
            public readonly string Name;

            protected ServerCallback(string name)
            {
                Name = name;
            }

            public abstract void InvokeCallback(object message, MessageSession session);
            public abstract Type GetCallbackType();
        }

        private class ServerCallback<T> : ServerCallback where T : Message
        {
            private readonly Action<T, MessageSession> _received;
            public ServerCallback(string name, Action<T, MessageSession> received) : base(name)
            {
                _received = received;
            }

            public override void InvokeCallback(object message, MessageSession session)
            {
                if (message is T data)
                {
                    try
                    {
                        _received.Invoke(data, session);
                    }
                    catch (Exception e)
                    {
                        LogWriter.LogException(e);
                    }
                } 
                else throw new ArgumentException($"Expected {typeof(T)}, but got {message.GetType()}");
            }

            public override Type GetCallbackType()
            {
                return typeof(T);
            }
        }

        #endregion
    }
    
    
}