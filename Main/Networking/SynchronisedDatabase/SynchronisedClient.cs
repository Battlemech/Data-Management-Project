using System.Net;
using Main.Networking.Base.Client;

namespace Main.Networking.SynchronisedDatabase
{
    public class SynchronisedClient : MessageClient
    {
        public SynchronisedClient(IPAddress address) : base(address)
        {
        }

        public SynchronisedClient(IPAddress address, int port) : base(address, port)
        {
        }

        public SynchronisedClient(string address) : base(address)
        {
        }

        public SynchronisedClient(string address, int port) : base(address, port)
        {
        }

        public SynchronisedClient(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public SynchronisedClient(IPEndPoint endpoint) : base(endpoint)
        {
        }
    }
}