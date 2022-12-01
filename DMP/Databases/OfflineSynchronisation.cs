using System;
using DMP.Utility;

namespace DMP.Databases
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

            //wait for hostId to be synchronised in network
            OnInitialized<Guid>(nameof(HostId), (guid =>
            {
                bool isHost = guid == Client.Id;
                
                //todo: synchronise data for client if host didn't change anything: SafeModify, get current modCount?
                if (!isHost) return;

                //if host modified data without connection: Synchronise it
                OnSetSynchronised(id, value);
            }));
        }
    }
}