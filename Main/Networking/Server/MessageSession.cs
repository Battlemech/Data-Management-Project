using System;
using Main.Networking.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Server
{
    public class MessageSession : TcpSession
    {
        private readonly NetworkSerializer _networkSerializer = new NetworkSerializer();
        
        public MessageSession(TcpServer server) : base(server)
        {
        }

        public bool SendMessage<T>(T message) where T : Message
        {
            return SendAsync(message.Serialize());
        }
        
        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            foreach (byte[] bytes in _networkSerializer.Deserialize(buffer, offset, size))
            {
                OnReceived(Serialization.Deserialize<Message>(bytes));    
            }
        }

        protected void OnReceived(Message message)
        {
            Console.WriteLine($"Session received {message.SerializedType}");
        }
    }
}