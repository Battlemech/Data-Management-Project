using System.Net;
using Main.Networking.Base.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Base.Client
{
    public partial class MessageClient : TcpClient
    {
        private readonly NetworkSerializer _networkSerializer = new NetworkSerializer();
        
        public MessageClient(IPAddress address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public MessageClient(string address, int port = Options.DefaultPort) : base(address, port)
        {
        }

        public MessageClient(DnsEndPoint endpoint) : base(endpoint)
        {
        }

        public MessageClient(IPEndPoint endpoint) : base(endpoint)
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
                OnReceived(Serialization.Deserialize<Message>(bytes), bytes);    
            }
        }

        protected void OnReceived(Message message, byte[] serializedBytes)
        {
            _callbackHandler.InvokeCallbacks(message.SerializedType, serializedBytes);
        }
    }
}