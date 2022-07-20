using System;
using System.Collections.Generic;

namespace Main.Databases
{
    public partial class Database
    {
        public bool IsSynchronised
        {
            get => _isSynchronised;
            set
            {
                //do nothing if database is (not) synchronised already
                if (value == _isSynchronised) return;

                _isSynchronised = value;
                
                //enable synchronisation if necessary
                if(value) OnSynchronisationEnabled();
            }
        }

        private bool _isSynchronised;

        private readonly Dictionary<string, ulong> _modificationCount = new Dictionary<string, ulong>();

        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value)
        {
            //increase modification count by one
            ulong modCount;
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
                
                Console.WriteLine($"{id}: Mod count = {modCount}. Found: {success}");
            }

            OnSetSynchronised(id, value, modCount);
        }

        /// <summary>
        /// Invoked when a value is loaded by the persistence module
        /// </summary>
        private void OnLoaded(string id, byte[] value, ulong modCount)
        {
            //update local mod count
            lock (_modificationCount)
            {
                bool success = _modificationCount.TryAdd(id, modCount);

                if (!success)
                {
                    _modificationCount[id] = Math.Max(_modificationCount[id], modCount);
                }
            }
            
            //todo: add is-synchronised bool to table, tracking if data was synchronised or not
            
            OnSetSynchronised(id, value, modCount);
        }
        
        private void OnSetSynchronised(string id, byte[] value, ulong modCount)
        {
            
        }

        private void OnSynchronisationEnabled()
        {
            lock (_values)
            {
                if(_values.Count == 0) return;
                
                //todo: synchronise
            }
        }
    }
}