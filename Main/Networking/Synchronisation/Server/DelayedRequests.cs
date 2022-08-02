using System.Collections.Generic;
using Main.Networking.Synchronisation.Messages;
using Main.Submodules.NetCoreServer;

namespace Main.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<uint, TcpSession>>> _delayedRequests =
            new Dictionary<string, Dictionary<string, Dictionary<uint, TcpSession>>>();

        private void EnqueueDelayedSetRequest(SetValueRequest request, TcpSession session)
            => EnqueueDelayedSetRequest(request.DatabaseId, request.ValueId, request.ModCount, session);
        
        private void EnqueueDelayedSetRequest(string databaseId, string valueId, uint modCount, TcpSession session)
        {
            Dictionary<string, Dictionary<uint, TcpSession>> trackedValues;
            
            //start tracking database if necessary
            lock (_delayedRequests)
            {
                if (!_delayedRequests.TryGetValue(databaseId, out trackedValues))
                {
                    trackedValues = new Dictionary<string, Dictionary<uint, TcpSession>>();
                    _delayedRequests.Add(databaseId, trackedValues);
                }
            }
            
            //dictionary containing the failed modification requests for the expected modification count
            Dictionary<uint, TcpSession> failedRequests;
            
            lock (trackedValues)
            {
                if (!trackedValues.TryGetValue(valueId, out failedRequests))
                {
                    failedRequests = new Dictionary<uint, TcpSession>();
                    trackedValues.Add(valueId, failedRequests);
                }
            }

            lock (failedRequests)
            {
                failedRequests.Add(modCount, session);
            }
        }

        private bool TryRemoveDelayedRequest(SetValueMessage message, TcpSession session)
        {
            string databaseId = message.DatabaseId;
            string valueId = message.ValueId;
            uint modCount = message.ModCount;

            //try get database
            Dictionary<string, Dictionary<uint, TcpSession>> trackedValues;
            lock (_delayedRequests)
            {
                if (!_delayedRequests.TryGetValue(databaseId, out trackedValues)) return false;
            }

            //try get value
            Dictionary<uint, TcpSession> failedRequests;
            lock (trackedValues)
            {
                if (!trackedValues.TryGetValue(valueId, out failedRequests)) return false;
            }

            //try get session requesting change for mod count
            lock (failedRequests)
            {
                failedRequests.TryGetValue(modCount, out var savedSession);
                
                //another session was granted expected mod count
                if (session != savedSession) return false;

                failedRequests.Remove(modCount);
                return true;
            }
            
        }
    }
}