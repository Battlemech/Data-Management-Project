﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Main.Networking.Synchronisation.Messages;
using Main.Utility;

namespace Main.Databases
{
    public partial class Database
    {
        protected internal void OnRemoteGet(Dictionary<string, uint> remoteModCounts, Action<List<SetValueMessage>> getDelegate)
        {
            List<SetValueMessage> messages = new List<SetValueMessage>();
            
            //copy current values to prevent modification during lookup
            Dictionary<string, object> values = new Dictionary<string, object>(_values);
            
            //track amount of completed lookups
            int completedLookups = 0;
            
            foreach (var id in remoteModCounts.Keys)
            {
                Scheduler.QueueTask(id, (() =>
                {
                    uint localModCount = GetModCount(id);
                    uint serverModCount = remoteModCounts[id];
                    Type type = null;
                    
                    //local modification count doesn't equal the servers -> Value can't be safely retrieved
                    bool success = serverModCount == localModCount;

                    //object cant be serialized because the type is unknown ->
                    //Occurs if object is default -> null -> Will be automatically mirrored on remote database
                    success = success && TryGetType(id, out type);

                    //value can be loaded safely
                    lock (messages)
                    {
                        if (success)
                            messages.Add(new SetValueMessage(Id, id, serverModCount, Serialization.Serialize(type, values[id])));
                        
                        //other lookups still need to be completed. Wait
                        completedLookups++;
                        
                        if(completedLookups != values.Count) return;
                    }

                    //all lookups were completed
                    getDelegate.Invoke(messages);
                }));    
            }
            
            //delegate will always be invoked: remoteModCount will always be at least one: Server will not send request for empty database
            
        }
    }
}