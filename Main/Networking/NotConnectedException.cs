using System;

namespace Main.Networking
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException() : base("Client is not connected!")
        {
            
        }
    }
}