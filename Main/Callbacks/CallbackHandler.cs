using System;
using System.Collections.Generic;
using System.Linq;
using Main.Utility;

namespace Main.Callbacks
{
    public delegate void ValueChanged<T>(T value);
    
    public class CallbackHandler<TKey>
    {
        private readonly Dictionary<TKey, List<Callback>> _callbacks = new Dictionary<TKey, List<Callback>>();

        /// <summary>
        /// Adds a callback to an id
        /// </summary>
        /// <param name="id">Key of the callback</param>
        /// <param name="onValueChange">Action to perform</param>
        /// <param name="name">Name of the callback</param>
        /// <param name="unique">True if the callback should not be added if another callback with the same name already exists for the same id</param>
        /// <typeparam name="T">Type of the expected value</typeparam>
        /// <returns>True if the callback has been added, otherwise false</returns>
        public bool AddCallback<T>(TKey id, Action<T> onValueChange, string name = "", bool unique = false)
        {
            Callback<T> callback = new Callback<T>(name, onValueChange);
            
            lock (_callbacks)
            {
                bool success = _callbacks.TryGetValue(id, out List<Callback> savedCallbacks);

                if (!success)
                {
                    savedCallbacks = new List<Callback>();
                    _callbacks.Add(id, savedCallbacks);
                }
                
                //don't save duplicate callbacks if unique parameter was specified
                if (unique && savedCallbacks.Count > 0 && savedCallbacks.Exists((savedCallback => savedCallback.Name == name)))
                    return false;
                
                savedCallbacks.Add(callback);
            }

            return true;
        }

        public int RemoveCallbacks(TKey id, string name = "")
        {
            int removedCallbacks = 0;
            
            lock (_callbacks)
            {
                bool success = _callbacks.TryGetValue(id, out List<Callback> callbacks);

                //no callbacks to remove
                if (!success || callbacks.Count == 0) return 0;

                //initialize new list. It will contain the callbacks which are not removed
                List<Callback> callbacksToKeep = new List<Callback>(callbacks.Count);

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
        
        public int InvokeCallbacks(TKey id, byte[] serializedBytes)
        {
            List<Callback> callbacks;
            
            //retrieve callbacks
            lock (_callbacks)
            {
                //try retrieving list of callbacks
                bool success = _callbacks.TryGetValue(id, out callbacks);
                
                //no callbacks to invoke
                if(!success || callbacks.Count == 0) return 0;

                //copy callback list to allow modification
                callbacks = new List<Callback>(callbacks);
            }

            //deserialize object by retrieving type
            object data = Serialization.Deserialize(serializedBytes, callbacks[0].GetCallbackType());

            //invoke callbacks
            foreach (var callback in callbacks)
            {
                callback.InvokeCallback(data);
            }

            return callbacks.Count;
            
        }

        public int InvokeCallbacks<T>(TKey id, T value)
        {
            List<Callback> callbacks;
            
            //retrieve callbacks
            lock (_callbacks)
            {
                //try retrieving list of callbacks
                bool success = _callbacks.TryGetValue(id, out callbacks);
                
                //no callbacks to invoke
                if(!success || callbacks.Count == 0) return 0;

                //copy callback list to allow modification
                callbacks = new List<Callback>(callbacks);
            }

            //invoke callbacks
            foreach (var callback in callbacks.Cast<Callback<T>>())
            {
                callback.InvokeCallback(value);
            }

            return callbacks.Count;
        }
        
        public bool TryGetType(TKey key, out Type type)
        {
            lock (_callbacks)
            {
                bool success = _callbacks.TryGetValue(key, out var callbacks);

                if (!success || callbacks.Count == 0)
                {
                    type = null;
                    return false;
                }

                type = callbacks[0].GetCallbackType();
                return true;
            }
        }
    }
    
    public abstract class Callback
    {
        public readonly string Name;

        protected Callback(string name)
        {
            Name = name;
        }

        public abstract void InvokeCallback(object o);
        public abstract Type GetCallbackType();
    }

    public class Callback<T> : Callback
    {
        private readonly Action<T> _callback;
        public Callback(string name, Action<T> callback) : base(name)
        {
            _callback = callback;
        }

        public void InvokeCallback(T value)
        {
            _callback.Invoke(value);
        }
        
        public override void InvokeCallback(object o)
        {
            switch (o)
            {
                //check type
                case T data:
                    //invoke callback
                    _callback.Invoke(data);
                    return;
                case null:
                    //invoke callback
                    _callback.Invoke(default);
                    return;
                default:
                    throw new ArgumentException($"Expected {typeof(T)}, but got {o?.GetType()}");
            }
        }

        public override Type GetCallbackType()
        {
            return typeof(T);
        }
    }
}