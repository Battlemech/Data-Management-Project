using System;
using System.Collections.Generic;
using Main.Networking.Synchronisation;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

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

        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value)
        {
            uint modCount = IncrementModCount(id);
            
            SetValueRequest request = new SetValueRequest()
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = modCount,
                Value = value
            };

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                //if request was successful: return
                if(reply.ExpectedModCount == modCount) return;
            });
        }

        /// <summary>
        /// Invoked when a value is loaded by the persistence module.
        /// The value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            //todo: request change from server. Change instantly if host
        }
        
        private void OnModifyValueSynchronised<T>(string id, byte[] value, ModifyValueDelegate<T> modify)
        {
            
        }

        private void OnSynchronisationEnabled()
        {
            //if no client was set, use the default instance
            if (Client == null)
            {
                if (SynchronisedClient.Instance == null) throw new Exception(
                    $"No synchronised Client exists which could manage synchronised database {Id}");
                
                Client = SynchronisedClient.Instance;
            }

            lock (_values)
            {
                if(_values.Count == 0) return;
                
                //todo: synchronise
            }
        }

        protected internal void OnRemoveSet(string id, byte[] value, uint modCount)
        {
            lock (_values)
            {
                _values[id] = Serialization.Deserialize<object>(value);
            }
         
            //invoke callbacks
            _callbackHandler.InvokeCallbacks(id, value);
            
            //invoke persistent data saving if required
            if(_isPersistent) OnSetPersistent(id, value);
            
            //todo: make use of mod count
        }
    }
}