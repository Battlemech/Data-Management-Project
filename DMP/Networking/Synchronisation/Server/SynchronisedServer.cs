using System.Net;
using DMP.Networking.Messaging.Server;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Networking.Synchronisation.Server
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
            AddCallback<SetValueRequest>(((request, session) =>
            {
                
            }));
            
            AddCallback<CollectionOperationRequest>(((request, session) =>
            {
                
            }));
        }
    }
}