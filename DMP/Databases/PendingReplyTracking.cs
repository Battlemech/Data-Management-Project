using System;
using System.Collections.Generic;

namespace DMP.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Tracks how many replies are still being awaited by the client
        /// </summary>
        private readonly Dictionary<string, int> _pendingReplies = new Dictionary<string, int>();

        /// <summary>
        /// Serialized confirmed bytes by remote.
        /// Only saved while replies are pending
        /// </summary>
        private readonly Dictionary<string, byte[]> _confirmedValues = new Dictionary<string, byte[]>();
        
        private void IncrementPendingCount(string id)
        {
            lock (_pendingReplies)
            {
                if (!_pendingReplies.TryGetValue(id, out int pendingCount))
                {
                    pendingCount = 0;
                }

                _pendingReplies[id] = pendingCount + 1;
            }
        }

        private void DecrementPendingCount(string id)
        {
            lock (_pendingReplies)
            {
                if (!_pendingReplies.TryGetValue(id, out int pendingCount) || pendingCount == 0)
                {
                    throw new Exception($"Tried to decrement the pending reply count of {id} below 0!");
                }

                //decrement pending replies
                if (pendingCount != 1)
                {
                    _pendingReplies[id] = pendingCount - 1;
                    return;
                }
                
                //decrement to 0. Remove entry
                _pendingReplies.Remove(id);
            }
        }

        private bool RepliesPending(string id)
        {
            lock (_pendingReplies)
            {
                return _pendingReplies.ContainsKey(id);
            }
        }
        
        private byte[] GetConfirmedValue(string id)
        {
            lock (_confirmedValues)
            {
                if (_confirmedValues.TryGetValue(id, out byte[] value)) return value;
            }

            throw new InvalidOperationException($"No value saved with id {id}");
        }
    }
}