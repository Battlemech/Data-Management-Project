using System.Net;
using Main.Networking.Base.Server;

namespace Main.Networking.SynchronisedDatabase
{
    public class SynchronisedServer : MessageServer
    {
        public SynchronisedServer(IPAddress address) : base(address)
        {
            Constructor();
        }

        public SynchronisedServer(IPAddress address, int port) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedServer(string address) : base(address)
        {
            Constructor();
        }

        public SynchronisedServer(string address, int port) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedServer(DnsEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }

        public SynchronisedServer(IPEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }

        private void Constructor()
        {
            
        }
    }
}