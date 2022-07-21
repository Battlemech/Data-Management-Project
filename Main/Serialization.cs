using System;
using GroBuf;
using GroBuf.DataMembersExtracters;

namespace Main
{
    public static class Serialization
    {
        private static readonly Serializer Serializer = new Serializer(new PropertiesExtractor(), options : GroBufOptions.WriteEmptyObjects);
        
        public static byte[] Serialize(object o)
        {
            return Serializer.Serialize(o);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return Serializer.Deserialize<T>(bytes);
        }

        public static object Deserialize(byte[] bytes, Type type)
        {
            return Serializer.Deserialize(type, bytes);
        }
    }
}