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
}