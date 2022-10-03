using System;
using System.Collections.Generic;
using DMP.Databases.ValueStorage;
using DMP.Networking.Synchronisation.Messages;
using DMP.Objects;
using DMP.Persistence;

namespace DMP.Databases
{
    public partial class Database
    {
        private readonly List<SynchronisedObject> _objects = new List<SynchronisedObject>();

        public void Delete()
        {
            if (_isSynchronised) RequestDelete();
            else OnDelete();
        }

        private void RequestDelete()
        {
            Client.SendMessage(new DeleteDatabaseMessage() { DatabaseId = Id});
        }

        protected internal void OnReferenced(SynchronisedObject synchronisedObject)
        {
            lock (_objects)
            {
                _objects.Add(synchronisedObject);
            }
        }
        
        protected internal void OnDelete()
        {
            //delete local reference
            Client = null;
            
            //clear all references from synchronised objects to this database
            lock (_objects)
            {
                foreach (SynchronisedObject synchronisedObject in _objects)
                {
                    synchronisedObject.OnDelete();
                }
                _objects.Clear();   
            }

            //delete all persistently saved data
            PersistentData.DeleteDatabase(Id);
            
            _values.Clear();
            _confirmedValues.Clear();
            _failedGets.Clear();
            _failedRequests.Clear();
            _modificationCount.Clear();
            _pendingReplies.Clear();
            _serializedData.Clear();
            _confirmedModCount.Clear();
            _confirmedModCount.Clear();
            _onInitializedTracker = 0;
            _callbackHandler.RemoveAllCallbacks();
        }
    }
}