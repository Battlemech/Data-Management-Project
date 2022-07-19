using System;
using System.Collections.Generic;
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
            IsPersistent = isPersistent;
            IsSynchronised = isSynchronised;
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
                    _values.Add(id, default);
                    return default;
                }

                if (value is T result)
                {
                    return result;
                }
                
                throw new ArgumentException($"Saved value has type {value.GetType()}, not {typeof(T)}");
            }
        }

        public void Set<T>(string id, T value)
        {
            byte[] serializedBytes;
            
            //set value in dictionary
            lock (_values)
            {
                _values[id] = value;
                
                //create serialized object if necessary
                if (!_isPersistent && !_isSynchronised) return;
                serializedBytes = Serialization.Serialize(value);
            }
            
            //process the set if database is synchronised or persistent
            Task internalTask = new Task((() =>
            {
                if(_isSynchronised) OnSetSynchronised(id, serializedBytes);
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            internalTask.Start(_scheduler);
        }
    }
}