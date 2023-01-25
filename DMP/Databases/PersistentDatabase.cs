using System;
using System.Collections.Generic;
using DMP.Databases.VS;
using DMP.Persistence;

namespace DMP.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, byte[]> _serializedData =
            new Dictionary<string, byte[]>();

        public bool IsPersistent
        {
            get => _isPersistent;
            set
            {
                //no need to change value
                if (value == _isPersistent) return;
                
                _isPersistent = value;
                
                if (value) OnPersistenceEnable();
                else OnPersistenceDisable();
            }
        }
        private bool _isPersistent;

        private void OnPersistenceEnable()
        {
            //Create database if necessary
            PersistentData.CreateDatabase(Id);
            
            //no data to load
            if(!PersistentData.TryLoadDatabase(Id, out List<DeSerializedObject> serializedObjects)) return;

            lock (_serializedData)
            {
                foreach (var serializedObject in serializedObjects)
                {
                    //update value if it exists
                    if (_values.TryGetValue(serializedObject.ValueStorageId, out ValueStorage valueStorage))
                    {
                        valueStorage.InternalSet(serializedObject.Buffer);
                    }
                    else
                    {
                        //wait for data to be accessed
                        _serializedData[serializedObject.ValueStorageId] = serializedObject.Buffer;    
                    }

                    if (serializedObject.SyncRequired)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private void OnPersistenceDisable()
        {
            //delete persistent data
            PersistentData.DeleteDatabase(Id);
        }

        /// <summary>
        /// Saves values persistently
        /// </summary>
        public void Save()
        {
            if(!_isPersistent) return;

            lock (_values)
            {
               
                foreach (ValueStorage valueStorage in _values.Values)
                {
                    //todo: set sync required
                    PersistentData.Save(Id, valueStorage.Id, valueStorage.Serialize(), false);
                }
            }
        }
    }
}