using System;
using System.Collections.Generic;
using System.Linq;
using Main.Networking.Messaging.Server;
using Main.Networking.Synchronisation.Messages;
using Main.Submodules.NetCoreServer;

namespace Main.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer
    {
        protected override void OnConnected(MessageSession session)
        {
            //copy modCount dictionary to allow modification to other
            Dictionary<string, Dictionary<string, uint>> modCount;

            //copy mod count
            lock (_modCount)
            {
                modCount = new Dictionary<string, Dictionary<string, uint>>(_modCount);
            }

            //send a request for each database
            foreach (var kv in modCount)
            {
                //skip databases without any modifications
                if(kv.Value.Count == 0) continue;
                
                //informing them of the current modCount of a value in a database
                GetValueRequest request = new GetValueRequest() { DatabaseId = kv.Key, ModificationCount = kv.Value };
                
                //replies contain all values which are currently not being modified on the client
                SendRequestsToOthers<GetValueRequest, GetValueReply>(request, session, replies =>
                {
                    
                    //get all received replies
                    List<SetValueMessage> setValueMessages = new List<SetValueMessage>();
                    foreach (var reply in replies)
                    {
                        //filter empty replies
                        if(reply == null) continue;
                        
                        setValueMessages.AddRange(reply.SetValueMessages);
                    }

                    //filter duplicate SetValueMessages and messages with a lower modCount
                    foreach (var message in FilterMessages(setValueMessages))
                    {
                        //forward them to the newly connected client
                        session.SendMessage(message);
                    }
                });   
            }
        }

        private List<SetValueMessage> FilterMessages(List<SetValueMessage> messages)
        {
            Dictionary<string, SetValueMessage> filteredMessages = new Dictionary<string, SetValueMessage>();

            foreach (var message in messages)
            {
                string id = message.ValueId;
                
                //save a value if no previous record of that id exists
                if (!filteredMessages.TryGetValue(id, out SetValueMessage current))
                {
                    filteredMessages[id] = message;
                    continue;
                }
                
                //currently saved mod count is equal or higher -> saved message is up to date -> don't overwrite
                if(current.ModCount >= message.ModCount) continue;

                filteredMessages[id] = message;
            }

            return filteredMessages.Values.ToList();
        }
    }
}