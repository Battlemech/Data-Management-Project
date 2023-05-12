using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Databases.ValueStorage;
using DMP.Networking;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases
{
    public delegate void OnSafeModification<T>(T value);
    
    public partial class Database
    {
        /// <summary>
        /// Waits until the server grants access to the value until the operation is executed.
        /// This ensures that the "modify" delegate is executed exactly once.
        /// Slower than Modify() because current modCount is requested from server. Use Modify() if possible
        /// <list type="bullet"> <item>
        /// Warning: Operation can produce inconsistent values if any operation except SafeModify() is used
        /// for the same id while the modification process is ongoing</item> </list> </summary>
        /// <remarks>
        /// The value will not be set locally after this function was called, only once the server allowed the operation.
        /// If you want to make sure that the logic you execute happens after the value was set
        /// include it in the "modify" delegate or use SafeModifySync().
        /// </remarks>
        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify)
        {
            
        }

        public T SafeModifySync<T>(string id, ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
        {
            ManualResetEvent modificationExecuted = new ManualResetEvent(false);
            T current = default;
            
            SafeModify<T>(id, (value =>
            {
                //invoke modification operation
                current = modify.Invoke(value);
                
                //signal waiting thread that modification commenced
                modificationExecuted.Set();

                return current;
            }));
            
            if(modificationExecuted.WaitOne(timeout)) return current;

            throw new TimeoutException($"Failed to execute modify operation within {timeout} ms!");
        }
    }
}