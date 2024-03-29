﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DMP.Persistence;
using DMP.Utility;

namespace DMP.Databases
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
                bool databaseExists = PersistentData.TryLoadDatabase(Id, out List<SerializedObject> savedObjects);

                if (!databaseExists) OnNoData();
                else OnDataFound(savedObjects);

                //set persistence to true as last operation because the required backend infrastructure just finished building
                _isPersistent = true;
            }
        }

        private bool _isPersistent;

        private void OnSetPersistent(string id, byte[] value, Type type)
        {
            PersistentData.Save(Id, id, value, type, !_isSynchronised);
        }
        
        /// <summary>
        /// Invoked when no persistent data was found
        /// </summary>
        private void OnNoData()
        {
            //create database persistently
            PersistentData.CreateDatabase(Id);

            //save its values
            foreach (var kv in _values)
            {
                OnSetPersistent(kv.Key, kv.Value.Serialize(out Type type), type);
            }
        }
        
        /// <summary>
        /// Invoked when persistent data was found
        /// </summary>
        private void OnDataFound(List<SerializedObject> savedObjects)
        {
            List<SerializedObject> toSynchronise = new List<SerializedObject>(savedObjects.Count);

            //get list of currently known ids
            List<string> existingIds = _values.Keys.ToList();

            //save all current values persistently
            foreach (var kv in _values)
            {
                ValueStorage.ValueStorage valueStorage = kv.Value;
                OnSetPersistent(kv.Key, valueStorage.Serialize(out Type type), type);
            }

            //load all values from database which didn't already exist
            foreach (var tso in savedObjects)
            {
                string id = tso.ValueStorageId;
                
                //Skip if object with loaded id already exists
                if (existingIds.Contains(id) || _values.ContainsKey(id)) continue;

                //save value to be deserialized later
                _serializedData[id] = new Tuple<byte[], Type>(tso.Buffer, Type.GetType(tso.Type, true));

                //skip objects which don't have to be synchronised
                if(!tso.SyncRequired) continue;
                    
                //queue object for synchronisation
                toSynchronise.Add(tso);
            }

            //no need to inform peers if database is not synchronised
            if(!_isSynchronised || Client == null || !Client.IsConnected) return;

            //inform peers that data was modified while no connection was established
            foreach (var tso in toSynchronise)
            {
                OnOfflineModification(tso.ValueStorageId, tso.Buffer, Type.GetType(tso.Type));
            }
        }
    }
}