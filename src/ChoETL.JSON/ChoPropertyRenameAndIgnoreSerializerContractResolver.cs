using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, HashSet<string>> _ignores;
        private readonly Dictionary<Type, Dictionary<string, string>> _renames;
        private readonly ChoJSONRecordConfiguration _configuration;

        public ChoPropertyRenameAndIgnoreSerializerContractResolver(ChoJSONRecordConfiguration configuration)
        {
            _ignores = new Dictionary<Type, HashSet<string>>();
            _renames = new Dictionary<Type, Dictionary<string, string>>();
            _configuration = configuration;
        }

        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.ContainsKey(type))
                _ignores[type] = new HashSet<string>();

            foreach (var prop in jsonPropertyNames)
                _ignores[type].Add(prop);
        }

        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.ContainsKey(type))
                _renames[type] = new Dictionary<string, string>();

            _renames[type][propertyName] = newJsonPropertyName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (IsIgnored(property.DeclaringType, property.PropertyName, property.UnderlyingName))
            {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            if (IsRenamed(property.DeclaringType, property.PropertyName, property.UnderlyingName, out var newJsonPropertyName))
                property.PropertyName = newJsonPropertyName;

            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName, string propertyName)
        {
            if (_configuration != null && _configuration.ContainsRecordConfigForType(type))
            {
                var dict = _configuration.JSONRecordFieldConfigurationsForType[type];
                if (dict != null && dict.ContainsKey(jsonPropertyName))
                    return false;
            }

            if (!_ignores.ContainsKey(type))
            {
                var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
                return pd != null ? pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any() : true;
            }

            return _ignores[type].Contains(jsonPropertyName);
        }

        private bool IsRenamed(Type type, string jsonPropertyName, string propertyName, out string newJsonPropertyName)
        {
            newJsonPropertyName = null;

            if (_configuration != null && _configuration.ContainsRecordConfigForType(type))
            {
                var dict = _configuration.JSONRecordFieldConfigurationsForType[type];
                if (dict != null && dict.ContainsKey(jsonPropertyName))
                {
                    newJsonPropertyName = dict[jsonPropertyName].FieldName;
                    return true;
                }
            }

            Dictionary<string, string> renames;

            if (!_renames.TryGetValue(type, out renames) || !renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
            {
                var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
                if (pd != null)
                {
                    var attr = pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault();
                    if (attr != null && !attr.FieldName.IsNullOrWhiteSpace())
                    {
                        newJsonPropertyName = attr.FieldName.Trim();
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}
