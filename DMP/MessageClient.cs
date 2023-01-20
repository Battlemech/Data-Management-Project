using System;
using Network;
using Network.Packets;

namespace DMP
{
    public class MessageClient
    {
        private TcpConnection _connection;
        
        public MessageClient()
        {
            
        }

        public bool Connect(string ip = "127.0.0.1", int port = 2000)
        {
            //Connection already established
            if (_connection != null) return false;
            
            _connection = ConnectionFactory.CreateTcpConnection(ip, port, out ConnectionResult result);

            //connection established successfully
            if (result == ConnectionResult.Connected) return true;

            //clear failed connection
            _connection = null;

            return false;
        }

        public bool Send()
        {
            throw new NotImplementedException();
        }
    }
}