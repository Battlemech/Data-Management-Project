using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DMP.Utility;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        private readonly ConcurrentDictionary<string, List<Callback>> _callbacks = new ConcurrentDictionary<string, List<Callback>>();

        /// <summary>
        /// Adds a callback to an Id. Whenever the value with the specified id changes, the callback is invoked
        /// </summary>
        /// <param name="onValueChange">The action to perform if the value is updated</param>
        /// <param name="name">Name of the callback</param>
        /// <param name="invokeCallback">True if the callback shall be invoked instantly. Unique callbacks are not invoked if a duplicate callback existed</param>
        /// <param name="unique">True if the callback shall only be added if no other callback with the same name exists for the same id</param>
        /// <param name="removeOnError">True if the callback should be removed once an error occurs</param>
        /// <typeparam name="T">Expected type of the tracked value</typeparam>
        /// <returns>True if the callback was added. False if unique=true and another callback with the same name existed</returns>
        public bool AddCallback(Action<T> onValueChange, string name = "", bool invokeCallback = false,
            bool unique = false, bool removeOnError = false)
        {
            //try creating list
            if (!_callbacks.TryGetValue(name, out List<Callback> callbacks))
            {
                callbacks = new List<Callback>();
                
                //if add failed: Try calling function again, accessing list created by another thread
                if (!_callbacks.TryAdd(name, callbacks))
                    return AddCallback(onValueChange, name, invokeCallback, unique, removeOnError);
                
            } else if (unique) return false;
                
            callbacks.Add(new Callback(onValueChange, removeOnError));
            
            //invoke callback if desired
            if (invokeCallback) BlockingGet(onValueChange.Invoke);

            return true;
        }

        public override int GetCallbackCount(string name = "")
        {
            return _callbacks.TryGetValue(name, out List<Callback> callbacks) ? callbacks.Count : 0;
        }

        public override int RemoveCallbacks(string name = "")
        {
            return _callbacks.TryRemove(name, out List<Callback> callbacks) ? callbacks.Count : 0;
        }

        public override int InvokeAllCallbacks() => BlockingGet(InvokeAllCallbacks);

        public override int InvokeAllCallbacks(byte[] value) => InvokeAllCallbacks(Serialization.Deserialize<T>(value));
        
        public int InvokeAllCallbacks(T value)
        {
            int count = 0;
            
            foreach (var callbacks in _callbacks.Values)
            {
                //copy list to allow removing during iteration?
                foreach (var callback in new List<Callback>(callbacks))
                {
                    if (!callback.Invoke(value)) callbacks.Remove(callback);
                }
                count += callbacks.Count;
            }
            return count;
        }
        
        private struct Callback
        {
            private readonly Action<T> _action;
            public readonly bool RemoveOnError;

            public Callback(Action<T> action, bool removeOnError)
            {
                _action = action;
                RemoveOnError = removeOnError;
            }

            public bool Invoke(T value)
            {
                try
                {
                    _action.Invoke(value);
                }
                catch (Exception e)
                {
                    if(RemoveOnError) LogWriter.Log($"Removing callback because it caused an exception.\nException: " + e);
                    else LogWriter.LogException(e);
                    return false;
                }

                return true;
            }
        }
    }
}