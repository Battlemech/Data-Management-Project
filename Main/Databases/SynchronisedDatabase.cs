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

        private readonly Dictionary<string, uint> _modificationCount = new Dictionary<string, uint>();

        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value)
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

            OnSetSynchronised(id, value, modCount);
        }

        /// <summary>
        /// Invoked when a value is loaded by the persistence module.
        /// The value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            //todo: request change from server. Change instantly if host
        }
        
        private void OnSetSynchronised(string id, byte[] value, uint modCount)
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