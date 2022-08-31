using System.Collections.Generic;
using Main.Networking.Synchronisation.Messages;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Contains a list of failed set requests, containing the expected modification count
        /// </summary>
        private readonly Dictionary<string, Queue<SetValueRequest>> _failedRequests =
            new Dictionary<string, Queue<SetValueRequest>>();

        /// <summary>
        /// Returns the number of value synchronisation tasks which are still ongoing 
        /// </summary>
        public int GetOngoingSets(string id)
        {
            lock (_failedRequests)
            {
                return !_failedRequests.TryGetValue(id, out var requests) ? 0 : requests.Count;
            }
        }

        private void EnqueueFailedRequest(SetValueRequest request)
        {
            string id = request.ValueId;
            
            lock (_failedRequests)
            {
                if (!_failedRequests.TryGetValue(id, out Queue<SetValueRequest> requests))
                {
                    requests = new Queue<SetValueRequest>();
                    _failedRequests.Add(id, requests);
                }
                    
                requests.Enqueue(request);
            }
        }

        private bool TryDequeueFailedRequest(string id, uint maxModCount, out SetValueRequest request)
        {
            request = null;
            
            lock (_failedRequests)
            {
                //no queue with requested id exists
                if (!_failedRequests.TryGetValue(id, out Queue<SetValueRequest> requests)) return false;

                //queue is empty
                if (requests.Count == 0) return false;

                //request needs to be processed later
                if(requests.Peek().ModCount > maxModCount) return false;

                //dequeue delayed request
                request = requests.Dequeue();
            }

            return true;
        }
    }
}