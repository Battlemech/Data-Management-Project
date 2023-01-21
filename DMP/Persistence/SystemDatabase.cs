using System;
using DMP.Databases;
using DMP.Databases.VS;

namespace DMP.Persistence
{
    public static class SystemDatabase
    {
        private const string Id = "SYSTEM/INTERNAL/";
        private static readonly Database Database = new Database(Id, isPersistent: true);

        private static ValueStorage<Guid> _guid => Database.Get<Guid>(nameof(_guid));

        public static Guid GetGuid()
        {
            Guid guid = _guid.Get();

            //guid was saved persistently
            if (guid != default) return guid;
            
            //generate new guid
            guid = new Guid();
            
            //save it
            _guid.Set(guid);
            Database.Save();

            return guid;
        }

        public static void DeleteData() => PersistentData.DeleteDatabase(Id);
    }
}