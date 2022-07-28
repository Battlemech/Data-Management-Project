﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Main.Networking.Messaging.Server;
using Main.Networking.Synchronisation.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

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

                //note that session requested modification once expected mod count was reached
                if (!success)
                {
                    request.ModCount = expected;
                    OnFailedSetRequest(request, session);
                }
                else
                {
                    //forward message to others, informing them of changed value
                    BroadcastToOthers(new SetValueMessage(request), session);
                }
            }));
            
            //A client waited to send the setValueMessage and forwarded it once he was allowed to
            AddCallback<SetValueMessage>(((message, session) =>
            {
                bool success = TryRemoveDelayedRequest(message, session);

                if (!success)
                {
                    LogWriter.LogError($"{session} tried to set data globally in wrong execution order!");
                    return;
                }

                BroadcastToOthers(message, session);
            }));
        }
    }
}