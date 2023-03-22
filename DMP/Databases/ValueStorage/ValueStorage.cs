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
            lock (Id)
            {
                _data = value;
                serializedBytes = Serialization.Serialize(value);
            }
            
            //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
            //allowing the delegation of callbacks to a task
            Delegate((() =>
            {
                InvokeAllCallbacks(serializedBytes);
                Database.OnSet(Id, serializedBytes);
            }));
        }

        public void BlockingSet(SetValueDelegate<T> setValueDelegate)
        {
            byte[] serializedBytes;
            lock (Id)
            {
                _data = setValueDelegate.Invoke();
                serializedBytes = Serialization.Serialize(_data);
            }
            
            //Using serialized bytes in callback to make sure "value" wasn't changed in the meantime,
            //allowing the delegation of callbacks to a task
            Delegate((() =>
            {
                InvokeAllCallbacks(serializedBytes);
                Database.OnSet(Id, serializedBytes);
            }));
        }

        public override string ToString()
        {
            return $"{Database}-{Id}:";
        }
        
        public static implicit operator T(ValueStorage<T> valueStorage) => valueStorage.Get();
    }
}