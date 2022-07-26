﻿using System.Collections.Generic;
using Main.Networking.Synchronisation.Messages;

namespace Main.Networking.Synchronisation
{
    public partial class SynchronisedServer
    {
        private readonly Dictionary<string, Dictionary<string, uint>> _modCount =
            new Dictionary<string, Dictionary<string, uint>>();

        public uint GetModCount(string databaseId, string valueId)
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
                    return 0;
                }
            }

            //retrieve modCount from dictionary
            lock (modCounts)
            {
                bool success = modCounts.TryGetValue(valueId, out uint modCount);
                return !success ? 0 : modCount;
            }
        }

        private void IncrementModCount(string databaseId, string valueId)
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
                bool success = modCounts.TryGetValue(valueId, out uint modCount);

                //increment mod count by 1
                if (!success) modCounts[valueId] = 1;
                else modCounts[valueId] = modCount + 1;
            }
        }
    }
}