using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {
        private readonly Dictionary<string, uint> _modCounts = new Dictionary<string, uint>();

        private uint IncrementModCount(string valueId)
        {
            //retrieve modCount from dictionary
            lock (_modCounts)
            {
                bool success = _modCounts.TryGetValue(valueId, out uint modCount);

                //increment mod count by 1
                if (!success) _modCounts[valueId] = 1;
                else _modCounts[valueId] = modCount + 1;
                
                return modCount;
            }
        }
    }
}