using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DMP.Databases.ValueStorage;
using DMP.Objects;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        public readonly string Id;
        private readonly ConcurrentDictionary<string, ValueStorage.ValueStorage> _values = new ConcurrentDictionary<string, ValueStorage.ValueStorage>();

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
            
            //enable host persistence if database was created with attributes synchronised and persistent
            if (isSynchronised && isPersistent) 
            {
                //once host is is synchronised
                HostId.OnInitialized((hostId) =>
                {
                    //if client is host
                    if (hostId == Client.Id)
                    {
                        HostPersistence.Set(true);
                    }
                });
            }
        }

        public ValueStorage<T> Get<T>(string id)
        {
            //try retrieving the value
            bool success = _values.TryGetValue(id, out ValueStorage.ValueStorage value);

            //if it wasn't found: Add default value
            if (!success)
            {
                T obj;
                //try loading the object from not-deserialized data (occurs if type is missing)
                if (_serializedData.TryGetValue(id, out Tuple<byte[], Type> serializedData)) 
                {
                    obj = (T) Serialization.Deserialize(serializedData.Item1, serializedData.Item2);
                    
                    //remove serialized data, it is no longer required
                    _serializedData.Remove(id);
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

            throw new ArgumentException($"Saved value has type {value.GetEnclosedType()}, not {typeof(T)}");
        }

        
        public ReadOnlyStorage<T> GetReadOnly<T>(string id) => Get<T>(id);

        public T GetValue<T>(string id) => Get<T>(id).Get();

        public void SetValue<T>(string id, T value) => Get<T>(id).Set(value);
        
        protected internal void OnSet(string id, byte[] serializedBytes, Type type)
        {
            if(_isSynchronised && Client.IsConnected) OnSetSynchronised(id, serializedBytes, type);
            if(_isPersistent) OnSetPersistent(id, serializedBytes, type);
        }
    }
}