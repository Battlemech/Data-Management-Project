using DMP.Databases;

namespace DMP.Objects
{
    public interface ISynchronisedObject
    {
        public Database GetDatabase();
    }
}