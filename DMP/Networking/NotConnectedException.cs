using System;

namespace DMP.Networking
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException() : base("Client is not connected!")
        {
            
        }
    }
}