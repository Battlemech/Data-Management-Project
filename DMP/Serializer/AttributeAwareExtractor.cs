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
            
            foreach (var field in fields)
            {
                List<Type> customAttributes = field.CustomAttributes.Select((data => data.AttributeType)).ToList();
                
                //field is probably a backing field
                if (customAttributes.Contains(typeof(CompilerGeneratedAttribute))) continue;
                
                //don't serialize fields with this attribute
                if(customAttributes.Contains(typeof(PreventSerialization))) continue;

                members.Add(DataMember.Create(field));
            }

            GetMembers(type.BaseType, members);
        }
        
    }
}