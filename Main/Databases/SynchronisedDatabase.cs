using System.Collections.Generic;
using Main.Networking.Synchronisation.Messages;

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
            SetValueRequest request = new SetValueRequest()
            {
                DatabaseId = Id,
                ValueId = id,
                ModCount = IncrementModCount(id),
                Value = value
            };

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                if(reply.Success) return;
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
            lock (_values)
            {
                if(_values.Count == 0) return;
                
                //todo: synchronise
            }
        }
    }
}