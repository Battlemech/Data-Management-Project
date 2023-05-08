using System;
using System.Threading.Tasks;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Databases.ValueStorage
{
    public abstract class ValueStorage
    {
        public readonly string Id;

        public ValueStorage(string id)
        {
            Id = id;
        }
        
        public abstract Type GetEnclosedType();

        public abstract object GetObject();

        public abstract void UnsafeSet(byte[] bytes, Type type);

        public abstract Task Delegate(Task task);

        public abstract int GetQueuedTasksCount();

        public abstract int InvokeAllCallbacks();
        
        public abstract int InvokeAllCallbacks(byte[] bytes, Type type);

        public abstract int GetCallbackCount(string name = "");

        public abstract int RemoveCallbacks(string name = "");

        public abstract byte[] Serialize(out Type type);

        protected internal abstract ValueStorage Copy();
    }
    
    public partial class ReadOnlyStorage<T>
    {
        public override Type GetEnclosedType()
        {
            lock (Id)
            {
                return _data.GetType();
            }
        }

        public override object GetObject()
        {
            return _data;
        }
        
        public override void UnsafeSet(byte[] bytes, Type type)
        {
            lock (Id) _data = (T)Serialization.Deserialize(bytes, type);
        }

        public override byte[] Serialize(out Type type)
        {
            lock (Id)
            {
                type = _data.GetType();
                return Serialization.Serialize(_data);
            }
        }

        protected internal override ValueStorage Copy()
        {
            return BlockingGet((obj => new ValueStorage<T>(null, Id, obj)));
        }

        protected internal byte[] InternalSet(ModifyValueDelegate<T> modify, out Type type)
        {
            lock (Id)
            {
                _data = modify.Invoke(_data);
                type = _data.GetType();
                return Serialization.Serialize(_data);
            }
        }

        public static implicit operator T(ReadOnlyStorage<T> valueStorage) => valueStorage.Get();
    }
}