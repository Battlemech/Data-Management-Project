using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Main.Threading;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        public readonly string Id;
        public readonly IdLockedScheduler Scheduler = new IdLockedScheduler();

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

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
                    T obj;
                    //try loading the object from not-deserialized data (occurs if type is missing)
                    if (_isSynchronised && _serializedData.TryGetValue(id, out byte[] serializedData))
                    {
                        object loadedObject = Serialization.Deserialize(serializedData, typeof(T));
                        
                        if (loadedObject is T expectedObject) obj = expectedObject;
                        else throw new ArgumentException($"Loaded object {loadedObject.GetType()}, but expected {typeof(T)}");
                    }
                    else
                    {
                        obj = default;
                        
                        //if it won't be possible to extract the type later
                        if (obj == null)
                        {
                            //keep track of failed get attempts to allow synchronisedDatabase to create objects of requested types
                            _failedGets[id] = typeof(T);
                        }
                    }

                    _values.Add(id, obj);
                    return obj;
                }

                return value switch
                {
                    T result => result,
                    null => default,
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
                _callbackHandler.InvokeCallbacks(id, serializedBytes);
                
                if(_isSynchronised) OnSetSynchronised(id, serializedBytes);
                if(_isPersistent) OnSetPersistent(id, serializedBytes);
            }));
            Scheduler.QueueTask(id, internalTask);
        }

        public int InvokeCallbacks(string id)
        {
            byte[] serializedBytes;
            
            lock (_values)
            {
                if(!_values.ContainsKey(id)) return 0;

                if (!TryGetType(id, out Type type))
                    throw new Exception($"Failed to extract type of {id} while trying to invoke callbacks!" +
                                        $"Try specifying the type in InvokeCallbacks");

                serializedBytes = Serialization.Serialize(type, _values[id]);
            }
            
            return _callbackHandler.InvokeCallbacks(id, serializedBytes);
        }

        public int InvokeCallbacks<T>(string id)
        {
            byte[] serializedBytes;

            lock (_values)
            {
                if (!_values.ContainsKey(id)) return 0;
                serializedBytes = Serialization.Serialize(typeof(T), _values[id]);
            }

            return _callbackHandler.InvokeCallbacks(id, serializedBytes);
        }
    }
}