using System.Net;
using Main.Submodules.NetCoreServer;

namespace Main.Networking.Messaging.Server
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

        public bool BroadcastToOthers<T>(T message, TcpSession session) where T : Message
        {
            byte[] bytes = message.Serialize();
            bool success = true;

            //todo: what happens if a client disconnects during foreach loop?
            foreach (var tcpSession in Sessions.Values)
            {
                if (tcpSession == session) continue;
                success = success && tcpSession.SendAsync(bytes);
            }

            return success;
        }
        
        protected override TcpSession CreateSession()
        {
            return new MessageSession(this);
        }
    }
}