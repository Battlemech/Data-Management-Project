using DMP.Networking.Messaging;
using DMP.Utility;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueMessage : Message
    {
        public string DatabaseId { get; }
        public string ValueId { get; }
        public uint ModCount { get; }
        public byte[] Value { get; }
        
        public SetValueMessage(SetValueRequest request)
        {
            DatabaseId = request.DatabaseId;
            ValueId = request.ValueId;
            ModCount = request.ModCount;
            Value = request.Value;
        }

        public SetValueMessage(string databaseId, string valueId, uint modCount, byte[] value)
        {
            DatabaseId = databaseId;
            ValueId = valueId;
            ModCount = modCount;
            Value = value;
        }

        public override string ToString()
        {
            return $"Database={DatabaseId}, ValueId={ValueId}, ModCount={ModCount}, Value={LogWriter.StringifyCollection(Value)}";
        }
    }
}