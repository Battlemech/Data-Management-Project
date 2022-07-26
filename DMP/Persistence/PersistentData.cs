﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using Dapper;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Persistence
{
    public static class PersistentData
    {
        public static int DataToSaveCount => DataToSaveQueue.Count;
        public static bool SavingData { get; private set; }
        
        private const string ConnectionString = "Data Source="+Path;
        private const string Path = "./Data.sqlite";
        
        private static readonly ConcurrentQueue<SerializedObject> DataToSaveQueue =
            new ConcurrentQueue<SerializedObject>();

        static PersistentData()
        {
            //makes sure that the database exists
            SQLiteConnection.CreateFile("Data.sqlite");
        }

        #region Manage Databases

        /// <summary>
        /// Creates a reference of a database locally
        /// </summary>
        /// <param name="databaseId"></param>
        public static void CreateDatabase(string databaseId)
        {
            ExecuteCommand($"create table if not exists '{databaseId}'(id MESSAGE_TEXT PRIMARY KEY, bytes BLOB, syncRequired BOOLEAN DEFAULT FALSE)");
        }

        /// <summary>
        /// Deletes the local reference of a database.
        /// </summary>
        public static void DeleteDatabase(string databaseId) => ExecuteCommand($"drop table if exists '{databaseId}'");

        public static bool DoesDatabaseExist(string databaseId)
        {
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            
            //get list of all tables with that name
            var result = connection.Query($"select name from sqlite_master where type='table' and name='{databaseId}'") as ICollection;
                
            //return true if the list has any entries
            return result?.Count != 0;
        }

        #endregion

        #region Load Data

        /// <summary>
        /// Loads a value from the database
        /// </summary>
        public static T Load<T>(string databaseId, string valueStorageId)
        {
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            
            //load data
            byte[] bytes = connection.QueryFirst<byte[]>($"select bytes from '{databaseId}' where id='{valueStorageId}'");

            //deserialize object
            return Serialization.Deserialize<T>(bytes);
        }

        public static bool TryLoad<T>(string databaseId, string valueStorageId, out T data)
        {
            //try to load the data
            try
            {
                data = Load<T>(databaseId, valueStorageId);
            }
            catch (SQLiteException e)
            {
                //make sure it was the right exception: database didn't exist
                if (!e.Message.Contains($"no such table: {databaseId}")) throw;

                data = default;
                return false;
            }
            catch (InvalidOperationException e)
            {
                //make sure it was the right exception: element didn't exist
                if (e.Message != "Sequence contains no elements") throw;
                
                data = default;
                return false;
            }

            return true;
        }

        public static bool TryLoadDatabase(string databaseId, out List<TrackedSavedObject> serializedObjects)
        {
            //init return lists
            serializedObjects = new List<TrackedSavedObject>();
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            
            try
            {
                //todo: fix for 1000000 addCount in LoadDatabase, "database is locked" SQLite Exception
                //https://stackoverflow.com/questions/17592671/sqlite-database-locked-exception
                
                serializedObjects = connection.Query<TrackedSavedObject>($"select id as ValueStorageId, bytes as Buffer, syncRequired as SyncRequired from '{databaseId}'").AsList();
            }
            catch (SQLiteException e)
            {
                //make sure it was the right exception: database didn't exist
                if (!e.Message.Contains($"no such table: {databaseId}")) throw;
                    
                return false;
            }

            return true;
        }

        public static bool TryLoadDatabase(string databaseId, out List<DeSerializedObject> savedObjects)
        {
            //init return list
            savedObjects = new List<DeSerializedObject>();

            //open connection
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            
            try
            {
                var serializedObjects = connection.Query<SavedObject>($"select id as ValueStorageId, bytes as Buffer from '{databaseId}'").AsList();

                foreach (var serializedObject in serializedObjects)
                {
                    savedObjects.Add(new DeSerializedObject(serializedObject.ValueStorageId, Serialization.Deserialize<object>(serializedObject.Buffer)));
                }
            }
            catch (SQLiteException e)
            {
                //make sure it was the right exception: database didn't exist
                if (!e.Message.Contains($"no such table: {databaseId}")) throw;
                    
                return false;
            }

            return true;
        }

        public static bool SyncRequired(string databaseId, string valueId)
        {
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            
            //load data
            return connection.QueryFirst<bool>($"select syncRequired from '{databaseId}' where id='{valueId}'");
        }
        
        #endregion

        #region Save Data

        /// <summary>
        /// Saves an value locally. Requires a database to be created before
        /// </summary>
        public static void Save(string databaseId, string valueStorageId, byte[] bytes, bool syncRequired)
        {
            //enqueue it to be saved in sql table by working thread
            DataToSaveQueue.Enqueue(new SerializedObject(databaseId, valueStorageId, bytes, syncRequired));

            //return if another thread is already writing the data to the sql database
            lock (DataToSaveQueue)
            {
                if (SavingData) return;
                SavingData = true;
            }
            
            Delegation.EnqueueAction(SaveQueuedData);
        }

        private static void SaveQueuedData()
        {
            //establish connection with database
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Open();

            //notify database that changes will be made
            using SQLiteTransaction transaction = connection.BeginTransaction();

            //execute all queued changes
            while (DataToSaveQueue.TryDequeue(out SerializedObject obj))
            {
                try
                {
                    //set new value
                    string command = $"insert or replace into '{obj.DataBaseId}'(id, bytes, syncRequired) values ('{obj.ValueStorageId}', @Buffer, '{obj.SyncRequired}')";
                    connection.Execute(command, obj);
                }
                catch (Exception e)
                {
                    LogWriter.LogError($"Failed setting {obj.DataBaseId}-{obj.ValueStorageId}");
                    LogWriter.LogException(e);
                    throw;
                }
            }

            //commit the queued changes
            transaction.Commit();
            
            //stop executing commands if none are left to execute
            lock (DataToSaveQueue)
            {
                if (DataToSaveQueue.IsEmpty)
                {
                    SavingData = false;
                    return;
                }
            }
                
            //if commands are left to execute, start executing them again
            SaveQueuedData();
        }
        
        #endregion

        private static void ExecuteCommand(string command)
        {
            using SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Execute(command);
        }

        
    }
}