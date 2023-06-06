using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly Dictionary<string, ValueStorage.ValueStorage> _values = new Dictionary<string, ValueStorage.ValueStorage>();

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
            ValueStorage.ValueStorage value;
            
            lock (_values)
            {
                //try retrieving the value
                bool success = _values.TryGetValue(id, out value);

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
                    }

                    //create value storage and add it
                    ValueStorage<T> valueStorage = new ValueStorage<T>(this, id, obj);
                    _values.Add(id, valueStorage);
                
                    return valueStorage;
                }
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