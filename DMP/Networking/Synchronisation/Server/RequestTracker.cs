using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DMP.Submodules.NetCoreServer;

namespace DMP.Networking.Synchronisation.Server
{
    public class RequestTracker
    {
        //Maps[ValueId, [ModCount, Session]]
        private readonly Dictionary<string, Dictionary<uint, TcpSession>> _trackedValues =
            new Dictionary<string, Dictionary<uint, TcpSession>>();

        private readonly List<Dictionary<string, uint>> _delayedDeleteRequests = new List<Dictionary<string, uint>>();

        public void EnqueueDelayedSetRequest(string valueId, uint modCount, TcpSession session)
        {
            //dictionary containing the failed modification requests for the expected modification count
            Dictionary<uint, TcpSession> failedRequests;
            
            lock (_trackedValues)
            {
                if (!_trackedValues.TryGetValue(valueId, out failedRequests))
                {
                    failedRequests = new Dictionary<uint, TcpSession>();
                    _trackedValues.Add(valueId, failedRequests);
                }
            }

            lock (failedRequests)
            {
                failedRequests.Add(modCount, session);
            }
        }

        public bool TryRemoveDelayedSetRequest(string valueId, uint modCount, TcpSession session, out bool deleteDatabase)
        {
            deleteDatabase = false;
            
            //try get value
            Dictionary<uint, TcpSession> failedRequests;
            lock (_trackedValues)
            {
                if (!_trackedValues.TryGetValue(valueId, out failedRequests)) return false;
            }

            //try get session requesting change for mod count
            lock (failedRequests)
            {
                failedRequests.TryGetValue(modCount, out var savedSession);
                
                //another session was granted expected mod count
                if (session != savedSession) return false;

                failedRequests.Remove(modCount);
            }

            //try processing delayed delete-database requests
            lock (_delayedDeleteRequests)
            {
                if (_delayedDeleteRequests.Count == 0) return true;

                foreach (Dictionary<string,uint> delayedDeleteRequest in new List<Dictionary<string, uint>>(_delayedDeleteRequests))
                {
                    //check if the SetValueMessage was the one the delayed delete request was waiting for
                    if (delayedDeleteRequest.TryGetValue(valueId, out uint value) && modCount >= value)
                        //if it is, remove it
                        delayedDeleteRequest.Remove(valueId);
                    
                    //more delayed sets need to be processed
                    if(delayedDeleteRequest.Count != 0) continue;

                    //all sets were processed. Delete database now
                    deleteDatabase = true;
                    _delayedDeleteRequests.Remove(delayedDeleteRequest);
                }
            }

            return true;
        }

        public bool TryDeleteDatabase()
        {
            //dictionary contains the mod count for all ValueIds which needs to be reached. Once reached, database may be deleted
            Dictionary<string, uint> requiredModCount = new Dictionary<string, uint>();
            
            //lock it to prevent request being enqueued/dequeued for this database while lookup is active
            lock (_trackedValues)
            {
                //iterate through each valueStorage mod count tracker
                foreach (var kv in _trackedValues)
                {
                    Dictionary<uint, TcpSession> failedRequests = kv.Value;
                    lock (failedRequests)
                    {
                        //no delayed requests exist. Continue looking
                        if(failedRequests.Count == 0) continue;
                    
                        //a failed set request exists. Must wait for it to be processed before deleting the database
                        string valueId = kv.Key;
                        uint modCount = failedRequests.Keys.Max();
                        
                        requiredModCount.Add(valueId, modCount);
                    }
                }
                
                //no delayed requests were found
                if (requiredModCount.Count == 0) return true;

                //add delayed delete request
                lock (_delayedDeleteRequests)
                {
                    _delayedDeleteRequests.Add(requiredModCount);
                }
            
                return false;
            }
        }
    }
}