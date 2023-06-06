using System;
using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueRequest : RequestMessage<SetValueReply>
    {
        public readonly string DatabaseId;
        public readonly string ValueId;
        public uint ModCount;
        public byte[] Value;
        public string TypeAsString;

        public SetValueRequest(string databaseId, string valueId, uint modCount, byte[] value, Type type)
        {
            DatabaseId = databaseId;
            ValueId = valueId;
            ModCount = modCount;
            Value = value;
            SetType(type);
        }

        public SetValueRequest(SetValueRequest other)
        {
            DatabaseId = other.DatabaseId;
            ValueId = other.ValueId;
            ModCount = other.ModCount;
            Value = other.Value;
            TypeAsString = other.TypeAsString;
        }

        public override string ToString()
        {
            return $"SetValueRequest(modCount={ModCount})";
        }

        public void SetType(Type type)
        {
            TypeAsString = type?.AssemblyQualifiedName;
        }

        public Type GetValueType()
        {
            return TypeAsString == null ? null : Type.GetType(TypeAsString, true);
        }
    }
    
    public class SetValueReply : ReplyMessage
    {
        public uint ExpectedModCount;
        
        public SetValueReply(SetValueRequest requestMessage) : base(requestMessage)
        {
        }

        public override string ToString()
        {
            return $"SetValueReply(modCount={ExpectedModCount})";
        }
    }
    
}