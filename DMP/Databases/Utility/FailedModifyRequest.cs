using System;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Databases.Utility
{
    public abstract class FailedModifyRequest : SetValueRequest
    {
        public readonly bool IncrementModCount;
        
        public abstract object RepeatModification(object current);

        protected FailedModifyRequest(string databaseId, string valueId, uint modCount, Type type, bool incrementModCount=false) : base(databaseId, valueId, modCount, null, type)
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

        public FailedModifyRequest(string databaseId, string valueId, uint modCount, Type type, ModifyValueDelegate<T> modify, bool incrementModCount = false)
            : base(databaseId, valueId, modCount, type, incrementModCount)
        {
            _modify = modify;
        }
        
        public FailedModifyRequest(SetValueRequest request, ModifyValueDelegate<T> modify) : base(request)
        {
            _modify = modify;
        }

        public override object RepeatModification(object current)
        {
            if (current is T data) return _modify.Invoke(data);
            if (current is null) return _modify.Invoke(default);

            throw new ArgumentException($"Expected {typeof(T)}, but got {current?.GetType()}");
        }
    }
}