using System;
using System.Collections.Generic;
using DMP.Databases;
using DMP.Persistence;

namespace DMP.Networking.Synchronisation.Client
{
    public partial class SynchronisedClient
    {
        private readonly Dictionary<string, Database> _databases = new Dictionary<string, Database>();

        public Database Get(string databaseId)
        {
            lock (_databases)
            {
                bool success = _databases.TryGetValue(databaseId, out Database database);

                //create database if no reference of it existed
                if (!success)
                {
                    bool existsLocally = PersistentData.DoesDatabaseExist(databaseId);
                    database = new Database(databaseId, existsLocally)
                    {
                        Client = this,
                        IsSynchronised = true
                    };

                    //don't add database to _databases because it is managed by setting the values in database (Client, IsSynchronised)
                }

                return database;
            }
        }
        
        protected internal void AddDatabase(Database database)
        {
            lock (_databases)
            {
                if (_databases.ContainsKey(database.Id))
                    throw new Exception($"A synchronised database with id {database.Id} is already being managed by {this}!");
                
                _databases.Add(database.Id, database);
            }
        }

        protected internal void RemoveDatabase(Database database)
        {
            lock (_databases)
            {
                bool success = _databases.Remove(database.Id);
                
                if(success) return;
            }

            throw new Exception($"{this} has can't remove database with id {database.Id} because it never added it!");
        }
    }
}