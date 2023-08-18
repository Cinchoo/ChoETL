using Microsoft.Hadoop.Avro;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ChoETL
{
    public class ChoAvroPublicMemberContractResolver : AvroPublicMemberContractResolver
    {
        public ChoAvroRecordConfiguration Configuration
        {
            get;
            set;
        }

        public ChoAvroPublicMemberContractResolver() : base(false)
        {
        }

        public ChoAvroPublicMemberContractResolver(bool allowNullable) : base(allowNullable)
        {
        }

        public override IEnumerable<Type> GetKnownTypes(Type type)
        {
            return Configuration != null ? Configuration.KnownTypes : null;
        }

        public override MemberSerializationInfo[] ResolveMembers(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var fields = Enumerable.Empty<FieldInfo>();
            //type
            //    .GetAllFields()
            //    .Where(f => (f.Attributes & FieldAttributes.Public) != 0 &&
            //                (f.Attributes & FieldAttributes.Static) == 0);

            var properties =
                type.GetAllProperties()
                    .Where(p =>
                           (p.DeclaringType.IsAnonymous() ||
                           p.DeclaringType.IsKeyValuePair() ||
                           (p.CanRead && p.CanWrite && p.GetSetMethod() != null && p.GetGetMethod() != null))
                           && p.GetCustomAttribute<ChoIgnoreMemberAttribute>() == null
                           );

            var serializedProperties = ChoType.RemoveDuplicates(properties);
            var fds = fields
                .Concat<MemberInfo>(serializedProperties)
                .Select(m => new MemberSerializationInfo { Name = m.Name, MemberInfo = m, Nullable = m.GetCustomAttributes(false).OfType<NullableSchemaAttribute>().Any() })
                .ToArray();

            if (Configuration != null)
            {
                List<MemberSerializationInfo> result = new List<MemberSerializationInfo>();
                foreach (var fd in fds)
                {
                    if (Configuration.IgnoredFields.Contains(fd.Name))
                        continue;

                    var c = Configuration.RecordFieldConfigurations.OfType<ChoAvroRecordFieldConfiguration>()
                        .FirstOrDefault(f => f.DeclaringMemberInternal == fd.MemberInfo.Name) as ChoAvroRecordFieldConfiguration;
                    if (c != null)
                    {
                        fd.Name = c.FieldName;
                        if (c.IsNullable != null)
                            fd.Nullable = c.IsNullable.Value;
                    }

                    result.Add(fd);
                }
                return result.ToArray();
            }
            else
                return fds.ToArray();
        }

        public override TypeSerializationInfo ResolveType(Type type)
        {
            if (type == typeof(object))
                return base.ResolveType(typeof(string));
            else
                return base.ResolveType(type);
        }
    }
}
