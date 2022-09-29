using System;
using DMP.Databases;
using DMP.Networking.Synchronisation.Client;

namespace DMP.Objects
{
    public abstract class SynchronisedObject
    {
        public readonly string DatabaseId;
        
        public bool IsHost => GetDatabase().IsHost;
        public Guid HostId => GetDatabase().HostId;

        public bool IsPersistent
        {
            get => GetDatabase().IsPersistent;
            set => GetDatabase().IsPersistent = value;
        }
        public bool IsSynchronised => GetDatabase().IsSynchronised;

        public bool ClientPersistence
        {
            get => GetDatabase().ClientPersistence;
            set => GetDatabase().ClientPersistence = value;
        }
        public bool HostPersistence
        {
            get => GetDatabase().HostPersistence;
            set => GetDatabase().HostPersistence = value;
        }

        //value is ignored during serialization (See Options.cs)
        private Database _database = null;
        
        protected SynchronisedObject(string databaseId, bool isPersistent = false)
        {
            DatabaseId = databaseId;
            _database = new Database(databaseId, isPersistent, true);
        }
        
        public Database GetDatabase()
        {
            //retrieve local version of database if necessary
            if (_database == null)
            {
                //try retrieving the client
                SynchronisedClient client = SynchronisedClient.Instance;
                if (client == null) 
                    throw new InvalidOperationException("Can't retrieve SynchronisedObject if no local SynchronisedClient exists!");

                _database = client.Get(DatabaseId);
            }
            
            return _database;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is SynchronisedObject so) return so.DatabaseId == DatabaseId;
            return false;
        }

        public override int GetHashCode()
        {
            return (DatabaseId != null ? DatabaseId.GetHashCode() : 0);
        }
    }
}