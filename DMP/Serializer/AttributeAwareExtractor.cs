using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            
            //get members //todo: optimize?
            var fields = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                            BindingFlags.DeclaredOnly)
                .Where(info => !info.CustomAttributes.Select(data => data.AttributeType).Contains(typeof(PreventSerialization)));
            
            members.AddRange(fields.Select(DataMember.Create));
            GetMembers(type.BaseType, members);
        }
    }
}