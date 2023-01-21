using System;
using DMP.Utility;

namespace DMP.Databases.VS
{
    public delegate TOut SafeOperationDelegate<T, TOut>(T current);
    public delegate T SetValueDelegate<out T>();
    
    public partial class ValueStorage<T> : VS.ValueStorage
    {
        public readonly Database Database;

        private T _data;
        
        public ValueStorage(Database database, string id, T data) : base(id)
        {
            Database = database;
            _data = data;
        }

        /// <summary>
        /// Returns the current value
        /// </summary>
        /// <remarks>Value may be modified by other threads. If you want to make sure the value isn't changed, use BlockingGet()</remarks>
        public T Get() => _data;
        
        public void BlockingGet(Action<T> action)
        {
            lock (Id)
            {
                action.Invoke(_data);
            }
        }

        /// <summary>
        /// Performs a thread-safe operation on the current value
        /// </summary>
        /// <param name="safeOperation">Action to execute</param>
        /// <typeparam name="TOut">Expected result of operation</typeparam>
        /// <returns></returns>
        /// <remarks>Don't modify current value! If you want to change it, use Modify() instead!</remarks>
        public TOut BlockingGet<TOut>(SafeOperationDelegate<T, TOut> safeOperation)
        {
            lock (Id)
            {
                return safeOperation.Invoke(_data);
            }
        }
        
        public void Set(T value)
        {
            byte[] serializedBytes;
            lock (Id)
            {
                _data = value;
                serializedBytes = Serialization.Serialize(value);
            }

            //throw new NotImplementedException();
        }

        protected internal override void InternalSet(byte[] bytes)
        {
            Set(Serialization.Deserialize<T>(bytes));
        }
        
        public void BlockingSet(SetValueDelegate<T> setValueDelegate)
        {
            byte[] serializedBytes;
            lock (Id)
            {
                _data = setValueDelegate.Invoke();
                serializedBytes = Serialization.Serialize(_data);
            }
            
            throw new NotImplementedException();
        }
    }
}