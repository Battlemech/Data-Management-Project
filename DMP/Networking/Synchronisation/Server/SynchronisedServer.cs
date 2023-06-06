using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using DMP.Networking.Messaging.Server;
using DMP.Networking.Synchronisation.Messages;
using DMP.Submodules.NetCoreServer;
using DMP.Utility;

namespace DMP.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer : MessageServer
    {
        private readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers =
            new ConcurrentDictionary<string, RequestTracker>();
        
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
                    GetTracker(databaseId).EnqueueDelayedSetRequest(valueId, expected, session);
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
                RequestTracker tracker = GetTracker(message.DatabaseId);
                bool success = tracker.TryRemoveDelayedSetRequest(message.ValueId, message.ModCount, session, out bool deleteDatabase);

                if (!success)
                {
                    LogWriter.LogError($"{session} tried to set data globally in wrong execution order!" +
                                       $" Can't execute id={message.ValueId}, mod={message.ModCount}");
                    return;
                }

                BroadcastToOthers(message, session);

                //checks if, after processing the delayed set, a delayed database delete may be processed
                if(deleteDatabase) Broadcast(new DeleteDatabaseMessage() { DatabaseId = message.DatabaseId});
            }));
            
            AddCallback<LockValueRequest>(((request, session) =>
            {
                string databaseId = request.DatabaseId;
                string valueId = request.ValueId;
                uint expected = IncrementModCount(databaseId, valueId);

                //a set request will be received later. Make sure server expects it
                GetTracker(databaseId).EnqueueDelayedSetRequest(valueId, expected, session);

                session.SendMessage(new LockValueReply(request) { ExpectedModCount = expected });
            }));
            
            AddCallback<DeleteDatabaseMessage>(((message, _) =>
            {
                //instantly forward delete message if no other requests are queued
                if (GetTracker(message.DatabaseId).TryDeleteDatabase())
                {
                    Broadcast(message);
                }
            }));
        }
        
        private RequestTracker GetTracker(string databaseId)
        {
            if (!_requestTrackers.TryGetValue(databaseId, out RequestTracker tracker))
            {
                tracker = new RequestTracker();
                if (!_requestTrackers.TryAdd(databaseId, tracker)) return GetTracker(databaseId);
            }

            return tracker;
        }
    }
}