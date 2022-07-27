using Main.Callbacks;

namespace Main.Databases
{
    public partial class Database
    {
        private readonly CallbackHandler<string> _callbackHandler = new CallbackHandler<string>();

        public void AddCallback<T>(string key, ValueChanged<T> onValueChange, string name = "")
            => _callbackHandler.AddCallback(key, onValueChange, name);

        public int RemoveCallbacks(string id, string name = "") => _callbackHandler.RemoveCallbacks(id, name);
    }

}