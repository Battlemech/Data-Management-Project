using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DMP.Utility;

namespace DMP.Callbacks
{
    public class CallbackHandler<TKey>
    {
        private readonly ConcurrentDictionary<TKey, List<Callback>> _callbacks = new ConcurrentDictionary<TKey, List<Callback>>();

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
            //create callback
            return AddCallback(id, new Callback<T>(name, onValueChange, removeOnError), name, unique);
        }

        public bool AddCallback<T1, T2>(TKey id, Action<T1, T2> onInvoked, string name = "", bool unique = false,
            bool removeOnError = false)
        {
            return AddCallback(id, new Callback<T1, T2>(name, onInvoked, removeOnError), name, unique);
        }

        private bool AddCallback(TKey id, Callback callback, string name, bool unique)
        {
            //get list of existing callbacks for id
            List<Callback> savedCallbacks = GetCallbacks(id);

            //try adding new callback
            lock (savedCallbacks)
            {
                //don't save duplicate callbacks if unique parameter was specified
                if (unique && savedCallbacks.Count > 0 && savedCallbacks.Exists((savedCallback => savedCallback.Name == name)))
                    return false;
                
                savedCallbacks.Add(callback);   
            }

            return true;
        }

        public int RemoveCallbacks(TKey id, string name = "")
        {
            //no callbacks for type exists
            if (!_callbacks.TryGetValue(id, out List<Callback> callbacks)) return 0;

            int removedCallbacks = 0;
            
            lock (callbacks)
            {
                //iterate through copy of list to allow modifying origin
                foreach (var callback in new List<Callback>(callbacks))
                {
                    //callback for type has wrong name
                    if(callback.Name != name) continue;

                    //remove callback from origin
                    callbacks.Remove(callback);
                    removedCallbacks++;
                }    
            }

            return removedCallbacks;
        }

        public int RemoveAllCallbacks()
        {
            int removedCallbacks = 0;
            
            //remove all callbacks for all keys
            foreach (var key in _callbacks.Keys)
            {
                removedCallbacks += _callbacks.TryRemove(key, out List<Callback> callbacks) ? callbacks.Count : 0;
            }

            return removedCallbacks;
        }

        public void UnsafeInvokeCallbacks(TKey id, object obj)
        {
            List<Callback> savedCallbacks = GetCallbacks(id);

            lock (savedCallbacks)
            {
                foreach (var callback in new List<Callback>(savedCallbacks))
                {
                    //remove callback if necessary
                    if (!callback.Invoke(obj)) savedCallbacks.Remove(callback);
                }
            }
        }

        public void UnsafeInvokeCallbacks(TKey id, object one, object two)
        {
            List<Callback> savedCallbacks = GetCallbacks(id);

            //iterate through copy of list to allow modifying it
            foreach (var callback in new List<Callback>(savedCallbacks))
            {
                //remove callback if necessary
                if (!callback.Invoke(one, two)) savedCallbacks.Remove(callback);
            }
        }
        
        public void InvokeCallbacks<T>(TKey id, T value)
        {
            List<Callback> savedCallbacks = GetCallbacks(id);

            //iterate through copy of list to allow modifying it
            foreach (var callback in new List<Callback<T>>(savedCallbacks.Cast<Callback<T>>()))
            {
                //remove callback if necessary
                if (!callback.Invoke(value)) savedCallbacks.Remove(callback);
            }
        }

        public void InvokeCallbacks<T1, T2>(TKey id, T1 one, T2 two)
        {
            List<Callback> savedCallbacks = GetCallbacks(id);

            //iterate through copy of list to allow modifying it
            foreach (var callback in new List<Callback<T1, T2>>(savedCallbacks.Cast<Callback<T1, T2>>()))
            {
                //remove callback if necessary
                if (!callback.Invoke(one, two)) savedCallbacks.Remove(callback);
            }
        }

        public int GetCallbackCount(TKey key, string name = "")
        {
            return !_callbacks.TryGetValue(key, out List<Callback> callbacks) ? 0 : callbacks.Count(callback => callback.Name == name);
        }

        private List<Callback> GetCallbacks(TKey key)
        {
            //if list for key exists: Return it
            if (_callbacks.TryGetValue(key, out List<Callback> callbacks)) return callbacks;

            //key doesn't exist: Try adding it
            callbacks = new List<Callback>();
            if (_callbacks.TryAdd(key, callbacks)) return callbacks;

            //key was added by another thread. Get already added list of callbacks
            return GetCallbacks(key);
        }
    }
}