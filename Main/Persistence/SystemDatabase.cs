using System;
using Main.Databases;

namespace Main.Persistence
{
    public static class SystemDatabase
    {
        private static readonly Database Database = new Database("SYSTEM/INTERNAL/", isPersistent: true);

        public static Guid LoadGuid()
        {
            string id = "GUID";
            Guid guid = Database.Get<Guid>(id);

            //generate new guid if necessary
            if (guid == default)
            {
                guid = new Guid();
                Database.Set(id, guid);
            }

            return guid;
        }
    }
}