using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Main.Threading;

namespace Main.Databases
{
    public partial class Database
    {
        public readonly string Id;

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly QueuedScheduler _scheduler = new QueuedScheduler();

        public Database(string id, bool isPersistent = false, bool isSynchronised = false)
        {
            Id = id;
            
            /*
             * Setting these properties will enable/disable synchronisation and persistence.
             * Potentially enabling synchronisation first allows send serialized data(bytes) directly,
             * avoiding additional (de)serialization.
             *
             * Performance is barely affected since synchronisation is delegated to Tasks
             */
            
            IsSynchronised = isSynchronised;
            IsPersistent = isPersistent;
        }

        public T Get<T>(string id)
        {
            lock (_values)
            {
                //try retrieving the value
                bool success = _values.TryGetValue(id, out object value);

                //if it wasn't found: Add default value
                if (!success)
                {
                    T defaultObject = default;
                    _values.Add(id, defaultObject);
                    return defaultObject;
                }

                return value switch
                {
                    T result => result,
                    _ => throw new ArgumentException($"Saved value has type {value.GetType()}, not {typeof(T)}")
                };
            }
        }

        public void Set<T>(string id, T value)
        {
            byte[] serializedBytes;
            
            //set value in dictionary
            lock (_values)
            {
                _values[id] = value;
                serializedBytes = Serialization.Serialize(value);
            }
            
            //process the set if database is synchronised or persistent
            Task internalTask = new Task((() =>
            {
                //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
                //allowing the delegation of callbacks to a task
                InvokeCallbacks(id, serializedBytes);
                
                if(_isSynchronised) OnSetSynchronised(id, serializedBytes);
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            internalTask.Start(_scheduler);
        }
    }
}