using System;
using DMP.Utility;

namespace DMP.Databases.VS
{
    public abstract class ValueStorage
    {
        public readonly string Id;

        public ValueStorage(string id)
        {
            Id = id;
        }
        
        //Serialization
        
        public abstract Type GetEnclosedType();
        
        public abstract byte[] Serialize();

        protected internal abstract ValueStorage Copy();
        
        //Callbacks
        
        public abstract int InvokeAllCallbacks();
        
        public abstract int InvokeAllCallbacks(byte[] bytes);

        public abstract int GetCallbackCount(string name = "");

        public abstract int RemoveCallbacks(string name = "");
        
        //Setters

        protected internal abstract void InternalSet(byte[] bytes);
    }
    
    public partial class ValueStorage<T>
    {
        public override Type GetEnclosedType()
        {
            return typeof(T);
        }

        public override byte[] Serialize()
        {
            return BlockingGet(Serialization.Serialize);
        }

        protected internal override ValueStorage Copy()
        {
            return BlockingGet((obj => new ValueStorage<T>(null, Id, obj)));
        }

        public static implicit operator T(ValueStorage<T> valueStorage) => valueStorage.Get();
    }
}