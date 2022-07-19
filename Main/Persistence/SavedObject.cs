namespace Data_Management_Project.Databases.Base
{
    public struct SavedObject
    {
        public string ValueStorageId { get; }
        public object Data { get; }

        public SavedObject(string valueStorageId, object data)
        {
            ValueStorageId = valueStorageId;
            Data = data;
        }
    }
    
    public struct SerializedObject
    {
        public string DataBaseId { get; }
        public string ValueStorageId { get; }
        public byte[] Buffer { get; }

        public SerializedObject(string dataBaseId, string valueStorageId, byte[] buffer)
        {
            Buffer = buffer;
            ValueStorageId = valueStorageId;
            DataBaseId = dataBaseId;
        }
    }
}