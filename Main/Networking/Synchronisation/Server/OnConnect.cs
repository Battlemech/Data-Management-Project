using System;
using System.Collections.Generic;
using System.Linq;
using Main.Networking.Messaging.Server;
using Main.Networking.Synchronisation.Messages;
using Main.Submodules.NetCoreServer;
using Main.Utility;

namespace Main.Networking.Synchronisation.Server
{
    public partial class SynchronisedServer
    {
        protected override void OnConnected(MessageSession session)
        {
            //get list of all known database ids
            List<string> databaseIds;
            lock (_modCount) databaseIds = _modCount.Keys.ToList();

            Console.WriteLine("Client connected. Synchronising ids: " + LogWriter.StringifyCollection(databaseIds));
            
            //for each database
            foreach (var databaseId in databaseIds)
            {
                //request local values with modCount
                SendRequestsToOthers<GetValueRequest, GetValueReply>(new GetValueRequest() {DatabaseId = databaseId},
                    session, replies =>
                    {
                        //select message with highest modCount
                        Dictionary<string, SetValueMessage> updatedMessages = new Dictionary<string, SetValueMessage>();
                        
                        foreach (var messages in replies.Select(r => r.Messages))
                        {
                            //get the most up to date information from each client for each database
                            foreach (var message in messages)
                            {
                                string valueId = message.ValueId;
                                
                                //if no current best value exists, add it
                                if (!updatedMessages.TryGetValue(valueId, out SetValueMessage newestMessage))
                                {
                                    updatedMessages.Add(valueId, message);
                                    continue;
                                }
                                
                                //if saved message is newer (up-to-date), dont overwrite it
                                if(newestMessage.ModCount >= message.ModCount) continue;

                                updatedMessages[valueId] = message;
                            }
                        }
                        
                        //forward the messages to the newly connected session
                        foreach (var setValueMessage in updatedMessages.Values)
                        {
                            Console.WriteLine($"Setting new value: {setValueMessage}");
                            session.SendMessage(setValueMessage);
                        }
                    });
            }
        }
    }
}