using Main.Networking.Messaging;

namespace Main.Networking.Synchronisation.Messages
{
    public class SetValueMessage : Message
    {
        public readonly string DatabaseId;
        public readonly string ValueId;
        public readonly uint ModCount;
        public readonly byte[] Value;
        
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
    }
}