using System;
using System.Linq;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Adds a callback to an Id. Whenever the value with the specified id changes, the callback is invoked
        /// </summary>
        /// <param name="id">Id of value</param>
        /// <param name="onValueChange">The action to perform if the value is updated</param>
        /// <param name="name">Name of the callback</param>
        /// <param name="invokeCallback">True if the callback shall be invoked instantly. Unique callbacks are not invoked if a duplicate callback existed</param>
        /// <param name="unique">True if the callback shall only be added if no other callback with the same name exists for the same id</param>
        /// <param name="removeOnError">True if the callback should be removed once an error occurs</param>
        /// <typeparam name="T">Expected type of the tracked value</typeparam>
        /// <returns>True if the callback was added. False if unique=true and another callback with the same name existed</returns>
        public bool AddCallback<T>(string id, Action<T> onValueChange, string name = "", bool invokeCallback = false,
            bool unique = false, bool removeOnError = false)
        {
            return Get<T>(id).AddCallback(onValueChange, name, invokeCallback, unique, removeOnError);
        }

        public int RemoveCallbacks(string id, string name = "")
        {
            return _values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage)
                ? valueStorage.RemoveCallbacks(name)
                : 0;
        }
        
        public int InvokeAllCallbacks(string id)
        {
            return _values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage)
                ? valueStorage.InvokeAllCallbacks()
                : 0;
        }
        
        public int InvokeAllCallbacks(string id, byte[] serializedBytes)
        {
            return _values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage)
                ? valueStorage.InvokeAllCallbacks(serializedBytes)
                : 0;
        }

        public int GetCallbackCount(string id)
        {
            return _values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage)
                ? valueStorage.GetCallbackCount()
                : 0;
        }
    }
}