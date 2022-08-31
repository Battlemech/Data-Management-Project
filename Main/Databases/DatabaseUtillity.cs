using System;
using System.Collections.Generic;
using System.Threading;
using Main.Networking.Synchronisation.Client;
using Main.Networking.Synchronisation.Messages;

namespace Main.Databases
{
    public partial class Database
    {
        private int _onInitializedTracker;
        
        /// <summary>
        /// Invokes an action once a value isn't null or default 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onInitialized"></param>
        /// <typeparam name="T"></typeparam>
        public void OnInitialized<T>(string id, Action<T> onInitialized)
        {
            lock (_values) //prevent modification on retrieved object
            {
                if(TryInvoke(Get<T>(id), onInitialized)) return;
                
                //get thread safe index increment
                string callbackName = $"SYSTEM/INTERNAL/{id}-{Interlocked.Increment(ref _onInitializedTracker)}";
                
                //invoke action once if value is not null or default
                AddCallback<T>(id, (obj =>
                {
                    if(!TryInvoke(obj, onInitialized)) return;

                    RemoveCallbacks(callbackName);
                }), callbackName);
            }
        }

        private bool TryInvoke<T>(T obj, Action<T> onInitialized)
        {
            if (EqualityComparer<T>.Default.Equals(obj, default(T))) return false;
            
            onInitialized.Invoke(obj);

            return true;
        }

        public override string ToString()
        {
            return _isSynchronised ? $"({Client})-{Id}" : $"DB-{Id}";
        }
    }
}