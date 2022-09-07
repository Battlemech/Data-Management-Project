using System.Collections.Concurrent;
using System.Collections.Generic;
using Main.Utility;
using Mono.Data.Sqlite;

public static class PersistentData
{
    private const string Path = "./Data.sql";
    private const string ConnectionString = "Data Source=" + Path;

    public static bool SavingData { get; private set; }
    public static int DataToSaveCount => DataToSave.Count;
    private static readonly ConcurrentQueue<ToSave> DataToSave = new ConcurrentQueue<ToSave>();

    static PersistentData()
    {
        SqliteConnection.CreateFile(Path);
    }

    public static void CreateDatabase(string databaseId)
    {
        ExecuteCommand($"create table if not exists '{databaseId}'(id MESSAGE_TEXT PRIMARY KEY, bytes BLOB, syncRequired BOOLEAN DEFAULT FALSE)");
    }

    public static void DeleteDatabase(string databaseId)
    {
        ExecuteCommand($"drop table if exists '{databaseId}'");
    }

    public static void Save(string databaseId, string valueId, byte[] bytes, bool syncRequired)
    {
        DataToSave.Enqueue(new ToSave()
        {
            DatabaseId = databaseId,
            ValueId = valueId,
            Bytes = bytes,
            SyncRequired = syncRequired
        });

        lock (DataToSave)
        {
            if(SavingData) return;
            SavingData = true;
        }

        //make sure only one thread is saving data at the same time to avoid waiting for transactions
        SaveQueuedData();
    }

    private static void SaveQueuedData()
    {
        using SqliteConnection connection = new SqliteConnection(ConnectionString);
        connection.Open();
    
        //save all queued data
        while (DataToSave.TryDequeue(out ToSave r))
        {
            //setup command
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"insert or replace into '{r.DatabaseId}'(id, bytes, syncRequired) values ('{r.ValueId}', :bytes, :syncRequired)";
            command.Parameters.AddWithValue(":bytes", r.Bytes);
            command.Parameters.AddWithValue(":syncRequired", r.SyncRequired);

            command.ExecuteNonQuery();
        }

        lock (DataToSave)
        {
            if (DataToSave.IsEmpty)
            {
                SavingData = false;
                return;
            }
        }
    
        SaveQueuedData();
    }

    public static bool TryLoadDatabase(string databaseId, out List<SavedObject> savedObjects)
    {
        savedObjects = new List<SavedObject>();
    
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
                savedObjects.Add(new SavedObject()
                {
                    ValueId = reader.GetString(0),
                    Bytes = reader[1] as byte[],
                    SyncRequired = reader.GetBoolean(2)
                });
            }
        }
        catch (SqliteException e)
        {
            if (!e.Message.Contains($"no such table: {databaseId}")) throw;

            return false;
        }

        return true;
    }
    
    public static bool TryGetObject(string databaseId, string valueId, out SavedObject result)
    {
        if (!TryLoadDatabase(databaseId, out List<SavedObject> savedObjects))
        {
            result = default;
            return false;
        }

        foreach (var savedObject in savedObjects)
        {
            if(savedObject.ValueId != valueId) continue;

            result = savedObject;
            return true;
        }

        result = default;
        return false;
    }
    
    public static bool TryLoad<T>(string databaseId, string valueId, out T value)
    {
        bool success = TryGetObject(databaseId, valueId, out SavedObject savedObject);
        value = (success) ? Serialization.Deserialize<T>(savedObject.Bytes) : default;
        return success;
    }

    public static bool TryGetSyncRequired(string databaseId, string valueId, out bool syncRequired)
    {
        bool success = TryGetObject(databaseId, valueId, out SavedObject savedObject);
        syncRequired = success && savedObject.SyncRequired;
        return success;
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

    private struct ToSave
    {
        public string DatabaseId;
        public string ValueId;
        public byte[] Bytes;
        public bool SyncRequired;            
    }
}


public struct SavedObject
{
    public string ValueId;
    public byte[] Bytes;
    public bool SyncRequired;
}