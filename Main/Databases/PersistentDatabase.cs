using System;
using System.Collections.Generic;
using System.Linq;
using Data_Management_Project.Databases.Base;

namespace Main.Databases
{
    public partial class Database
    {
        public bool IsPersistent
        {
            get => _isPersistent;
            set
            {
                //value doesn't need to be adjusted
                if (value == _isPersistent) return;
                
                //update value
                _isPersistent = value;
                
                //delete database if persistence is no longer required 
                if (!value)
                {
                    PersistentData.DeleteDatabase(Id);
                    return;
                }

                //see if database exists
                bool databaseExists = PersistentData.TryLoadDatabase(Id, out List<SavedObject> savedObjects);
                
                //if it doesnt, create it
                if (!databaseExists)
                {
                    PersistentData.CreateDatabase(Id);
                    SaveDataPersistently();
                    return;
                }
                
                //if it does, load persistently saved values locally
                string[] savedIds = new string[savedObjects.Count];
                lock (_values)
                {
                    for (int i = 0; i < savedObjects.Count; i++)
                    {
                        SavedObject savedObject = savedObjects[i];
                        string id = savedObject.ValueStorageId;

                        savedIds[i] = id;
                        _values.Add(id, savedObject.Data);
                    }

                    //save all values persistently which were not saved before
                    foreach (var localId in _values.Keys)
                    {
                        //id was saved persistently. No need to overwrite it with potentially outdated value 
                        if (savedIds.Contains(localId)) continue;

                        byte[] serializedBytes = Serialization.Serialize(_values[localId]);
                        
                        PersistentData.Save(Id, localId, serializedBytes);
                        
                        //inform peers of potentially new data
                        if(_isSynchronised) OnSetSynchronised(localId, serializedBytes);
                    }
                }
            }
        }

        private bool _isPersistent;

        private void OnSetPersistent(string id, byte[] value)
        {
            PersistentData.Save(Id, id, value);
        }

        private void SaveDataPersistently()
        {
            lock (_values)
            {
                foreach (var kv in _values)
                {
                    PersistentData.Save(Id, kv.Key, Serialization.Serialize(kv.Value));
                }
            }
        }
    }
}