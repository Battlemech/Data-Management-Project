using DMP.Networking.Messaging.Server;

namespace DMP.Networking.Synchronisation.Server
{
    public class SynchronisedSession : MessageSession
    {
        public SynchronisedSession(MessageServer server) : base(server)
        {
            
        }
    }
}