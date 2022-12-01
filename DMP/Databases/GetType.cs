using System;
using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {
        //keeps track of all get attempts which failed to return an object
        private readonly Dictionary<string, Type> _failedGets = new Dictionary<string, Type>();
        
        private bool TryGetType(string id, out Type type)
        {
            //try retrieving type from currently saved objects
            if (_values.TryGetValue(id, out ValueStorage.ValueStorage valueStorage))
            {
                type = valueStorage.GetEnclosedType();
                return true;
            }

            //try retrieving type from failed get requests
            return _failedGets.TryGetValue(id, out type);
        }

        private bool TryGetType(string id)
        {
            return TryGetType(id, out _);
        }
    }
}