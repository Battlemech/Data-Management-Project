using System.Collections.Generic;
using Main.Networking.Synchronisation;

namespace Main.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, uint> _modificationCount = new Dictionary<string, uint>();
        private SynchronisedClient Client => SynchronisedClient.Instance;
        
        private uint IncrementModCount(string id)
        {
            //increase modification count by one
            uint modCount;
            lock (_modificationCount)
            {
                bool success = _modificationCount.TryGetValue(id, out modCount);

                if (success)
                {
                    modCount += 1;
                    _modificationCount[id] = modCount;
                }
                else
                {
                    modCount = 1;
                    _modificationCount.Add(id, modCount);
                }
            }

            return modCount;
        }
        
    }
}