using System;
using System.Net;
using Main;
using Main.Networking.Synchronisation.Client;

namespace Tests
{
    public class TestClient : SynchronisedClient
    {
        public static int IdTracker = 1;

        public readonly string Name = IdTracker++.ToString();

        /// <summary>
        /// Client used for testing. Simulates a remote client, generates a random Id. Connects to localhost
        /// </summary>
        public TestClient(int port = Options.DefaultPort) : base(port)
        {
            
        }

        public override string ToString()
        {
            return $"[{Name}]";
        }
    }
}