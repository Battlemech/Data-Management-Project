using System;

namespace DMP.Networking
{
    public class ReplyTimedOutException : Exception
    {
        public ReplyTimedOutException(int timeoutInMs) : base($"Received no reply within {timeoutInMs} ms")
        {
            
        }
    }
}