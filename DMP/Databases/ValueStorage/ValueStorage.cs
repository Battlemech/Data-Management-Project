using System;
using DMP.Utility;

namespace DMP.Databases.ValueStorage
{
    public delegate TOut SafeOperationDelegate<T, TOut>(T current);
    public delegate T SetValueDelegate<out T>();
    
    public partial class ValueStorage<T> : ReadOnlyStorage<T>
    {
        public ValueStorage(Database database, string id, T data) : base(database, id, data)
        {
            
        }
        
        public void Set(T value)
        {
            byte[] serializedBytes;
            Type type;
            
            lock (Id)
            {
                _data = value;
                serializedBytes = Serialization.Serialize(value);
                type = _data?.GetType();
            }
            
            //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
            //allowing the delegation of callbacks to a task
            Delegate((() =>
            {
                InvokeAllCallbacks(serializedBytes, type);
                Database.OnSet(Id, serializedBytes, type);
            }));
        }

        public void BlockingSet(SetValueDelegate<T> setValueDelegate)
        {
            byte[] serializedBytes;
            Type type;
            
            lock (Id)
            {
                _data = setValueDelegate.Invoke();
                serializedBytes = Serialization.Serialize(_data);
                type = _data.GetType();
            }
            
            //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
            //allowing the delegation of callbacks to a task
            Delegate((() =>
            {
                InvokeAllCallbacks(serializedBytes, type);
                Database.OnSet(Id, serializedBytes, type);
            }));
        }

        public override string ToString()
        {
            return $"{Database}-{Id}:";
        }
        
        public static implicit operator T(ValueStorage<T> valueStorage) => valueStorage.Get();
    }
}