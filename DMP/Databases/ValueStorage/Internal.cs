using System;
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

        public abstract void BlockingGetObject(Action<object> value);

        public abstract void UnsafeSet(object o);

        protected internal abstract ValueStorage Copy();
    }
    
    public partial class ValueStorage<T>
    {
        public override Type GetEnclosedType()
        {
            return typeof(T);
        }

        public override object GetObject()
        {
            return _data;
        }

        public override void BlockingGetObject(Action<object> action)
        {
            lock (Id) action.Invoke(_data);
        }

        public override void UnsafeSet(object o)
        {
            if (o is not T data) throw new ArgumentException($"Expected {typeof(T)}, but got: {o?.GetType()}");

            lock (Id) _data = data;
        }

        protected internal override ValueStorage Copy()
        {
            return BlockingGet((obj => new ValueStorage<T>(null, Id, obj)));
        }

        protected internal byte[] InternalSet(ModifyValueDelegate<T> modify)
        {
            lock (Id)
            {
                _data = modify.Invoke(_data);
                return Serialization.Serialize(_data);
            }
        }

        public static implicit operator T(ValueStorage<T> valueStorage) => valueStorage.Get();
    }
}