
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DMP.Threading;
using DMP.Utility;
using Mono.Data.Sqlite;

namespace DMP.Persistence
{
    public static class PersistentData
    {
        
        private const string Path = "./Data.sql";
        private const string ConnectionString = "Data Source=" + Path;

        public static bool SavingData { get; private set; }
        public static int DataToSaveCount => DataToSave.Count;
        private static readonly ConcurrentQueue<SerializedObject> DataToSave = new ConcurrentQueue<SerializedObject>();

        static PersistentData()
        {
            SqliteConnection.CreateFile(Path);
        }

        public static void CreateDatabase(string databaseId)
        {
            ExecuteCommand($"create table if not exists '{databaseId}'(id MESSAGE_TEXT PRIMARY KEY, bytes BLOB, type MESSAGE_TEXT, syncRequired BOOLEAN DEFAULT FALSE)");
        }

        public static void DeleteDatabase(string databaseId)
        {
            ExecuteCommand($"drop table if exists '{databaseId}'");
        }

        public static void Save(string databaseId, string valueId, byte[] bytes, Type type, bool syncRequired)
        {
            DataToSave.Enqueue(new SerializedObject(databaseId, valueId, bytes, type, syncRequired));

            lock (DataToSave)
            {
                if(SavingData) return;
                SavingData = true;
            }
            
            //make sure only one thread is saving data at the same time to avoid waiting for transactions
            Delegation.EnqueueAction(SaveQueuedData);
        }

        private static void SaveQueuedData()
        {
            while (true)
            {
                using SqliteConnection connection = new SqliteConnection(ConnectionString);
                connection.Open();

                //save all queued data
                while (DataToSave.TryDequeue(out SerializedObject r))
                {
                    //setup command
                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = $"insert or replace into '{r.DataBaseId}'(id, bytes, type, syncRequired) values ('{r.ValueStorageId}', :bytes, :type, :syncRequired)";
                    command.Parameters.AddWithValue(":bytes", r.Buffer);
                    command.Parameters.AddWithValue(":type", r.Type);
                    command.Parameters.AddWithValue(":syncRequired", r.SyncRequired);

                    command.ExecuteNonQuery();
                }

                lock (DataToSave)
                {
                    if (!DataToSave.IsEmpty) continue;
                    
                    SavingData = false;
                    return;
                }
            }
        }

        public static bool TryLoadDatabase(string databaseId, out List<SerializedObject> savedObjects)
        {
            savedObjects = new List<SerializedObject>();
        
            //setup command
            using SqliteConnection connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"select id as ValueStorageId, bytes as Buffer, syncRequired as SyncRequired from '{databaseId}'";

            //read data
            try
            {
                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    savedObjects.Add(new SerializedObject(databaseId, reader.GetString(0), reader[1] as byte[], Type.GetType(reader.GetString(2)), reader.GetBoolean(2)));
                }
            }
            catch (SqliteException e)
            {
                if (!e.Message.Contains($"no such table: {databaseId}")) throw;

                return false;
            }

            return true;
        }


        //todo: more efficient implementation
        public static bool DoesDatabaseExist(string databaseId)
        {
            return TryLoadDatabase(databaseId, out _);
        }

        private static void ExecuteCommand(string commandString)
        {
            //setup command
            using SqliteConnection connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = commandString;

            //execute command
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}