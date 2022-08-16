﻿using System;
using System.Collections.Generic;

namespace Main.Databases
{
    public partial class Database
    {
        /// <summary>
        /// Tracks how many replies are still being awaited by the client
        /// </summary>
        private readonly Dictionary<string, int> _pendingReplies = new Dictionary<string, int>();

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
            
            //clear any saved values for remaining pending replies
            lock (_confirmedValues)
            {
                _confirmedValues.Remove(id);
            }
        }

        private bool RepliesPending(string id)
        {
            lock (_pendingReplies)
            {
                return _pendingReplies.ContainsKey(id);
            }
        }
    }
}