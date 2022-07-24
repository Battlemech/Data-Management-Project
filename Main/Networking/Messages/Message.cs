using System;
using Main.Utility;

namespace Main.Networking.Messages
{
    public class Message
    {
        public readonly string SerializedType;
        protected Message()
        {
            SerializedType = GetType().FullName;
        }
    }

    public static class MessageUtility
    {
        public static byte[] Serialize<T>(this T message) where T : Message
        {
            return NetworkSerializer.Serialize(Serialization.Serialize(message));
        }
    }

}