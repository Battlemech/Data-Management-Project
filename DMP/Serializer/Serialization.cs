using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DMP.Databases;
using GroBuf;
using GroBuf.DataMembersExtracters;

namespace DMP.Utility
{
    public static class Serialization
    {
        private static readonly Serializer Serializer = new Serializer(new AttributeAwareExtractor(), options : GroBufOptions.WriteEmptyObjects);

        /// <summary>
        /// Serializes the object
        /// </summary>
        /// <remarks>
        /// Make sure that the object is given to the function with its original type,
        /// or serialization will return a byte array representing null!
        /// </remarks>
        public static byte[] Serialize<T>(T o)
        {
            //Using type parameter to avoid an additional cast and allow the serializer to properly read object type.            
            return Serializer.Serialize(o);
        }

        public static byte[] Serialize(Type type, object o)
        {
            return Serializer.Serialize(type, o);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            return Serializer.Deserialize<T>(bytes);
        }

        public static object Deserialize(byte[] bytes, Type type)
        {
            return type == null ? default : Serializer.Deserialize(type, bytes);
        }

        public static T Copy<T>(T o)
        {
            return Serializer.Copy(o);
        }
    }
}