using System.Net;
using Main.Networking.Message.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Message.Client
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

        public bool SendMessage<T>(T message) where T : Messages.Message
        {
            return SendAsync(message.Serialize());
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            foreach (byte[] bytes in _networkSerializer.Deserialize(buffer, offset, size))
            {
                OnReceived(Serialization.Deserialize<Messages.Message>(bytes), bytes);    
            }
        }

        protected void OnReceived(Messages.Message message, byte[] serializedBytes)
        {
            _callbackHandler.InvokeCallbacks(message.SerializedType, serializedBytes);
        }
    }
}