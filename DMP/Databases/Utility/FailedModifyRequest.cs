using System;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases.Utility
{
    public abstract class FailedModifyRequest : SetValueRequest
    {
        public readonly bool IncrementModCount;
        
        public abstract byte[] RepeatModification(byte[] value, Type type, out Type newType);

        protected FailedModifyRequest(string databaseId, string valueId, uint modCount, bool incrementModCount=false) : base(databaseId, valueId, modCount, null, null)
        {
            IncrementModCount = incrementModCount;
        }

        protected FailedModifyRequest(SetValueRequest other) : base(other)
        {
            
        }
    }

    public class FailedModifyRequest<T> : FailedModifyRequest
    {
        private readonly ModifyValueDelegate<T> _modify;

        public FailedModifyRequest(string databaseId, string valueId, uint modCount, ModifyValueDelegate<T> modify, bool incrementModCount = false)
            : base(databaseId, valueId, modCount, incrementModCount)
        {
            _modify = modify;
        }
        
        public FailedModifyRequest(SetValueRequest request, ModifyValueDelegate<T> modify) : base(request)
        {
            _modify = modify;
        }

        public override byte[] RepeatModification(byte[] value, Type type, out Type newType)
        {
            //deserialize object
            object o = type == null ? null : Serialization.Deserialize(value, type);

            //repeat the operation
            if (o is T data) o = _modify.Invoke(data);
            else if (o is null) o = _modify.Invoke(default);
            else throw new ArgumentException($"Expected {typeof(T)}, but got {o.GetType()}");

            newType = o?.GetType();
            return Serialization.Serialize(newType, o);
        }
    }
}