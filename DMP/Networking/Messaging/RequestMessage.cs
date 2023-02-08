using System;

namespace DMP.Networking.Messaging
{
    public abstract class RequestMessage : Message
    {
        public Int16 Id { get; }

        protected RequestMessage()
        {
            Id = IdGenerator.GenerateId();
        }
        
        private static class IdGenerator
        {
            private static Int16 _currentId;
            private static readonly object Locked = new object();

            public static Int16 GenerateId()
            {
                lock (Locked) return _currentId++;
            }
        }
    }

    public abstract class RequestMessage<T> : RequestMessage where T : ReplyMessage
    {
        
    }

    public abstract class ReplyMessage : Message
    {
        public Int16 Id { get; }

        protected ReplyMessage(RequestMessage requestMessage)
        {
            Id = requestMessage.Id;
        }
    }
}