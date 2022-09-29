using System.Collections.Generic;
using DMP.Databases;

namespace DMP.Networking.Synchronisation.Client
{
    public partial class SynchronisedClient
    {
        private readonly Queue<Database> _toSynchronise = new Queue<Database>();

        protected internal void OnFailedSynchronise(Database database)
        {
            //track database: It needs to be synchronised later
            lock (_toSynchronise)
            {
                //if client connected in meantime: continue establishing connection
                if (IsConnected)
                {
                    database.OnConnectionEstablished();
                    return;
                }
                
                if(_toSynchronise.Contains(database)) return;
                _toSynchronise.Enqueue(database);
            }
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            lock (_toSynchronise)
            {
                while (_toSynchronise.Count != 0)
                {
                    _toSynchronise.Dequeue().OnConnectionEstablished();
                }
            }
        }
    }
}