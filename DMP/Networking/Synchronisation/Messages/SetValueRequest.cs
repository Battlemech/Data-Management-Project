using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueRequest : RequestMessage<SetValueReply>
    {
        public string DatabaseId;
        public string ValueId;
        public uint ModCount;
        public byte[] Value;

        public override string ToString()
        {
            return $"SetValueRequest(modCount={ModCount})";
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