using Main.Networking.Messaging;

namespace Main.Networking.Synchronisation.Messages
{
    public class LockValueRequest : RequestMessage<LockValueReply>
    {
        public string DatabaseId;
        public string ValueId;
        public uint ModCount;  
    }
    
    public class LockValueReply : ReplyMessage
    {
        public uint ExpectedModCount;
        
        public LockValueReply(LockValueRequest requestMessage) : base(requestMessage)
        {
        }
    }
}