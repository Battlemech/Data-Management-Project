using System.Net;
using Main.Networking.Messaging.Server;
using Main.Networking.Synchronisation.Messages;

namespace Main.Networking.Synchronisation
{
    public partial class SynchronisedServer : MessageServer
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
            AddCallback<SetValueRequest>(((request, session) =>
            {
                string databaseId = request.DatabaseId;
                string valueId = request.ValueId;

                //successful modification request
                if (request.ModCount == GetModCount(databaseId, valueId) + 1)
                {
                    
                }
            }));
        }
    }
}