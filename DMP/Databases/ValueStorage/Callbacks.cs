using System;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        public bool AddCallback(Action<T> onValueChange, string name = "", bool invokeCallback = false,
            bool unique = false, bool removeOnError = false)
            => Database.AddCallback(Id, onValueChange, name, invokeCallback, unique, removeOnError);

        public int GetCallbackCount(string name = "") => Database.GetCallbackCount(Id, name);

        public int RemoveCallbacks(string name = "") => Database.RemoveCallbacks(Id, name);
        
        public int InvokeAllCallbacks() => Database.InvokeAllCallbacks(Id);
    }
}