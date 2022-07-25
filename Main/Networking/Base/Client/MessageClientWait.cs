using System.Threading;

namespace Main.Networking.Base.Client
{
    public partial class MessageClient
    {
        private readonly ManualResetEvent _connectedEvent = new ManualResetEvent(false);
        
        protected override void OnConnected()
        {
            _connectedEvent.Set();
        }

        protected override void OnDisconnecting()
        {
            _connectedEvent.Reset();
        }

        /// <summary>
        /// Waits until a connection was established. Disconnects if server times out.
        /// Returns true if connection was established, otherwise false
        /// </summary>
        public bool WaitForConnect()
        {
            if(_connectedEvent.WaitOne(Options.DefaultTimeout)) return true;

            Disconnect();

            return false;
        }
    }
}