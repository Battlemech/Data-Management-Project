using System.Net;
using Main.Submodules.NetCoreServer;

namespace Main.Networking
{
    public class MessageServer : TcpServer
    {
        public MessageServer(IPAddress address) : this(address, Options.DefaultPort)
        {
        }
        
        public MessageServer(IPAddress address, int port) : base(address, port)
        {
        }
        
        public MessageServer(string address) : base(address, Options.DefaultPort)
        {
        }

        public MessageServer(string address, int port) : base(address, port)
        {
        }

        public MessageServer(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public MessageServer(IPEndPoint endpoint) : base(endpoint)
        {
        }

        protected override TcpSession CreateSession()
        {
            return new MessageSession(this);
        }
    }
}