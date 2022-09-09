using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<string, ValueStorage> _values = new ConcurrentDictionary<string, ValueStorage>();

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

        private ValueStorage<T> Get<T>(string id)
        {
            //try retrieving the value
            bool success = _values.TryGetValue(id, out ValueStorage value);

            //if it wasn't found: Add default value
            if (!success)
            {
                T obj;
                //try loading the object from not-deserialized data (occurs if type is missing)
                if (_serializedData.TryGetValue(id, out byte[] serializedData)) //todo: remove instead?
                {
                    object loadedObject = Serialization.Deserialize(serializedData, typeof(T));

                    if (loadedObject is not T expectedObject)
                        throw new ArgumentException($"Loaded object {loadedObject?.GetType()}, but expected {typeof(T)}");
                    
                    //assign obj
                    obj = expectedObject;
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

                ValueStorage<T> valueStorage = new ValueStorage<T>(this, id, obj);

                //if adding valueStorage object failed: other thread must have added it in the meantime. Try getting it again
                if (!_values.TryAdd(id, valueStorage)) return Get<T>(id);
                
                return valueStorage;
            }

            //return valueStorage if it is of expected type
            if (value is ValueStorage<T> expected) return expected;

            throw new ArgumentException($"Saved value has type {value.GetObject()?.GetType()}, not {typeof(T)}");
        }

        public T GetValue<T>(string id) => Get<T>(id).Get();

        public void SetValue<T>(string id, T value) => Get<T>(id).Set(value);
        
        protected internal void OnSet(string id, byte[] serializedBytes)
        {
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
            if(!_values.ContainsKey(id)) return 0;

            if (!TryGetType(id, out Type type))
                throw new ArgumentException($"Failed to extract type of {id} while trying to invoke callbacks!");

            byte[] serializedBytes = Serialization.Serialize(type, _values[id].GetObject());
            
            return _callbackHandler.InvokeCallbacks(id, serializedBytes);
        }
    }
}