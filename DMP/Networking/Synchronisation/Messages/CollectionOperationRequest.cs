using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class CollectionOperationRequest : SetValueRequest
    {
        public CollectionOperation Type;
    }

    public enum CollectionOperation
    {
        Set, Add, Delete
    }

    public class CollectionOperationReply : SetValueReply
    {
        public CollectionOperationReply(CollectionOperationRequest requestMessage) : base(requestMessage)
        {
        }
    }
}