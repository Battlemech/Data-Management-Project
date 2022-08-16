using System.Collections.Generic;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Tracks the locally expected mod count
        /// </summary>
        private readonly Dictionary<string, uint> _modificationCount = new Dictionary<string, uint>();
        
        /// <summary>
        /// Tracks which modification count was confirmed by the server
        /// </summary>
        private readonly Dictionary<string, uint> _confirmedModCount = new Dictionary<string, uint>();

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

        public uint GetModCount(string id)
        {
            lock (_modificationCount)
            {
                return _modificationCount.TryGetValue(id, out uint modCount) ? modCount : 0;
            }
        }
        
        private bool TryGetConfirmedModCount(string id, out uint modCount)
        {
            lock (_confirmedModCount) return _confirmedModCount.TryGetValue(id, out modCount);
        }
    }
}