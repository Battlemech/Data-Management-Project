using System.Net;
using Main.Networking.Base.Client;

namespace Main.Networking.SynchronisedDatabase
{
    public class SynchronisedClient : MessageClient
    {
        public SynchronisedClient(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedClient(string address, int port = Options.DefaultPort) : base(address, port)
        {
            Constructor();
        }

        public SynchronisedClient(DnsEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }

        public SynchronisedClient(IPEndPoint endpoint) : base(endpoint)
        {
            Constructor();
        }

        private void Constructor()
        {
            
        }
    }
}