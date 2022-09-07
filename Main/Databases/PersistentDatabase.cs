using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Main.Persistence;
using Main.Utility;

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
                
                //delete database if persistence is no longer required 
                if (!value)
                {
                    //update value
                    _isPersistent = false;
                    
                    PersistentData.DeleteDatabase(Id);
                    return;
                }

                //see if database exists
                bool databaseExists = PersistentData.TryLoadDatabase(Id, out List<SavedObject> savedObjects);

                if (!databaseExists) OnNoData();
                else OnDataFound(savedObjects);

                //set persistence to true as last operation because the required backend infrastructure just finished building
                _isPersistent = true;
            }
        }

        private bool _isPersistent;

        private void OnSetPersistent(string id, byte[] value)
        {
            PersistentData.Save(Id, id, value, !_isSynchronised);
        }
        
        /// <summary>
        /// Invoked when no persistent data was found
        /// </summary>
        private void OnNoData()
        {
            //create database persistently
            PersistentData.CreateDatabase(Id);

            bool syncRequired = !IsSynchronised;
            
            //save its values
            lock (_values)
            {
                foreach (var kv in _values)
                {
                    PersistentData.Save(Id, kv.Key, Serialization.Serialize(kv.Value), syncRequired);
                }
            }
        }
        
        /// <summary>
        /// Invoked when persistent data was found
        /// </summary>
        private void OnDataFound(List<SavedObject> savedObjects)
        {
            List<SavedObject> toSynchronise = new List<SavedObject>(savedObjects.Count);
            bool syncRequired = !IsSynchronised;

            lock (_values)
            {
                //get list of currently known ids
                List<string> existingIds = _values.Keys.ToList();

                //save all current values persistently
                foreach (var kv in _values)
                {
                    PersistentData.Save(Id, kv.Key, Serialization.Serialize(kv.Value), syncRequired);
                }

                //load all values from database which didn't already exist
                foreach (var tso in savedObjects)
                {
                    string id = tso.ValueId;
                    
                    //Skip if object with loaded id already exists
                    if (existingIds.Contains(id)) continue;
                    
                    //- Load it
                    _values[id] = Serialization.Deserialize<object>(tso.Bytes);
                    
                    //skip objects which don't have to be synchronised
                    if(!tso.SyncRequired) continue;
                    
                    //queue object for synchronisation
                    toSynchronise.Add(tso);
                }
            }

            //no need to inform peers if database is not synchronised
            if(!_isSynchronised) return;

            //delegate task to increase performance
            Task synchronisationTask = new Task((() =>
            {
                //inform peers that data was modified while no connection was established
                foreach (var tso in toSynchronise)
                {
                    OnOfflineModification(tso.ValueId, tso.Bytes);
                }
            }));
            Scheduler.QueueTask(synchronisationTask);
        }
    }
}