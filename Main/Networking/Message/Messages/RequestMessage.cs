using System;

namespace Main.Networking.Message.Messages
{
    public abstract class RequestMessage : Message
    {
        public readonly Int16 Id = IdGenerator.GenerateId();

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
        public T ReplyMessage = null;
    }

    public abstract class ReplyMessage : Message
    {
        public readonly Int16 Id;

        protected ReplyMessage(RequestMessage requestMessage)
        {
            Id = requestMessage.Id;
        }
    }
}