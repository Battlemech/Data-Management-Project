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
    
}