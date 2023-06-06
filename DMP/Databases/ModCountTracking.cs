using System;
using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Tracks the locally expected mod count
        /// </summary>
        private readonly Dictionary<string, uint> _modificationCount = new Dictionary<string, uint>();

        /// <summary>
        /// Tracks which values were confirmed by server
        /// </summary>
        private readonly Dictionary<string, ConfirmedValue> _confirmedValues =
            new Dictionary<string, ConfirmedValue>();

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

        private void UpdateConfirmedValues(string id, uint modCount, byte[] value, Type type)
        {
            lock (_confirmedValues)
            {
                _confirmedValues[id] = new ConfirmedValue(modCount, value, type);
            }
        }

        private bool TryGetConfirmedModCount(string id, out uint modCount)
        {
            lock (_confirmedValues)
            {
                if (_confirmedValues.TryGetValue(id, out ConfirmedValue value))
                {
                    modCount = value.ModCount;
                    return true;
                }

                modCount = default;
                return false;
            }
        }
        
        private bool TryGetConfirmedValue(string id, out uint modCount, out byte[] bytes, out Type type)
        {
            lock (_confirmedValues)
            {
                //value exists
                if (_confirmedValues.TryGetValue(id, out ConfirmedValue confirmed))
                {
                    modCount = confirmed.ModCount;
                    bytes = confirmed.Value;
                    type = confirmed.Type;
                    return true;
                }

                //value doesn't exist
                modCount = default;
                bytes = default;
                type = default;
                return false;
            }
        }
        
        private struct ConfirmedValue
        {
            public readonly uint ModCount;
            public readonly byte[] Value;
            public readonly Type Type;

            public ConfirmedValue(uint modCount, byte[] value, Type type)
            {
                ModCount = modCount;
                Value = value;
                Type = type;
            }
        }
    }
}