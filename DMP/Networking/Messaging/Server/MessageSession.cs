using System;
using DMP.Callbacks;
using DMP.Submodules.NetCoreServer;
using DMP.Utility;

namespace DMP.Networking.Messaging.Server
{
    public partial class MessageSession : TcpSession
    {
        private readonly NetworkSerializer _networkSerializer = new NetworkSerializer();
        private readonly CallbackHandler<Type> _requestHandler = new CallbackHandler<Type>();
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

        private void OnReceived(Message message, byte[] serializedMessage)
        {
            //extract type
            Type type = message.GetMessageType();

            //deserialize message
            object deserializedMessage = Serialization.Deserialize(serializedMessage, type);

            //invoke global callbacks
            _server.InvokeCallbacks(type, deserializedMessage, this);

            //invoke callbacks specific to this message session
            _requestHandler.UnsafeInvokeCallbacks(type, deserializedMessage);
        }
        
        public void AddCallback<T>(Action<T> onValueChange, string name = "") where T : Message
        {
            _requestHandler.AddCallback(typeof(T), onValueChange, name);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            return _requestHandler.RemoveCallbacks(typeof(T), name);
        }
    }
}