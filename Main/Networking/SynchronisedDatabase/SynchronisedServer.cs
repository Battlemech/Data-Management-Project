using System.Net;
using Main.Networking.Base.Server;

namespace Main.Networking.SynchronisedDatabase
{
    public class SynchronisedServer : MessageServer
    {
        public SynchronisedServer(IPAddress address) : base(address)
        {
        }

        public SynchronisedServer(IPAddress address, int port) : base(address, port)
        {
        }

        public SynchronisedServer(string address) : base(address)
        {
        }

        public SynchronisedServer(string address, int port) : base(address, port)
        {
        }

        public SynchronisedServer(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public SynchronisedServer(IPEndPoint endpoint) : base(endpoint)
        {
        }
    }
}