using System;
using System.Collections.Generic;
using System.Net;
using DMP.Databases;
using DMP.Networking.Messaging.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Persistence;

namespace DMP.Networking.Synchronisation.Client
{
    public class SynchronisedClient : MessageClient
    {
        public static SynchronisedClient Instance;
        private static int _counter;
        
        private readonly Dictionary<string, Database> _databases = new Dictionary<string, Database>();
        private int _id;

        public SynchronisedClient(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedClient(string address, int port = Options.DefaultPort) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedClient(DnsEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }

        public SynchronisedClient(IPEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }
        
        private void Constructor()
        {
            AddCallback<SetValueRequest>((request =>
            {
                GetDatabase(request.DatabaseId).OnRemoteSet(request.ValueId, request.Value, request.ModificationCount);
            }));
            
            AddCallback<SetValueMessage>((message =>
            {
                GetDatabase(message.DatabaseId).OnRemoteSet(message.ValueId, message.Value, message.ModificationCount);
            }));

            //set client to instance
            Instance ??= this;

            _id = _counter++;
        }

        protected internal void AddDatabase(Database database)
        {
            lock (_databases)
            {
               _databases.Add(database.Id, database); 
            }
        }
        
        public Database GetDatabase(string databaseId)
        {
            lock (_databases)
            {
                //database exists
                if (_databases.TryGetValue(databaseId, out Database database)) return database;
                
                //create database if necessary
                bool isPersistent = PersistentData.DoesDatabaseExist(databaseId);
                database = new Database(databaseId, isPersistent, true);
                
                //database is added to dictionary with internal logic, not required here

                return database;
            }
        }

        public override string ToString()
        {
            return $"[{_id}]";
        }
    }
}