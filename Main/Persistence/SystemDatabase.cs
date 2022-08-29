using System;
using Main.Databases;

namespace Main.Persistence
{
    public static class SystemDatabase
    {
        private const string Id = "SYSTEM/INTERNAL/";
        private static readonly Database Database = new Database(Id, isPersistent: true);

        public static Guid Guid
        {
            get
            {
                //create new guid if necessary
                Database.Modify("Guid", value => (value == default) ? Guid.NewGuid() : value, out Guid result);
                //return result
                return result;    
            }
        }

        public static void DeleteData() => PersistentData.DeleteDatabase(Id);
    }
}