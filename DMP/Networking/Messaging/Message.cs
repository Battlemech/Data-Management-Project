using System;
using DMP.Utility;

namespace DMP.Networking.Messaging
{
    public class Message
    {
        public readonly string SerializedType;
        protected Message()
        {
            SerializedType = GetType().AssemblyQualifiedName;
        }

        public Type GetMessageType()
        {
            return Type.GetType(SerializedType, true);
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