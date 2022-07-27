using System;
using System.Net;
using Main.Networking.Messaging.Client;
using Main.Networking.Synchronisation.Messages;

namespace Main.Networking.Synchronisation.Client
{
    public partial class SynchronisedClient : MessageClient
    {
        public static SynchronisedClient Instance { get; private set; }

        #region Constructors

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

        #endregion

        private void Constructor()
        {
            //initialize instance
            Instance ??= this;
            
            AddCallback<SetValueMessage>((message =>
            {
                //forward set value message to database
                Get(message.DatabaseId).OnRemoteSet(message.ValueId, message.Value, message.ModCount);
            }));
        }

        ~SynchronisedClient()
        {
            //remove static reference if it was this client
            if (Instance == this) Instance = null;
        }
    }
}