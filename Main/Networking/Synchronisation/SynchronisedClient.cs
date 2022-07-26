using System.Net;
using Main.Networking.Messaging.Client;

namespace Main.Networking.Synchronisation
{
    public class SynchronisedClient : MessageClient
    {
        public static SynchronisedClient Instance { get; private set; }
        
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
            //initialize instance
            Instance ??= this;
        }

        ~SynchronisedClient()
        {
            //remove static reference if it was this client
            if (Instance == this) Instance = null;
        }
    }
}