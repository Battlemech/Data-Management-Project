using System;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Databases.Utility
{
    public abstract class FailedModifyRequest : SetValueRequest
    {
        public bool IncrementModCount { get; protected set; }
        
        public abstract object RepeatModification(object current);

        public abstract Type GetDelegateType();
    }

    public class FailedModifyRequest<T> : FailedModifyRequest
    {
        public readonly ModifyValueDelegate<T> Modify;

        public FailedModifyRequest(string databaseId, string valueId, uint modCount, ModifyValueDelegate<T> modify, bool incrementModCount = false)
        {
            DatabaseId = databaseId;
            ValueId = valueId;
            ModCount = modCount;
            Modify = modify;
            IncrementModCount = incrementModCount;
        }
        
        public FailedModifyRequest(SetValueRequest request, ModifyValueDelegate<T> modify)
        {
            DatabaseId = request.DatabaseId;
            ValueId = request.ValueId;
            ModCount = request.ModCount;
            Modify = modify;
        }

        public override object RepeatModification(object current)
        {
            if (current is T data) return Modify.Invoke(data);

            throw new ArgumentException($"Expected {typeof(T)}, but got {current?.GetType()}");
        }

        public override Type GetDelegateType()
        {
            return typeof(T);
        }
    }
}