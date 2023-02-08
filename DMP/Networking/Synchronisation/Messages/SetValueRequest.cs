using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueRequest : RequestMessage<SetValueReply>
    {
        public string DatabaseId { get; set; }
        public string ValueId { get; set; }
        public uint ModCount { get; set; }
        public byte[] Value { get; set; }

        public override string ToString()
        {
            return $"SetValueRequest(modCount={ModCount})";
        }
    }
    
    public class SetValueReply : ReplyMessage
    {
        public uint ExpectedModCount { get; set; }
        
        public SetValueReply(SetValueRequest requestMessage) : base(requestMessage)
        {
        }

        public override string ToString()
        {
            return $"SetValueReply(modCount={ExpectedModCount})";
        }
    }
    
}