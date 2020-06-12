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
        private readonly ChoJSONRecordConfiguration _configuration;

        public ChoPropertyRenameAndIgnoreSerializerContractResolver(ChoJSONRecordConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var propertyFullName = member.GetFullName();
            if (IsIgnored(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName))
            {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }

            if (IsRenamed(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName, out var newJsonPropertyName))
            {
                if (!newJsonPropertyName.IsNullOrWhiteSpace())
                    property.PropertyName = newJsonPropertyName;
            }

            if (_configuration.ContainsRecordConfigForType(property.DeclaringType))
            {
                var dict = _configuration.JSONRecordFieldConfigurationsForType[property.DeclaringType];
                if (dict != null && dict.ContainsKey(property.UnderlyingName))
                {
                    property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(dict[property.UnderlyingName], _configuration.Culture, 
                        property.PropertyType, _configuration.ObjectValidationMode, member);
                }
            }
            else if (_configuration.JSONRecordFieldConfigurations.Any(f => f.DeclaringMember == propertyFullName))
            {
                var fc = _configuration.JSONRecordFieldConfigurations.First(f => f.DeclaringMember == propertyFullName);
                property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member);
                property.DefaultValue = fc.DefaultValue;
                property.Order = fc.Order;
            }
            else if (_configuration.JSONRecordFieldConfigurations.Any(f => f.Name == propertyFullName))
            {
                var fc = _configuration.JSONRecordFieldConfigurations.First(f => f.Name == propertyFullName);
                property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member);
                property.DefaultValue = fc.DefaultValue;
                property.Order = fc.Order;
            }
            else
            {
                var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                if (pd != null)
                {
                    if (pd.Attributes.OfType<DefaultValueAttribute>().Any())
                        property.DefaultValue = pd.Attributes.OfType<DefaultValueAttribute>().First().Value;
                    if (pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any())
                        property.Order = pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().First().Order;
                }
            }

            if (_configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                property.NullValueHandling = NullValueHandling.Ignore;
            else
                property.NullValueHandling = NullValueHandling.Include;


            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName, string propertyName, string propertyFullName)
        {
            if (_configuration.IgnoredFields.Contains(propertyFullName) || _configuration.IgnoredFields.Contains(propertyName))
                return true;

            var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
            if (pd != null)
            {
                if (pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any())
                    return true;
                else if (pd.Attributes.OfType<JsonIgnoreAttribute>().Any())
                    return true;
            }

            return false;
        }

        private bool IsRenamed(Type type, string jsonPropertyName, string propertyName, string propertyFullName, out string newJsonPropertyName)
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

            if (_configuration.JSONRecordFieldConfigurations.Any(f => f.DeclaringMember == propertyFullName))
            {
                newJsonPropertyName = _configuration.JSONRecordFieldConfigurations.First(f => f.DeclaringMember == propertyFullName).FieldName;
                return true;
            }

            if (_configuration.JSONRecordFieldConfigurations.Any(f => f.Name == propertyName))
            {
                newJsonPropertyName = _configuration.JSONRecordFieldConfigurations.First(f => f.Name == propertyName).FieldName;
                return true;
            }

            var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
            if (pd != null)
            {
                var attr = pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault();
                if (attr != null && !attr.FieldName.IsNullOrWhiteSpace())
                {
                    newJsonPropertyName = attr.FieldName.Trim();
                    return true;
                }
                var attr1 = pd.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                if (attr1 != null && !attr1.DisplayName.IsNullOrWhiteSpace())
                {
                    newJsonPropertyName = attr1.DisplayName.Trim();
                    return true;
                }
                var dpAttr = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                if (dpAttr != null)
                {
                    if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                    {
                        newJsonPropertyName = dpAttr.ShortName;
                        return true;
                    }
                    else if (!dpAttr.Name.IsNullOrWhiteSpace())
                    {
                        newJsonPropertyName = dpAttr.Name;
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class ChoContractResolverJsonConverter : JsonConverter
    {
        private ChoJSONRecordFieldConfiguration _fc = null;
        private CultureInfo _culture;
        private Type _objType;
        private ChoObjectValidationMode _validationMode;
        private MemberInfo _mi;

        public ChoContractResolverJsonConverter(ChoJSONRecordFieldConfiguration fc, CultureInfo culture, Type objType, ChoObjectValidationMode validationMode, MemberInfo mi)
        {
            _fc = fc;
            _culture = culture;
            _objType = objType;
            _validationMode = validationMode;
            _mi = mi;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object retValue = null;
            if (_fc.CustomSerializer == null)
            {
                if (_fc.ValueConverter == null)
                    retValue = serializer.Deserialize(reader, objectType);
                else
                    retValue = _fc.ValueConverter(serializer.Deserialize(reader, typeof(string)));
            }
            else
            {
                retValue = _fc.CustomSerializer(reader);
            }

            if (retValue != null)
                retValue = ChoConvert.ConvertFrom(retValue, objectType, null, _fc.PropConverters, _fc.PropConverterParams, _culture);

            Validate(retValue);

            return retValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && _objType != null)
                value = ChoConvert.ConvertTo(value, _objType, null, _fc.PropConverters, _fc.PropConverterParams, _culture);

            if (_fc.CustomSerializer == null)
            {
                if (_fc.ValueConverter == null)
                {
                    JToken t = JToken.FromObject(value);
                    t.WriteTo(writer);
                }
                else
                {
                    object retValue = _fc.ValueConverter(value);

                    Validate(retValue);

                    ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);
                    JToken t = JToken.FromObject(retValue);
                    t.WriteTo(writer);
                }
            }
            else
            {
                object retValue = _fc.CustomSerializer(writer);
                if (retValue != null)
                {
                    Validate(retValue);
                    JToken t = JToken.FromObject(value);
                    t.WriteTo(writer);
                }
            }
        }

        private void Validate(object value)
        {
            if (_validationMode == ChoObjectValidationMode.MemberLevel)
            {
                var results = new List<ValidationResult>();
                var context = new ValidationContext(value, null, null);
                context.MemberName = _mi.Name;

                var vResult = Validator.TryValidateValue(value, context, results, _fc.Validators.IsNullOrEmpty() ? _mi.GetCustomAttributes<ValidationAttribute>() : _fc.Validators);
                if (!vResult)
                {
                    if (results.Count > 0)
                        throw new ValidationException("Failed to validate '{0}' member. {2}{1}".FormatString(_mi.Name, ToString(results), Environment.NewLine));
                    else
                        throw new ValidationException("Failed to valudate.");
                }
            }
        }
        private static string ToString(IEnumerable<ValidationResult> results)
        {
            StringBuilder msg = new StringBuilder();
            foreach (var validationResult in results)
            {
                msg.AppendLine(validationResult.ErrorMessage);

                if (validationResult is CompositeValidationResult)
                    msg.AppendLine(ToString(((CompositeValidationResult)validationResult).Results).Indent());
            }

            return msg.ToString();
        }
    }
}
