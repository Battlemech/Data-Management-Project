using System;
using System.Collections.Generic;
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
        
        protected internal void OnLocalSet<T>(string valueId, byte[] value, Action<T> onConfirm)
        {
            if(!IsSynchronised) return;

            uint expected = IncrementModCount(valueId, true);
            
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
                if (reply.ExpectedModificationCount == expected)
                {
                    onConfirm?.Invoke(Serialization.Deserialize<T>(value));
                    return;
                }
                
                TrackFailedRequest(valueId, value, reply.ExpectedModificationCount, onConfirm);
            });
        }

        protected internal void OnLocalSet<T>(string valueId, byte[] value, SetValueDelegate<T> modifyValueDelegate, Action<T> onConfirm)
        {
            if(!IsSynchronised) return;

            uint expected = IncrementModCount(valueId, true);
            
            SetValueRequest request = new SetValueRequest()
            { 
                DatabaseId = Id,
                ValueId = valueId, 
                ModificationCount = expected, 
                Value = value
            };
            
            Console.WriteLine($"{this}: Sending request: {request}");
            
            _client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                //request was successful
                if (reply.ExpectedModificationCount == expected)
                {
                    onConfirm?.Invoke(Serialization.Deserialize<T>(value));
                    return;
                }

                Console.WriteLine($"{this}: Request failed: {request}");
                TrackFailedRequest(valueId, modifyValueDelegate, reply.ExpectedModificationCount, onConfirm);
            });
        }

        protected internal void OnRemoteSet(string valueId, byte[] value, uint modCount)
        {
            if(modCount <= GetModCount(valueId, false)) return;
            
            Console.WriteLine($"{this}- received remote set id={valueId}, modCount={modCount}");
            
            lock (_values)
            {
                //update local value if it exists
                if (_values.TryGetValue(valueId, out ValueStorage valueStorage))
                {
                    valueStorage.InternalSet(value);
                }
                else
                {
                    //allow delayed gets to retrieve and load data
                    lock (_serializedData)
                    {
                        _serializedData[valueId] = value;
                    }   
                }
            }
            
            //local value was updated. Increase mod count
            IncrementModCount(valueId, true);
            IncrementModCount(valueId, false);

            if(!TryDequeueFailedRequest(valueId, modCount + 1, out FailedRequest failedRequest)) return;

            Console.WriteLine($"{this}: executing delayed request: valueId={valueId}, modCount={failedRequest.ModCount}");
            
            //repeat modification
            value = failedRequest.RepeatModification(value);
            modCount = failedRequest.ModCount;
            
            //notify peers of new value
            _client.SendMessage(new SetValueMessage()
                { DatabaseId = Id, ValueId = valueId, Value = value, ModificationCount = modCount });
            
            OnRemoteSet(valueId, value, modCount);
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