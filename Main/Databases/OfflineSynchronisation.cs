using System;
using System.Collections.Generic;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Invoked when a value was modified while no connection was established.
        /// </summary>
        private void OnOfflineModification(string id, byte[] value)
        {
            //prevent modification of hostId if offline
            if(id == nameof(HostId)) return;
            
            OnInitialized<Guid>(nameof(HostId), (guid =>
            {
                bool isHost = guid == Client.Id;
                
                if (!isHost) return;
                
                //if host modified data without connection: Synchronise it
                OnSetSynchronised(id, value);
            }));
        }
    }
}