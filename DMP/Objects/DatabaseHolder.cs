using DMP.Databases;

namespace DMP.Objects
{
    public abstract class DatabaseHolder
    {
        public readonly string Id;

        protected DatabaseHolder(string id, bool isPersistent, bool isSynchronised)
        {
            Id = id;
        }

        protected DatabaseHolder(Database database)
        {
            Id = database.Id;
        }
    }
}