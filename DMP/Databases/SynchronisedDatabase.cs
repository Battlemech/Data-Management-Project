using System;
using DMP.Databases.VS;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;

namespace DMP.Databases
{
    public partial class Database
    {
        //Get local instance of default client if otherwise undefined
        public SynchronisedClient Client => _client ??= SynchronisedClient.Instance;
        private SynchronisedClient _client;
        
        protected internal void OnRemoteSet(string valueId, byte[] value, uint modCount)
        {
            lock (_values)
            {
                //update local value if it exists
                if (_values.TryGetValue(valueId, out ValueStorage valueStorage))
                {
                    valueStorage.InternalSet(value);
                    return;
                }

                //allow delayed gets to retrieve and load data
                lock (_serializedData)
                {
                    _serializedData[valueId] = value;
                }
            }
        }

        protected internal void OnRemoteModify(string valueId, byte[] value, uint modCount, CollectionOperation type)
        {
            throw new NotImplementedException();
        }
    }
}