using System;
using Main.Databases;
using Main.Networking.Synchronisation.Client;

namespace Main.Objects
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

        public void Modify<T>(string id, ModifyValueDelegate<T> modify, Action<T> onResultConfirmed = null) =>
            GetDatabase().Modify(id, modify, onResultConfirmed);

        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify) => GetDatabase().SafeModify(id, modify);
    }
}