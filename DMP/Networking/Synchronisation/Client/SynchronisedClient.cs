using System.Collections.Generic;
using DMP.Databases;
using DMP.Networking.Messaging.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Persistence;

namespace DMP.Networking.Synchronisation.Client
{
    public class SynchronisedClient : MessageClient
    {
        public static SynchronisedClient Instance;
        
        private readonly Dictionary<string, Database> _databases = new Dictionary<string, Database>();

        public SynchronisedClient()
        {
            AddCallback<SetValueRequest>((request =>
            {
                GetDatabase(request.DatabaseId).OnRemoteSet(request.ValueId, request.Value, request.ModificationCount);
            }));
            
            AddCallback<CollectionOperationRequest>((request =>
            {
                GetDatabase(request.DatabaseId).OnRemoteModify(request.ValueId, request.Value, request.ModificationCount, request.Type);
            }));

            //set client to instance
            Instance ??= this;
        }

        private Database GetDatabase(string databaseId)
        {
            lock (_databases)
            {
                //database exists
                if (_databases.TryGetValue(databaseId, out Database database)) return database;
                
                //create database if necessary
                bool isPersistent = PersistentData.DoesDatabaseExist(databaseId);
                database = new Database(databaseId, isPersistent, true);
                _databases.Add(databaseId, database);

                return database;
            }
        }
    }
}