using System.Collections.Generic;
using Main.Networking.Synchronisation;

namespace Main.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, uint> _modificationCount = new Dictionary<string, uint>();

        public SynchronisedClient Client
        {
            get => _client;
            set
            {
                //transfer management of this database from one client to another
                _client?.RemoveDatabase(this);
                value.AddDatabase(this);

                //update local value
                _client = value;
            }
        }
        private SynchronisedClient _client;

        /// <summary>
        /// Increase modification count by 1 after retrieving it
        /// </summary>
        private uint IncrementModCount(string id)
        {
            //increase modification count by one
            uint modCount;
            lock (_modificationCount)
            {
                bool success = _modificationCount.TryGetValue(id, out modCount);

                if (success)
                {
                    _modificationCount[id] = modCount + 1;
                }
                else
                {
                    _modificationCount.Add(id, 1);
                }
            }

            return modCount;
        }
        
    }
}