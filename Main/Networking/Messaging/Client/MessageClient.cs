using System.Net;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Messaging.Client
{
    /// <summary>
    /// Client capable of sending and receiving messages
    /// </summary>
    /// <remarks>
    /// The receiving thread is used to invoke message callbacks. This means that only one client callback
    /// will be executed at the same time
    /// </remarks>
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

        /// <summary>
        /// Client used for testing. Simulates a remote client, generates a random Id. Connects to localhost
        /// </summary>
        public MessageClient(int port = Options.DefaultPort) : base(port)
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
                //todo: use deserialized message as only parameter-> Avoid deserializing multiple times?
                OnReceived(Serialization.Deserialize<Message>(bytes), bytes);    
            }
        }

        protected void OnReceived(Message message, byte[] serializedBytes)
        {
            _callbackHandler.InvokeCallbacks(message.SerializedType, serializedBytes);
        }
    }
}