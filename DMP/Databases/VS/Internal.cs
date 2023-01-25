using System;
using System.Collections;
using System.Collections.Generic;
using DMP.Networking.Synchronisation.Messages;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Databases.VS
{
    public abstract class ValueStorage
    {
        public readonly string Id;
        public readonly QueuedScheduler Scheduler = new QueuedScheduler();

        public ValueStorage(string id)
        {
            Id = id;
        }
        
        //Serialization
        
        public abstract Type GetEnclosedType();
        
        public abstract byte[] Serialize();

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

        protected internal override void InternalSet(byte[] bytes)
        {
            Set(Serialization.Deserialize<T>(bytes));
        }

        public static implicit operator T(ValueStorage<T> valueStorage) => valueStorage.Get();
    }
}