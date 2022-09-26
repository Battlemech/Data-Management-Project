using System;
using System.Collections.Generic;
using System.Threading;

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
            Get<T>(id).BlockingGet((value) =>
            {
                if(TryInvoke(value, onInitialized)) return;
                
                //get thread safe index increment
                string callbackName = $"SYSTEM/INTERNAL/{id}-{Interlocked.Increment(ref _onInitializedTracker)}";
                
                //invoke action once if value is not null or default
                AddCallback<T>(id, (obj =>
                {
                    if(!TryInvoke(obj, onInitialized)) return;

                    RemoveCallbacks(id, callbackName);
                }), callbackName); 
            });
        }

        private bool TryInvoke<T>(T obj, Action<T> onInitialized)
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