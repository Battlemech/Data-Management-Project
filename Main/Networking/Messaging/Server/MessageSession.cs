using Main.Callbacks;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Messaging.Server
{
    public partial class MessageSession : TcpSession
    {
        private readonly NetworkSerializer _networkSerializer = new NetworkSerializer();
        private readonly CallbackHandler<string> _requestHandler = new CallbackHandler<string>();
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
            //invoke global callbacks
            _server.InvokeCallbacks(message.SerializedType, serializedMessage,this);
            
            //invoke callbacks specific to this message session
            _requestHandler.InvokeCallbacks(message.SerializedType, serializedMessage);
        }
        
        public void AddCallback<T>(ValueChanged<T> onValueChange, string name = "") where T : Message
        {
            _requestHandler.AddCallback(typeof(T).FullName, onValueChange, name);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            return _requestHandler.RemoveCallbacks(typeof(T).FullName, name);
        }
    }
}