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

            //get fields //todo: optimize?
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | 
                                        BindingFlags.Public | BindingFlags.DeclaredOnly)
                //filter fields with NonSerialized attribute
                .Where((info =>
                    !info.CustomAttributes.Select((data => data.AttributeType))
                        .Contains(typeof(NonSerializedAttribute))));
            
            members.AddRange(fields.Select(DataMember.Create));
            GetMembers(type.BaseType, members);
        }
    }
}