using System;
using System.Collections.Generic;

namespace Main.Databases
{
    public partial class Database
    {
        //keeps track of all get attempts which failed to return an object
        private readonly Dictionary<string, Type> _failedGets = new Dictionary<string, Type>();
        
        private bool TryGetType(string id, out Type type)
        {
            //try retrieving type from currently saved objects
            if (_values.TryGetValue(id, out object current) && current != null)
            {
                type = current.GetType();
                return true;
            }
            
            //try retrieving type from failed get requests
            if (_failedGets.TryGetValue(id, out type)) return true;

            //try retrieving type from callbacks
            return _callbackHandler.TryGetType(id, out type);
        }

        private bool TryGetType(string id)
        {
            return TryGetType(id, out _);
        }
    }
}