using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DMP.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer
    {
        private readonly Dictionary<string, Dictionary<string, uint>> _modCount =
            new Dictionary<string, Dictionary<string, uint>>();

        /// <summary>
        /// Increase modification count by 1 after retrieving it
        /// </summary>
        private uint IncrementModCount(string databaseId, string valueId)
        {
            Dictionary<string, uint> modCounts;

            //access global database reference
            lock (_modCount)
            {
                bool success = _modCount.TryGetValue(databaseId, out modCounts);

                //database wasn't tracked jet
                if (!success)
                {
                    modCounts = new Dictionary<string, uint>();
                    _modCount.Add(databaseId, modCounts);
                }
            }

            //retrieve modCount from dictionary
            lock (modCounts)
            {
                modCounts.TryGetValue(valueId, out uint modCount);

                //increment mod count by 1
                modCount++;
                
                //update modCount locally
                modCounts[valueId] = modCount;
                
                return modCount;
            }
        }
    }
}