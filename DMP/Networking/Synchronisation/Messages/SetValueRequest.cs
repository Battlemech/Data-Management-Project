using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueRequest : RequestMessage<SetValueReply>
    {
        public string DatabaseId;
        public string ValueId;
        public byte[] Value;
        public uint ModificationCount;
    }

    public class SetValueReply : ReplyMessage
    {
        public uint ExpectedModificationCount;
        
        public SetValueReply(SetValueRequest requestMessage) : base(requestMessage)
        {
            
        }
    }
}