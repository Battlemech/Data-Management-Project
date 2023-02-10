using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class LockValueRequest : RequestMessage<LockValueReply>
    {
        public string DatabaseId { get; set; }
        public string ValueId { get; set; }
        public uint ModCount { get; set; }
    }
    
    public class LockValueReply : ReplyMessage
    {
        public uint ExpectedModCount { get; set; }
        
        public LockValueReply(LockValueRequest requestMessage) : base(requestMessage)
        {
        }
    }
}