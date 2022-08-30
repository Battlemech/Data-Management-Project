using System;

namespace Main.Databases
{
    public partial class Database
    {
        public Guid HostId => Get<Guid>(nameof(HostId));
        public bool IsHost => Client.Id == HostId;

        public bool HostPersistence
        {
            get => Get<bool>(nameof(HostPersistence));
            set => Set(nameof(HostPersistence), value);
        }

        public bool ClientPersistence
        {
            get => Get<bool>(nameof(ClientPersistence));
            set => Set(nameof(ClientPersistence), value);
        }

        private void ConfigureSynchronisedPersistence()
        {
            //set hostId to current database if no other client claimed host-privileges
            if (HostId == default)
            {
                SafeModify<Guid>(nameof(HostId), value =>
                {   
                    //set host id to this client if it is still the default one
                    Guid hostId = (value == default) ? Client.Id : value;
                    
                    //if IsHost: Configure global HostPersistence and ClientPersistence
                    if (hostId == Client.Id)
                    {
                        HostPersistence = IsPersistent;
                        ClientPersistence = IsPersistent && Options.DefaultClientPersistence;
                    }
                    
                    return hostId;
                });   
            }

            //If isHost: change persistence
            AddCallback<bool>(nameof(HostPersistence), value =>
            {
                if(!IsHost) return;
                IsPersistent = value;
            }, $"SYSTEM/INTERNAL/{nameof(HostPersistence)}", unique:true);
            
            //if isClient: change persistence
            AddCallback<bool>(nameof(ClientPersistence), value =>
            {
                if(IsHost) return;
                IsPersistent = value;
            }, $"SYSTEM/INTERNAL/{nameof(ClientPersistence)}", unique:true);
            
            //change persistence when host changes
            AddCallback<Guid>(nameof(HostId), (value =>
            {
                //invoke callback
                InvokeCallbacks(value == Client.Id ? nameof(HostPersistence) : nameof(ClientPersistence));
            }), $"SYSTEM/INTERNAL/{nameof(HostId)}", true, unique:true);
        }
    }
}