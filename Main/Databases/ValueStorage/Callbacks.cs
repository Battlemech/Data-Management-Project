using System;

namespace Main.Databases
{
    public partial class ValueStorage<T>
    {
        public bool AddCallback(string id, Action<T> onValueChange, string name = "", bool invokeCallback = false,
            bool unique = false, bool removeOnError = false)
            => Database.AddCallback(id, onValueChange, name, invokeCallback, unique, removeOnError);

        public int GetCallbackCount(string id, string name = "") => Database.GetCallbackCount(id, name);

        public int RemoveCallbacks(string id, string name = "") => Database.RemoveCallbacks(id, name);
    }
}