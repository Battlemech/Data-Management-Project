using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, uint> _modCountsLocal = new Dictionary<string, uint>();
        private readonly Dictionary<string, uint> _modCountsRemote = new Dictionary<string, uint>();

        private uint IncrementModCount(string valueId, bool local)
        {
            //access local or remote mod count tracking
            Dictionary<string, uint> dict = local ? _modCountsLocal : _modCountsRemote;
            
            //retrieve modCount from dictionary
            lock (dict)
            {
                dict.TryGetValue(valueId, out uint modCount);

                //increment mod count by 1
                modCount++;
                
                //update mod count
                dict[valueId] = modCount;
                
                return modCount;
            }
        }

        private uint GetModCount(string valueId, bool local)
        {
            //access local or remote mod count tracking
            Dictionary<string, uint> dict = local ? _modCountsLocal : _modCountsRemote;
            
            lock (dict)
            {
                return dict.TryGetValue(valueId, out uint modCount) ? modCount : 0;
            }
        }

        private void SetModCount(string valueId, uint modCount, bool local)
        {
            //access local or remote mod count tracking
            Dictionary<string, uint> dict = local ? _modCountsLocal : _modCountsRemote;
            
            lock (dict)
            {
                dict[valueId] = modCount;
            }
        }
    }
}