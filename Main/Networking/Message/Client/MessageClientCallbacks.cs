using Main.Callbacks;

namespace Main.Networking.Message.Client
{
    public partial class MessageClient
    {
        private readonly CallbackHandler<string> _callbackHandler = new CallbackHandler<string>();

        /// <summary>
        /// Add a function to be executed when a message of a certain type was received asynchronously
        /// </summary>
        public void AddCallback<T>(ValueChanged<T> onValueChange, string name = "") where T : Messages.Message
        {
            _callbackHandler.AddCallback(typeof(T).FullName, onValueChange, name);
        }

        public int RemoveCallbacks<T>(string name = "") where T : Messages.Message
        {
            return _callbackHandler.RemoveCallbacks(typeof(T).FullName, name);
        }
    }
}