using System;
using DMP.Networking.Messaging;
using DMP.Utility;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueMessage : Message
    {
        public readonly string DatabaseId;
        public readonly string ValueId;
        public readonly string Type;
        public readonly uint ModCount;
        public readonly byte[] Value;

        public SetValueMessage(SetValueRequest request)
        {
            DatabaseId = request.DatabaseId;
            ValueId = request.ValueId;
            Type = request.TypeAsString;
            ModCount = request.ModCount;
            Value = request.Value;
        }

        public SetValueMessage(string databaseId, string valueId, byte[] value, Type type, uint modCount)
        {
            DatabaseId = databaseId;
            ValueId = valueId;
            ModCount = modCount;
            Value = value;
            Type = type?.AssemblyQualifiedName;
        }

        public override string ToString()
        {
            return $"Database={DatabaseId}, ValueId={ValueId}, ModCount={ModCount}";
        }
    }
}