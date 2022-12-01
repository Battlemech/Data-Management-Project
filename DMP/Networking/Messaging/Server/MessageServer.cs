using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DMP.Submodules.NetCoreServer;

namespace DMP.Networking.Messaging.Server
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

        public delegate void OnReplies<T>(List<T> replies) where T : ReplyMessage;
        
        public bool SendRequests<TRequest, TReply>(TRequest request, OnReplies<TReply> onReplies)
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
                    
                    onReplies.Invoke(replies);
                });
            }

            return success;
        }

        public bool SendRequestsToOthers<TRequest, TReply>(TRequest request, TcpSession excluded, OnReplies<TReply> onReplies)
            where TReply : ReplyMessage
            where TRequest : RequestMessage<TReply>
        {
            bool success = true;
            List<MessageSession> sessions = new List<MessageSession>(Sessions.Values.Cast<MessageSession>());
            List<TReply> replies = new List<TReply>(sessions.Count);

            //no sessions connected. Instantly invoke onReplies
            if (sessions.Count == 0)
            {
                onReplies.Invoke(replies);
                return true;
            }
            
            foreach (var session in sessions)
            {
                if(session == excluded) continue;
                
                success = success && session.SendRequest<TRequest, TReply>(request, (reply) =>
                {
                    replies.Add(reply);
                    
                    //wait for all replies to arrive
                    if(replies.Count != sessions.Count - 1) return;
                    
                    onReplies.Invoke(replies);
                });
            }

            return success;
        }
        
        protected override void OnConnected(TcpSession session)
        {
            if (session is MessageSession messageSession) OnConnected(messageSession);
            else throw new InvalidCastException($"Expected connected session to be of type MessageSession, " +
                                               $"but is {session?.GetType()}");
        }

        protected virtual void OnConnected(MessageSession session)
        {
            
        }

        protected override void OnDisconnected(TcpSession session)
        {
            if(session is MessageSession messageSession) OnDisconnected(messageSession);
            else throw new InvalidCastException($"Expected connected session to be of type MessageSession, " +
                                                $"but is {session?.GetType()}");
        }

        protected virtual void OnDisconnected(MessageSession messageSession)
        {
            
        }

        protected override TcpSession CreateSession()
        {
            return new MessageSession(this);
        }
    }
}