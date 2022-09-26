using System;
using System.Net;
using DMP.Networking.Messaging.Client;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Networking.Synchronisation.Client
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

        /// <summary>
        /// Client used for testing. Simulates a remote client, generates a random Id. Connects to localhost
        /// </summary>
        public SynchronisedClient(int port = Options.DefaultPort) : base(port)
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
                Get(message.DatabaseId).OnRemoteSet(message.ValueId, message.Value, message.ModCount, true);
            }));
            
            AddCallback<GetValueRequest>((message) =>
            {
                //get local values
                Get(message.DatabaseId).OnRemoteGet(message.ModificationCount, messages =>
                {
                    //once setValueMessages were created, inform server of their current values
                    SendMessage(new GetValueReply(message) { SetValueMessages = messages });
                });
            });
        }
    }
}