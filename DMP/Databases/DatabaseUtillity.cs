using System;
using System.Collections.Generic;
using System.Threading;
using DMP.Databases.ValueStorage;

namespace DMP.Databases
{
    public partial class Database
    {
        private int _onInitializedTracker;
        
        /// <summary>
        /// Invokes an action exactly one time as soon as the value isn't null or default 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onInitialized"></param>
        /// <typeparam name="T"></typeparam>
        public void OnInitialized<T>(string id, Action<T> onInitialized)
        {
            ValueStorage<T> valueStorage = Get<T>(id);
            
            valueStorage.BlockingGet((value) =>
            {
                if (TryInvoke(value, onInitialized)) return;

                //get thread safe index increment
                string callbackName = $"SYSTEM/INTERNAL/{id}-{Interlocked.Increment(ref _onInitializedTracker)}";
                
                //invoke action once if value is not null or default
                valueStorage.AddCallback((obj =>
                {
                    //remove callback if invocation was successful
                    if (TryInvoke(obj, onInitialized)) valueStorage.RemoveCallbacks(callbackName);
                }), callbackName); 
            });
        }

        private static bool TryInvoke<T>(T obj, Action<T> onInitialized)
        {
            if (IsNullOrDefault(obj)) return false;
            
            onInitialized.Invoke(obj);

            return true;
        }

        private static bool IsNullOrDefault<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }

        public override string ToString()
        {
            return Client != null ? $"({Client})-{Id}" : $"DB-{Id}";
        }
    }
}