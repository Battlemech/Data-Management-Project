using System;
using Main.Networking.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Server
{
    public class MessageSession : TcpSession
    {
        private readonly NetworkSerializer _networkSerializer = new NetworkSerializer();
        private readonly MessageServer _server;
        
        public MessageSession(MessageServer server) : base(server)
        {
            _server = server;
        }

        public bool SendMessage<T>(T message) where T : Message
        {
            return SendAsync(message.Serialize());
        }
        
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            foreach (byte[] bytes in _networkSerializer.Deserialize(buffer, offset, size))
            {
                OnReceived(Serialization.Deserialize<Message>(bytes), bytes);    
            }
        }

        protected void OnReceived(Message message, byte[] serializedMessage)
        {
            _server.InvokeCallbacks(message.SerializedType, serializedMessage,this);
        }
    }
}