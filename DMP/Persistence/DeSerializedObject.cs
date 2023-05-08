using System;

namespace DMP.Persistence
{
    /// <summary>
    /// Object which will be saved persistently
    /// </summary>
    public struct SerializedObject
    {
        public string DataBaseId { get; }
        public string ValueStorageId { get; }
        public byte[] Buffer { get; }
        public string Type { get; }
        public bool SyncRequired { get; }

        public SerializedObject(string dataBaseId, string valueStorageId, byte[] buffer, Type type, bool syncRequired)
        {
            DataBaseId = dataBaseId;
            ValueStorageId = valueStorageId;
            Buffer = buffer;
            Type = type.AssemblyQualifiedName;
            SyncRequired = syncRequired;
        }
    }
    
    /// <summary>
    /// Object which was saved persistently.
    /// Contains the data as bytes
    /// </summary>
    public struct SavedObject
    {
        public string ValueStorageId { get; }
        public byte[] Buffer { get; }
        public SavedObject(string valueStorageId, byte[] buffer)
        {
            Buffer = buffer;
            ValueStorageId = valueStorageId;
        }
    }
    
    /// <summary>
    /// Object which was saved persistently.
    /// Contains the data as bytes
    /// </summary>
    public struct TrackedSavedObject
    {
        public string ValueStorageId { get; }
        public byte[] Buffer { get; }
        public string Type { get; }
        public bool SyncRequired { get; }

        public TrackedSavedObject(string valueStorageId, byte[] buffer, string type, bool syncRequired)
        {
            Buffer = buffer;
            SyncRequired = syncRequired;
            ValueStorageId = valueStorageId;
            Type = type;
        }

        public override string ToString()
        {
            return $"Id: {ValueStorageId}, SyncRequired: {SyncRequired}";
        }
    }
    
    /// <summary>
    /// Object which was saved persistently.
    /// Contains the data as a deserialized object
    /// </summary>
    public struct DeSerializedObject
    {
        public string ValueStorageId { get; }
        public object Data { get; }

        public DeSerializedObject(string valueStorageId, object data)
        {
            ValueStorageId = valueStorageId;
            Data = data;
        }
    }
}