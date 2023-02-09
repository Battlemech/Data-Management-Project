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
    }
    
    /// <summary>
    /// Allows preserving type of message when calling Serialize() function
    /// </summary>
    public static class MessageUtility
    {
        public static byte[] Serialize<T>(this T message) where T : Message
        {
            return NetworkSerializer.Serialize(Serialization.Serialize(message));
        }
    }

}