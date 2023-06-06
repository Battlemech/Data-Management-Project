using System.Threading.Tasks;
using DMP.Threading;

namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        public void SafeModify(ModifyValueDelegate<T> modify) => Database.SafeModify(Id, modify);

        public T SafeModifySync(ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
            => Database.SafeModifySync(Id, modify, timeout);

        public Task<T> SafeModifyAsync(ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
            => Database.SafeModifyAsync(Id, modify, timeout);
    }
}