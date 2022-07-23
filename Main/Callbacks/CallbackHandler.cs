using System;
using System.Collections.Generic;
using Main.Utility;

namespace Main.Callbacks
{
    public delegate void ValueChanged<T>(T value);
    
    public class CallbackHandler<TKey>
    {
        private readonly Dictionary<TKey, List<Callback>> _callbacks = new Dictionary<TKey, List<Callback>>();

        public void AddCallback<T>(TKey id, ValueChanged<T> onValueChange, string name = "")
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
                
                savedCallbacks.Add(callback);
            }
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
        
        public void InvokeCallbacks(TKey id, byte[] serializedBytes)
        {
            List<Callback> callbacks;
            
            //retrieve callbacks
            lock (_callbacks)
            {
                //try retrieving list of callbacks
                bool success = _callbacks.TryGetValue(id, out callbacks);
                
                //no callbacks to invoke
                if(!success || callbacks.Count == 0) return;

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
        private readonly ValueChanged<T> _callback;
        public Callback(string name, ValueChanged<T> callback) : base(name)
        {
            _callback = callback;
        }

        public override void InvokeCallback(object o)
        {
            //check type
            if (o is not T data) throw new ArgumentException($"Expected {typeof(T)}, but got {o.GetType()}");
            
            //invoke callback
            _callback.Invoke(data);
        }

        public override Type GetCallbackType()
        {
            return typeof(T);
        }
    }
}