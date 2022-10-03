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
            if (id.Contains("/")) 
                throw new ArgumentException("Id may not contain '/', it is used as an internal separator!");
            
            Id = id;
            _database = new Database(id, isPersistent, true);
            _database.OnReferenced(this);
        }

        /// <summary>
        /// Creates a child object for "synchronisedObject", copying its id as prefix, followed by the new object as suffix.
        /// Uses a "/" as separator.
        /// </summary>
        protected SynchronisedObject(SynchronisedObject synchronisedObject, string id, bool isPersistent = false)
        {
            if (id.Contains("/")) 
                throw new ArgumentException("Id may not contain '/', it is used as an internal separator!");
            
            Id = $"{synchronisedObject.Id}/{id}";
            _database = new Database(Id, isPersistent, true);
            _database.OnReferenced(this);
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
                _database.OnReferenced(this);
            }
            
            return _database;
        }

        public void Delete()
        {
            GetDatabase().Delete();
        }
        
        protected internal virtual void OnDelete()
        {
            _database = null;
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