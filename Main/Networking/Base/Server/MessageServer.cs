using System.Net;
using Main.Submodules.NetCoreServer;

namespace Main.Networking.Base.Server
{
    public partial class MessageServer : TcpServer
    {
        public MessageServer(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
        }
        
        public MessageServer(string address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public MessageServer(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public MessageServer(IPEndPoint endpoint) : base(endpoint)
        {
        }

        public bool Broadcast<T>(T message) where T : Message
        {
            return Multicast(message.Serialize());
        }
        
        protected override TcpSession CreateSession()
        {
            return new MessageSession(this);
        }
    }
}