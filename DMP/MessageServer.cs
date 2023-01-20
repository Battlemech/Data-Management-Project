using Network;
using Network.Enums;

namespace DMP
{
    public class MessageServer
    {
        private ServerConnectionContainer _server;
        
        public MessageServer()
        {
            
        }

        public bool Start(int port = 2000)
        {
            //server is already started
            if (_server != null) return false;
            
            _server = ConnectionFactory.CreateServerConnectionContainer(port, false);

            _server.ConnectionEstablished += OnConnect;
            _server.ConnectionLost += OnDisconnect;

            _server.Start();
            return true;
        }

        protected virtual void OnConnect(Connection connection, ConnectionType connectionType)
        {
            
        }

        protected virtual void OnDisconnect(Connection connection, ConnectionType connectionType,
            CloseReason closeReason)
        {
            
        }

        public bool Stop()
        {
            if (_server == null) return false;
            
            _server.Stop();

            return true;
        }
    }
}