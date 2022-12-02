using System;
using System.Collections.Generic;
using System.Linq;
using DMP.Databases;
using GroBuf;
using GroBuf.DataMembersExtracters;

namespace DMP.Utility
{
    public static class Serialization
    {
        /// <summary>
        /// Array of types which will be ignored during serialization of objects.
        /// Configure this before you access Utility/Serialization.cs for the first time.
        /// Changes to IgnoredTypes after the initialization will not have any effect
        /// </summary>
        public static readonly List<Type> IgnoredTypes = new List<Type>() { typeof(Database) };
        
        private static readonly Serializer Serializer = new Serializer(new AllFieldsExtractor(), options : GroBufOptions.WriteEmptyObjects, customSerializerCollection: new IgnoreObjectSerializerCollection());

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
            return Serializer.Deserialize(type, bytes);
        }

        public static T Copy<T>(T o)
        {
            return Serializer.Copy(o);
        }
    }
    
    public class IgnoreObjectSerializerCollection : IGroBufCustomSerializerCollection
    {
        private readonly IgnoreObjectSerializer _ignoreObjectSerializer = new IgnoreObjectSerializer();

        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
        {
            //if declared type can be assigned to the type to ignore: Don't serialize it
            foreach (var ignoredType in Serialization.IgnoredTypes)
            {
                if (ignoredType.IsAssignableFrom(declaredType)) return _ignoreObjectSerializer;
            }
            
            return null;
        }
        
        private class IgnoreObjectSerializer : IGroBufCustomSerializer
        {
            public int CountSize(object obj, bool writeEmpty, WriterContext context)
            {
                return 0;
            }

            public void Write(object obj, bool writeEmpty, IntPtr result, ref int index, WriterContext context)
            {
                return;
            }

            public void Read(IntPtr data, ref int index, ref object result, ReaderContext context)
            {
                return;
            }
        }
    }
}