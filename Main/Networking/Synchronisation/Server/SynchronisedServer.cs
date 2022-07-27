using System;
using System.Net;
using Main.Networking.Messaging.Server;
using Main.Networking.Synchronisation.Messages;

namespace Main.Networking.Synchronisation.Server
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
                SetValueReply reply = new SetValueReply(request);

                uint expected = IncrementModCount(databaseId, valueId);
                bool success = request.ModCount == expected;

                //notify client of result
                reply.ExpectedModCount = expected;
                session.SendMessage(reply);
                
                //send message to others if request was successful
                if (!success) return;

                //forward message to others, informing them of changed value
                BroadcastToOthers(new SetValueMessage(request), session);
                //Broadcast(new SetValueMessage(request));
            }));
        }
    }
}