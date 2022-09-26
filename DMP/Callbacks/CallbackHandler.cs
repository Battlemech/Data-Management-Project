using System;
using System.Collections.Generic;
using System.Linq;
using DMP.Utility;

namespace DMP.Callbacks
{
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
        /// <param name="removeOnError">True if the callback should be removed once an error occurs</param>
        /// <typeparam name="T">Type of the expected value</typeparam>
        /// <returns>True if the callback has been added, otherwise false</returns>
        public bool AddCallback<T>(TKey id, Action<T> onValueChange, string name = "", bool unique = false, bool removeOnError = false)
        {
            Callback<T> callback = new Callback<T>(name, onValueChange, removeOnError);
            
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
                //if callback caused an exception and is supposed to be removed:
                if (!callback.InvokeCallback(data) && callback.RemoveOnError)
                {
                    //remove the callback
                    lock (_callbacks)
                    {
                        foreach (Callback savedCallback in _callbacks[id])
                        {
                            if (savedCallback.Equals(callback))
                            {
                                _callbacks[id].Remove(callback);
                                break;
                            }
                        }
                    }
                }
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
                //if callback caused an exception and is supposed to be removed:
                if (!callback.InvokeCallback(value) && callback.RemoveOnError)
                {
                    //remove the callback
                    lock (_callbacks)
                    {
                        foreach (Callback savedCallback in _callbacks[id])
                        {
                            if (savedCallback.Equals(callback))
                            {
                                _callbacks[id].Remove(callback);
                                break;
                            }
                        }
                    }
                }
            }

            return callbacks.Count;
        }

        public int GetCallbackCount(TKey key, string name = "")
        {
            lock (_callbacks)
            {
                return !_callbacks.TryGetValue(key, out List<Callback> callbacks) ? 0 : callbacks.Count(callback => callback.Name == name);
            }
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
        public readonly bool RemoveOnError;

        protected Callback(string name, bool removeOnError)
        {
            Name = name;
            RemoveOnError = removeOnError;
        }

        public abstract bool InvokeCallback(object o);
        public abstract Type GetCallbackType();
    }

    public class Callback<T> : Callback
    {
        private readonly Action<T> _callback;
        public Callback(string name, Action<T> callback, bool removeOnError) : base(name, removeOnError)
        {
            _callback = callback;
        }

        public bool InvokeCallback(T value)
        {
            try
            {
                _callback.Invoke(value);
            }
            catch (Exception e)
            {
                if(RemoveOnError) LogWriter.Log($"Removing callback {Name} because it caused an exception.\nException: " + e);
                else LogWriter.LogException(e);
                return false;
            }

            return true;
        }
        
        public override bool InvokeCallback(object o)
        {
            switch (o)
            {
                //check type
                case T data:
                    //invoke callback
                    return InvokeCallback(data);
                case null:
                    //invoke callback
                    return InvokeCallback(default);
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