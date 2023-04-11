using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using GroBuf.DataMembersExtracters;

namespace DMP.Utility
{
    public class AttributeAwareExtractor : IDataMembersExtractor
    {
        public IDataMember[] GetMembers(Type type)
        {
            var result = new List<IDataMember>();
            GetMembers(type, result);
            return result.ToArray();
        }

        private static void GetMembers(Type type, List<IDataMember> members)
        {
            if (type == null || type == typeof(object))
                return;

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            members.AddRange((from field in fields let customAttributes = field.CustomAttributes.Select((data => data.AttributeType)).ToList() where !customAttributes.Contains(typeof(CompilerGeneratedAttribute)) where !customAttributes.Contains(typeof(PreventSerialization)) select DataMember.Create(field)));

            GetMembers(type.BaseType, members);
        }
        
    }
}