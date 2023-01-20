using System;
using DMP.Databases;

namespace DMP.Persistence
{
    public static class SystemDatabase
    {
        private const string Id = "SYSTEM/INTERNAL/";
        private static readonly Database Database = new Database(Id, isPersistent: true);

        public static Guid Guid
        {
            get
            {
                throw new NotImplementedException();
                //create new guid if necessary
                //Database.Modify("Guid", value => (value == default) ? Guid.NewGuid() : value, out Guid result);
                //return result
            }
        }

        public static void DeleteData() => PersistentData.DeleteDatabase(Id);
    }
}