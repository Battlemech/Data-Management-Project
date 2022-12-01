using System;
using DMP.Databases.ValueStorage;
using DMP.Networking.Synchronisation.Client;

namespace DMP.Databases
{
    public partial class Database
    {
        public bool IsHost => HostId.BlockingGet((hostId => hostId == Client.Id));
        public ValueStorage<Guid> HostId => Get<Guid>(nameof(HostId));
        public ValueStorage<bool> HostPersistence => Get<bool>(nameof(HostPersistence));
        public ValueStorage<bool> ClientPersistence => Get<bool>(nameof(ClientPersistence));

        public SynchronisedClient Client
        {
            get => _client;
            set
            {
                //transfer management of this database from one client to another
                _client?.RemoveDatabase(this);
                value?.AddDatabase(this);

                //update local value
                _client = value;
            }
        }
        private SynchronisedClient _client;

        
        private void ConfigureSynchronisedPersistence()
        {
            //set hostId to current database if no other client claimed host-privileges
            if (HostId.BlockingGet((hostId => hostId == default)))
            {
                //set host id to this client if it is still the default one
                SafeModify<Guid>(nameof(HostId), value => value == default ? Client.Id : value);   
            }

            /*
             * Specify unique parameter because synchronisation may be enabled, disabled and enabled again,
             * triggering AddCallback() again
             */
            
            //If isHost: change persistence
            HostPersistence.AddCallback((value =>
            {
                //if host id was initialised and local client is host
                if (HostId.BlockingGet((hostId) => hostId != default && hostId == Client.Id))
                {
                    IsPersistent = value;
                }
            }), $"SYSTEM/INTERNAL/{nameof(HostPersistence)}", unique:true);
            
            //if isClient: change persistence
            ClientPersistence.AddCallback((value =>
            {
                
                //if host id was initialised and local client is not host
                if (HostId.BlockingGet((hostId) => hostId != default && hostId != Client.Id))
                {
                    IsPersistent = value;
                }
            }), $"SYSTEM/INTERNAL/{nameof(ClientPersistence)}", unique:true);

            //update persistence when host changes
            AddCallback<Guid>(nameof(HostId), (hostId =>
            {
                //host id wasn't initialised yet
                if (hostId == default) return;

                HostPersistence.InvokeAllCallbacks();
                ClientPersistence.InvokeAllCallbacks();
            }), $"SYSTEM/INTERNAL/{nameof(HostId)}", true, unique:true);
        }
    }
}