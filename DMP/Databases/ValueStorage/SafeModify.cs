namespace DMP.Databases.ValueStorage
{
    public partial class ValueStorage<T>
    {
        public void SafeModify(ModifyValueDelegate<T> modify) => Database.SafeModify(Id, modify);

        public T SafeModifySync(ModifyValueDelegate<T> modify, int timeout = Options.DefaultTimeout)
            => Database.SafeModifySync(Id, modify, timeout);
    }
}