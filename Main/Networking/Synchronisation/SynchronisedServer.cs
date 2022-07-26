using System.Net;
using Main.Networking.Message.Server;

namespace Main.Networking.Synchronisation
{
    public class SynchronisedServer : MessageServer
    {
        public SynchronisedServer(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedServer(string address, int port = Options.DefaultPort) : base(address, port)
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