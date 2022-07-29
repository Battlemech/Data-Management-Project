using System;
using Main.Databases;
using Main.Networking.Messaging;

namespace Main.Networking.Synchronisation.Messages
{
    public class SetValueRequest : RequestMessage<SetValueReply>
    {
        public string DatabaseId;
        public string ValueId;
        public uint ModCount;
        public byte[] Value;
    }
    
    public class SetValueReply : ReplyMessage
    {
        public uint ExpectedModCount;
        
        public SetValueReply(SetValueRequest requestMessage) : base(requestMessage)
        {
        }
    }

    public abstract class FailedModifyRequest : SetValueRequest
    {
        public abstract object RepeatModification(object current);
    }

    public class FailedModifyRequest<T> : FailedModifyRequest
    {
        public readonly ModifyValueDelegate<T> Modify;

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
    }
}