using System.Net;
using Main;
using Main.Networking.Synchronisation.Client;

namespace Tests
{
    public class TestClient : SynchronisedClient
    {
        public static int IdTracker = 1;

        public readonly string Name = IdTracker++.ToString();
        
        public TestClient(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public TestClient(string address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public TestClient(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public TestClient(IPEndPoint endpoint) : base(endpoint)
        {
        }

        public override string ToString()
        {
            return $"[{Name}]";
        }
    }
}