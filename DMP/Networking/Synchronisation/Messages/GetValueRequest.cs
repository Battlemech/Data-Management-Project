using System.Collections.Generic;
using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class GetValueRequest : RequestMessage<GetValueReply>
    {
        public string DatabaseId { get; set; }
        public Dictionary<string, uint> ModificationCount { get; set; }
    }
    
    public class GetValueReply : ReplyMessage
    {
        public List<SetValueMessage> SetValueMessages { get; set; }
        
        public GetValueReply(GetValueRequest requestMessage) : base(requestMessage)
        {
        }
    }
}