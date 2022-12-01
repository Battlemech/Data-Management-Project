using System;
using System.Threading;
using System.Threading.Tasks;
using DMP.Utility;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        /// <summary>
        /// The modify operation considers current value during modification action.
        /// Necessary for synchronised collections: If multiple adds will be executed at the same time,
        /// the Set() function will overwrite the other values. The Modify() function will keep them during set.
        /// </summary>
        /// <remarks>
        /// This function creates inconsistent values during execution if multiple clients
        /// start modifying the same value at the same time. If you need to avoid inconsistent states
        /// or want to make sure ModifyValueDelegate() is executed exactly once, use SafeModify() instead!
        /// </remarks>
        /// <param name="modify"> Modification of value. Can be executed a second time if client falsely assumes
        /// to be up to date</param>
        /// <param name="onResultConfirmed">Delegate called once the current value was confirmed by server</param>
        /// <typeparam name="T">Type of value being modified</typeparam>
        public void Modify(ModifyValueDelegate<T> modify, Action<T> onResultConfirmed = null)
        {
            byte[] serializedBytes;

            //set value in dictionary
            lock (Id)
            {
                _data = modify.Invoke(_data);
                serializedBytes = Serialization.Serialize(_data);
            }
            
            //delegates internal logic to a thread, increasing performance
            Delegate(() =>
            {
                InvokeAllCallbacks(serializedBytes);
                Database.OnModify(Id, serializedBytes, modify, onResultConfirmed);
            });
        }
        
        public void Modify(ModifyValueDelegate<T> modify, out T result)
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            T value = default;
            
            //start modification action
            Modify(modify, valueConfirmed =>
            {
                //save result locally
                value = valueConfirmed;
                
                //signal waiting thread to continue
                resetEvent.Set();
            });

            if (!resetEvent.WaitOne(Options.DefaultTimeout))
                throw new TimeoutException($"Failed to modify {Id} within {Options.DefaultTimeout} ms!");

            //assign value resulting from modification to out parameter
            result = value;
        }
    }
}