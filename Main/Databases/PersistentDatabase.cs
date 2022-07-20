using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                bool databaseExists = PersistentData.TryLoadDatabase(Id, out List<TrackedSerializedObject> savedObjects);
                
                if (!databaseExists) OnNoData();
                else OnDataFound(savedObjects);
            }
        }

        private bool _isPersistent;

        private void OnSetPersistent(string id, byte[] value)
        {
            PersistentData.Save(Id, id, value);
        }
        
        /// <summary>
        /// Invoked when no persistent data was found
        /// </summary>
        private void OnNoData()
        {
            //create database persistently
            PersistentData.CreateDatabase(Id);
            
            //save its values
            lock (_values)
            {
                foreach (var kv in _values)
                {
                    PersistentData.Save(Id, kv.Key, Serialization.Serialize(kv.Value));
                }
            }
        }
        
        /// <summary>
        /// Invoked when persistent data was found
        /// </summary>
        private void OnDataFound(List<TrackedSerializedObject> savedObjects)
        {
            List<TrackedSerializedObject> toSynchronise = new List<TrackedSerializedObject>(savedObjects.Count);

            lock (_values)
            {
                //get list of currently known ids
                List<string> existingIds = _values.Keys.ToList();

                //save all current values persistently
                foreach (var kv in _values)
                {
                    PersistentData.Save(Id, kv.Key, Serialization.Serialize(kv.Value));
                }

                //load all values from database which didn't already exist
                foreach (var tso in savedObjects)
                {
                    string id = tso.ValueStorageId;
                    
                    //id is known, skip it
                    if (existingIds.Contains(id)) continue;
                    
                    //previously unknown data has been found. Load it
                    _values[id] = Serialization.Deserialize<object>(tso.Buffer);
                    
                    //queue object for synchronisation
                    toSynchronise.Add(tso);
                }
            }
            
            //inform peers that data was loaded
            Task synchronisationTask = new Task((() =>
            {
                foreach (var tso in toSynchronise)
                {
                    OnSetSynchronised(tso.ValueStorageId, tso.Buffer, tso.ModificationCount);
                }
            }));
            synchronisationTask.Start(_scheduler);
        }
    }
}