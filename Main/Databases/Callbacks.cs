using System;
using Main.Callbacks;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        private readonly CallbackHandler<string> _callbackHandler = new CallbackHandler<string>();

        /// <summary>
        /// Adds a callback to an Id. Whenever the value with the specified id changes, the callback is invoked
        /// </summary>
        /// <param name="id">Id of the monitored value</param>
        /// <param name="onValueChange">The action to perform if the value is updated</param>
        /// <param name="name">Name of the callback</param>
        /// <param name="invokeCallback">True if all callbacks for the same id shall be invoked instantly</param>
        /// <param name="unique">True if the callback shall only be added if no other callback with the same name exists for the same id</param>
        /// <typeparam name="T">Expected type of the tracked value</typeparam>
        /// <returns>True if the callback was added. False if unique=true and another callback with the same name existed</returns>
        public bool AddCallback<T>(string id, Action<T> onValueChange, string name = "", bool invokeCallback = false, bool unique = false)
        {
            bool success = _callbackHandler.AddCallback(id, onValueChange, name, unique);
            
            //only invoke callback if it was added successfully
            if(invokeCallback && success)
                _callbackHandler.InvokeCallbacks(id, Serialization.Serialize(Get<T>(id)));

            return success;
        }

        public int RemoveCallbacks(string id, string name = "") => _callbackHandler.RemoveCallbacks(id, name);
    }

}