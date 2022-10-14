using DMP.Objects;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Return true if the object was already known locally, or false it was referenced for the first time
        /// </summary>
        protected internal bool TryTrackObject(SynchronisedObject synchronisedObject)
        {
            lock (_objects)
            {
                if(_objects.Contains(synchronisedObject)) return false;
                _objects.Add(synchronisedObject);
            }

            return true;
        }
    }
}