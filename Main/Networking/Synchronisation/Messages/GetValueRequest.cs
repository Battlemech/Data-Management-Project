using System.Collections.Generic;
using Main.Networking.Messaging;

namespace Main.Networking.Synchronisation.Messages
{
    public class GetValueRequest : RequestMessage<GetValueReply>
    {
        public string DatabaseId;
        public Dictionary<string, uint> ModificationCount;
    }
    
    public class GetValueReply : ReplyMessage
    {
        public List<SetValueMessage> SetValueMessages;
        
        public GetValueReply(GetValueRequest requestMessage) : base(requestMessage)
        {
        }
    }
}