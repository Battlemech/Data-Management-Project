using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DMP.Threading;
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
        /// <returns>The internal task invoking callbacks and sending the modification request</returns>
        public Task Modify(ModifyValueDelegate<T> modify, Action<T> onResultConfirmed = null)
        {
            byte[] serializedBytes;

            //set value in dictionary
            lock (Id)
            {
                _data = TryModify(modify, _data);
                serializedBytes = Serialization.Serialize(_data);
            }
            
            //delegates internal logic to a thread, increasing performance
            return Delegate(() =>
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

        public async Task<T> ModifyAsync(ModifyValueDelegate<T> modify)
        {
            //allow waiting for modification to complete
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            
            //save remotely confirmed value
            T value = default;
            
            //wait for internal logic to be executed
            await Modify(modify, obj =>
            {
                //save value
                value = obj;
                
                //allow waiting thread to continue
                resetEvent.Set();
            });

            //wait for value to be confirmed by remote
            if (!resetEvent.WaitOne(Options.DefaultTimeout))
            {
                throw new TimeoutException($"Failed to modify {Id} within {Options.DefaultTimeout} ms!");
            }

            return value;
        }

        /// <summary>
        /// Try invoking the delegate, logging any exceptions which occur
        /// </summary>
        protected internal static T TryModify(ModifyValueDelegate<T> modify, T current)
        {
            try
            {
                return modify.Invoke(current);
            }
            catch (Exception e)
            {
                LogWriter.LogException(e);
                throw;
            }
        }
    }
}