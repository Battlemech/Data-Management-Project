using DMP.Utility;

namespace DMP.Networking.Messaging
{
    public class Message
    {
        public string SerializedType { get; }
        protected Message()
        {
            SerializedType = GetType().FullName;
        }
        
        public byte[] Serialize()
        {
            return NetworkSerializer.Serialize(Serialization.Serialize(this));
        }
    }

}