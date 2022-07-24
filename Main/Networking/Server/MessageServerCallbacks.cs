using Main.Callbacks;
using Main.Networking.Messages;

namespace Main.Networking.Server
{
    public partial class MessageServer
    {
        private readonly CallbackHandler<string> _callbackHandler = new CallbackHandler<string>();

        public void AddCallback<T>(ValueChanged<T> onValueChange, string name = "") where T : Message
        {
            _callbackHandler.AddCallback(typeof(T).FullName, onValueChange, name);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Message
        {
            return _callbackHandler.RemoveCallbacks(typeof(T).FullName, name);
        }

        protected internal void InvokeCallbacks(string id, byte[] serializedBytes)
        {
            _callbackHandler.InvokeCallbacks(id, serializedBytes);
        }
    }
}