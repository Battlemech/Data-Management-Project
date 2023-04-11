using System.Threading.Tasks;
using DMP.Threading;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        public void SafeModify(ModifyValueDelegate<T> modify) => Database.SafeModify(Id, modify);

        public T SafeModifySync(ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
            => Database.SafeModifySync(Id, modify, timeout);

        public Task<T> SafeModifyAsync(ModifyValueDelegate<T> modifyValueDelegate, int timeout = Options.DefaultTimeout)
        {
            //create task
            Task<T> task = new Task<T>((() => SafeModifySync(modifyValueDelegate, timeout)));
            
            //start executing it
            Delegation.DelegateTask(task);
            
            //return started task
            return task;
        }
    }
}