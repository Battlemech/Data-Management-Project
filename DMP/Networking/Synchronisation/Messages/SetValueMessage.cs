using DMP.Networking.Messaging;

namespace DMP.Networking.Synchronisation.Messages
{
    public class SetValueMessage : Message
    {
        public string DatabaseId;
        public string ValueId;
        public byte[] Value;
        public uint ModificationCount;
    }
}