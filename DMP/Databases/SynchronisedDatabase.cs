using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DMP.Databases.Utility;
using DMP.Databases.ValueStorage;
using DMP.Networking;
using DMP.Networking.Synchronisation.Client;
using DMP.Networking.Synchronisation.Messages;
using DMP.Objects;
using DMP.Threading;
using DMP.Utility;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Saves all data which could not be deserialized on a remote set
        /// </summary>
        private readonly Dictionary<string, Tuple<byte[], Type>> _serializedData =
            new Dictionary<string, Tuple<byte[], Type>>();

        public bool IsSynchronised
        {
            get => _isSynchronised;
            set
            {
                _isSynchronised = value;
                
                //enable synchronisation if necessary
                if(!value) return;
                
                //if no client was set, use the default instance
                if (Client == null)
                {
                    if (SynchronisedClient.Instance == null) throw new InvalidOperationException(
                        $"No synchronised Client exists which could manage synchronised database {Id}");
                
                    Client = SynchronisedClient.Instance;
                }

                //if client is not connected: Mark this database for later synchronisation
                if (!Client.IsConnected)
                {
                    Client.OnFailedSynchronise(this);
                    return;
                }
                
                OnConnectionEstablished();
            }
        }
        private bool _isSynchronised;

        protected internal void OnConnectionEstablished()
        {
            //try resolving HostId
            ConfigureSynchronisedPersistence();

            //return if there are no values to synchronise
            if(_values.Count == 0) return;
            
            Delegation.DelegateAction((() =>
            {
                //return if there are no values to synchronise
                if (_values.Count == 0) return;

                foreach (var vs in _values.Values)
                {
                    OnOfflineModification(vs.Id, vs.Serialize(out Type type), type);
                }
            }));
        }
        
        /// <summary>
        /// Invoked when a value is set
        /// </summary>
        private void OnSetSynchronised(string id, byte[] value, Type type)
        {
            SetValueRequest request = new SetValueRequest(Id, id, GetModCount(id), value, type);

            Client.SendRequest<SetValueRequest, SetValueReply>(request, (reply) =>
            {
                
            });
        }

        protected internal void OnRemoteSet(string id, byte[] value, Type type, uint modCount, bool incrementModCount)
        {
            
        }
    }
}