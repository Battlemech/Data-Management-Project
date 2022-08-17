﻿using System;
using System.Collections.Generic;
using Main.Networking.Synchronisation.Client;
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
        
        //keeps track of all get attempts which failed to return an object
        private readonly Dictionary<string, Type> _failedGets = new Dictionary<string, Type>();

        public SynchronisedClient Client
        {
            get => _client;
            set
            {
                //transfer management of this database from one client to another
                _client?.RemoveDatabase(this);
                value.AddDatabase(this);

                //update local value
                _client = value;
            }
        }
        private SynchronisedClient _client;

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

        private bool TryGetType(string id, out Type type)
        {
            //try retrieving type from currently saved objects
            if (_values.TryGetValue(id, out object current) && current != null)
            {
                type = current.GetType();
                return true;
            }
            
            //try retrieving type from failed get requests
            if (_failedGets.TryGetValue(id, out type)) return true;

            //try retrieving type from callbacks
            return _callbackHandler.TryGetType(id, out type);
        }

        private bool TryGetType(string id)
        {
            return TryGetType(id, out _);
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

        public override string ToString()
        {
            //todo: replace with: return _isSynchronised ? $"DB({Client})-{Id}" : $"DB-{Id}";
            return _isSynchronised ? $"{Client}" : $"DB-{Id}";
        }
    }
}