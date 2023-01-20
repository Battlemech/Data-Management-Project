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
        public bool SyncRequired { get; }

        public SerializedObject(string dataBaseId, string valueStorageId, byte[] buffer, bool syncRequired)
        {
            Buffer = buffer;
            SyncRequired = syncRequired;
            ValueStorageId = valueStorageId;
            DataBaseId = dataBaseId;
        }
    }

    /// <summary>
    /// Object which was saved persistently.
    /// Contains the data as bytes
    /// </summary>
    public struct DeSerializedObject
    {
        public string ValueStorageId { get; }
        public byte[] Buffer { get; }
        public bool SyncRequired { get; }

        public DeSerializedObject(string valueStorageId, byte[] buffer, bool syncRequired)
        {
            Buffer = buffer;
            SyncRequired = syncRequired;
            ValueStorageId = valueStorageId;
        }

        public override string ToString()
        {
            return $"Id: {ValueStorageId}, SyncRequired: {SyncRequired}";
        }
    }
}