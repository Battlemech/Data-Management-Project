using System;

namespace Main.Databases
{
    public partial class Database
    {
        public void SafeModify<T>(string id, ModifyValueDelegate<T> modify)
        {
            throw new NotImplementedException();
        }
    }
}