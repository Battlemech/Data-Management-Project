using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class LockValueRequest : RequestMessage<LockValueReply>
    {
        public readonly string DatabaseId;
        public readonly string ValueId;
        public readonly uint ModCount;

        public LockValueRequest(string databaseId, string valueId, uint modCount)
        {
            DatabaseId = databaseId;
            ValueId = valueId;
            ModCount = modCount;
        }
    }
    
    public class LockValueReply : ReplyMessage
    {
        public uint ExpectedModCount;
        
        public LockValueReply(LockValueRequest requestMessage) : base(requestMessage)
        {
        }
    }
}