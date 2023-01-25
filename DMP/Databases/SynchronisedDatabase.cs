using System;
using DMP.Databases.VS;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        private SynchronisedClient _client;

        public bool IsSynchronised
        {
            get => _isSynchronised;
            set
            {
                //no update necessary
                if(value == _isSynchronised) return;

                _isSynchronised = value;
                
                //database will no longer synchronise values
                if(!value) return;
                
                //make sure client instance is assigned
                if(_client != null) return;

                if (SynchronisedClient.Instance == null)
                    throw new ArgumentException("Can't create synchronised database without synchronised client!");
                
                SetClient(SynchronisedClient.Instance);
            }
        }

        private bool _isSynchronised;
        
        protected internal void OnLocalSet(string valueId, byte[] value)
        {
            if(!IsSynchronised) return;

            uint expected = IncrementModCount(valueId);
            
            SetValueRequest request = new SetValueRequest()
                { 
                    DatabaseId = Id,
                    ValueId = valueId, 
                    ModificationCount = expected, 
                    Value = value
                };
            
            _client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                //request was successful
                if(reply.ExpectedModificationCount == expected) return;

                //todo eventually send SetValueMessage on failure
                throw new NotImplementedException();
            });
        }

        protected internal void OnLocalSet<T>(string valueId, byte[] value, SetValueDelegate<T> modifyValueDelegate)
        {
            if(!IsSynchronised) return;

            throw new NotImplementedException();
        }

        protected internal void OnRemoteSet(string valueId, byte[] value, uint modCount)
        {
            lock (_values)
            {
                Console.WriteLine($"{this}Remote update for: {valueId}. Values: {LogWriter.StringifyCollection(_values.Keys)}");
                
                //update local value if it exists
                if (_values.TryGetValue(valueId, out ValueStorage valueStorage))
                {
                    Console.WriteLine($"{this}Updating {valueId} from remote");
                    valueStorage.InternalSet(value);
                    return;
                }

                //allow delayed gets to retrieve and load data
                lock (_serializedData)
                {
                    Console.WriteLine($"{this}Delaying remote update of {valueId} until value is accessed");
                    _serializedData[valueId] = value;
                }
            }
        }

        /// <summary>
        /// Overwrites the default local client. Useful for testing.
        /// </summary>
        /// <param name="client"></param>
        public void SetClient(SynchronisedClient client)
        {
            _client = client;
            
            //add reference to this database to the client
            _client.AddDatabase(this);
        }

        public override string ToString()
        {
            return _client != null ? $"{_client}+Id={Id}" : $"Id={Id}";
        }
    }
}