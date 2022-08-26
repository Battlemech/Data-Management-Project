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

        private void UpdateModCount(string id, uint remoteModCount)
        {
            lock (_modificationCount)
            {
                //if no mod count was found: init with value 0
                if (!_modificationCount.TryGetValue(id, out uint modCount)) modCount = 0;
                
                //locally saved modCount is bigger or equal to then remote modCount
                if (modCount >= remoteModCount)
                {
                    _modificationCount[id] = modCount + 1;
                    return;
                }
                
                //remote mod count is bigger then locally saved one
                _modificationCount[id] = remoteModCount;
            }
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