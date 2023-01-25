using System;
using System.Collections.Generic;
using System.Net;
using DMP.Networking.Messaging.Server;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Networking.Synchronisation.Server
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
            //Clients request to change value
            AddCallback<SetValueRequest>(((request, session) =>
            {
                //extract request parameters
                string databaseId = request.DatabaseId;
                string valueId = request.ValueId;
                uint modificationCount = request.ModificationCount;
                
                uint expected = IncrementModCount(databaseId, valueId);
                bool success = modificationCount == expected;

                //inform others of new value
                if (success)
                {
                    Console.WriteLine("Server: Broadcasting successful set value request");
                    BroadcastToOthers(request, session);
                }
                else
                {
                    EnqueueFailedRequest(session, databaseId, valueId, expected);
                }

                //notify client of success/failure
                session.SendMessage(new SetValueReply(request) { ExpectedModificationCount = expected });
            }));
            
            //clients set value, failed during previous SetValueRequest
            AddCallback<SetValueMessage>(((message, session) =>
            {
                //make sure session requested change previously
                bool success = TryDequeueFailedRequest(session, message.DatabaseId, message.ValueId,
                    message.ModificationCount);
                
                if (success)
                {
                    string error = $"{session} tried to repeat SetValueRequest{message.ModificationCount}, but never requested it!";
                    throw new ArgumentException(error);
                }

                //inform others of new value
                BroadcastToOthers(message, session);
            }));
        }
    }
}