using System;
using System.Collections.Generic;
using System.Linq;
using GroBuf;
using GroBuf.DataMembersExtracters;
using Main.Databases;

namespace Main.Utility
{
    public static class Serialization
    {
        private static readonly Serializer Serializer = new Serializer(new AllFieldsExtractor(), options : GroBufOptions.WriteEmptyObjects, customSerializerCollection: new IgnoreObjectSerializerCollection(Options.IgnoredTypes));

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
    }
    
    public class IgnoreObjectSerializerCollection : IGroBufCustomSerializerCollection
    {
        private readonly IgnoreObjectSerializer _ignoreObjectSerializer = new IgnoreObjectSerializer();
        private readonly Type[] _ignoredTypes;

        public IgnoreObjectSerializerCollection(params Type[] ignoredTypes)
        {
            _ignoredTypes = ignoredTypes;
        }
        
        public IGroBufCustomSerializer Get(Type declaredType, Func<Type, IGroBufCustomSerializer> factory, IGroBufCustomSerializer baseSerializer)
        {
            if (_ignoredTypes.Contains(declaredType)) return _ignoreObjectSerializer;
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