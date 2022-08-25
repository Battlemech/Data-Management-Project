using System.Collections.Generic;
using Main.Networking.Messaging;

namespace Main.Networking.Synchronisation.Messages
{
    public class GetValueRequest : RequestMessage<GetValueReply>
    {
        public string DatabaseId;
    }
    
    public class GetValueReply : ReplyMessage
    {
        public List<SetValueMessage> Messages;
        
        public GetValueReply(RequestMessage requestMessage) : base(requestMessage)
        {
            
        }
    }
}