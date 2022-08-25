using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Main.Submodules.NetCoreServer;

namespace Main.Networking.Messaging.Server
{
    public partial class MessageServer : TcpServer
    {
        public MessageServer(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
        }
        
        public MessageServer(string address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public MessageServer(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public MessageServer(IPEndPoint endpoint) : base(endpoint)
        {
        }

        public bool Broadcast<T>(T message) where T : Message
        {
            return Multicast(message.Serialize());
        }

        public bool BroadcastToOthers<T>(T message, TcpSession session) where T : Message
        {
            byte[] bytes = message.Serialize();
            bool success = true;

            //todo: what happens if a client disconnects during foreach loop?
            foreach (var tcpSession in Sessions.Values)
            {
                if (tcpSession == session) continue;
                success = success && tcpSession.SendAsync(bytes);
            }

            return success;
        }

        public delegate void OnReply<T>(List<T> replies) where T : ReplyMessage;
        
        public bool SendRequests<TRequest, TReply>(TRequest request, OnReply<TReply> onReply)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            bool success = true;
            List<MessageSession> sessions = new List<MessageSession>(Sessions.Values.Cast<MessageSession>());
            List<TReply> replies = new List<TReply>(sessions.Count);

            foreach (var session in sessions)
            {
                success = success && session.SendRequest<TRequest, TReply>(request, (reply) =>
                {
                    replies.Add(reply);
                    
                    //wait for all replies to arrive
                    if(replies.Count != sessions.Count) return;
                    
                    onReply.Invoke(replies);
                });
            }

            return success;
        }

        public bool SendRequestsToOthers<TRequest, TReply>(TRequest request, TcpSession excluded, OnReply<TReply> onReply)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            bool success = true;
            List<MessageSession> sessions = new List<MessageSession>(Sessions.Values.Cast<MessageSession>());
            List<TReply> replies = new List<TReply>(sessions.Count);

            foreach (var session in sessions)
            {
                if(session == excluded) continue;
                
                success = success && session.SendRequest<TRequest, TReply>(request, (reply) =>
                {
                    replies.Add(reply);
                    
                    //wait for all replies to arrive
                    if(replies.Count != sessions.Count - 1) return;
                    
                    onReply.Invoke(replies);
                });
            }

            return success;
        }
        
        protected override void OnConnected(TcpSession session)
        {
            if (session is not MessageSession messageSession)
                throw new Exception(
                    $"Expected connected session to be of type MessageSession, but is {session?.GetType()}");
            
            OnConnected(messageSession);
        }

        protected virtual void OnConnected(MessageSession session)
        {
            
        }

        protected override TcpSession CreateSession()
        {
            return new MessageSession(this);
        }
    }
}