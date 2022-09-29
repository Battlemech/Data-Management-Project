using System;
using DMP.Databases;
using DMP.Networking.Synchronisation.Client;

namespace DMP.Objects
{
    public abstract class SynchronisedObject
    {
        public readonly string Id;
        
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
        
        protected SynchronisedObject(string id, bool isPersistent = false)
        {
            Id = id;
            _database = new Database(id, isPersistent, true);
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

                _database = client.Get(Id);
            }
            
            return _database;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is SynchronisedObject so) return so.Id == Id;
            return false;
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}