using System.Collections.Generic;
using DMP.Networking.Messaging.Server;

namespace DMP.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer
    {
        private readonly Dictionary<MessageSession, Dictionary<string, Dictionary<string, List<uint>>>> _failedRequests
            = new Dictionary<MessageSession, Dictionary<string, Dictionary<string, List<uint>>>>();

        private void EnqueueFailedRequest(MessageSession session, string databaseId, string valueId, 
            uint expectedModCount)
        {
            //get list of mod counts of delayed requests
            List<uint> delayedModCounts = GetDelayedModCounts(session, databaseId, valueId);

            //add expected mod count to
            lock (delayedModCounts)
            {
                delayedModCounts.Add(expectedModCount);
            }
        }

        private bool TryDequeueFailedRequest(MessageSession session, string databaseId, string valueId,
            uint expectedModCount)
        {
            //get list of mod counts of delayed requests
            List<uint> delayedModCounts = GetDelayedModCounts(session, databaseId, valueId);

            //try removing it
            lock (delayedModCounts)
            {
                return delayedModCounts.Remove(expectedModCount);
            }
        }

        private List<uint> GetDelayedModCounts(MessageSession session, string databaseId, string valueId)
        {
            //get databases of sessions
            Dictionary<string, Dictionary<string, List<uint>>> databases;
            lock (_failedRequests)
            {
                //create database tracking if necessary
                if (!_failedRequests.TryGetValue(session, out databases))
                {
                    databases = new Dictionary<string, Dictionary<string, List<uint>>>();
                    _failedRequests.Add(session, databases);
                }
            }

            //get value of database
            Dictionary<string, List<uint>> values;
            lock (databases)
            {
                //create value tracking if necessary
                if (!databases.TryGetValue(databaseId, out values))
                {
                    values = new Dictionary<string, List<uint>>();
                    databases.Add(databaseId, values);
                }
            }
            
            //get delayed requests(mod counts) of valueId
            List<uint> delayedModCounts;
            lock (values)
            {
                if (!values.TryGetValue(valueId, out delayedModCounts))
                {
                    delayedModCounts = new List<uint>();
                    values.Add(valueId, delayedModCounts);
                }
            }

            return delayedModCounts;
        }
    }
}