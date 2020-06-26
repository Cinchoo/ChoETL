using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        private readonly ChoFileRecordConfiguration _configuration;
        public IChoNotifyRecordFieldWrite CallbackRecordFieldWrite
        {
            get;
            set;
        }
        public ChoWriter Writer
        {
            get;
            set;
        }
        public IChoNotifyRecordFieldRead CallbackRecordFieldRead { get; set; }
        public ChoReader Reader { get; set; }

        public ChoPropertyRenameAndIgnoreSerializerContractResolver(ChoFileRecordConfiguration configuration)
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
                var dict = _configuration.GetRecordConfigDictionaryForType(property.DeclaringType);
                if (dict != null && dict.ContainsKey(property.UnderlyingName))
                {
                    property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(dict[property.UnderlyingName] as ChoFileRecordFieldConfiguration, _configuration.Culture,
                        property.PropertyType, _configuration.ObjectValidationMode, member)
                    {
                        Reader = Reader,
                        CallbackRecordFieldRead = CallbackRecordFieldRead,
                         Writer = Writer,
                        CallbackRecordFieldWrite = CallbackRecordFieldWrite
                    };
                }
            }
            else if (_configuration.RecordFieldConfigurations.Any(f => f.DeclaringMember == propertyFullName))
            {
                var fc = _configuration.RecordFieldConfigurations.First(f => f.DeclaringMember == propertyFullName) as ChoFileRecordFieldConfiguration;
                property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                {
                    Reader = Reader,
                    CallbackRecordFieldRead = CallbackRecordFieldRead,
                    Writer = Writer,
                    CallbackRecordFieldWrite = CallbackRecordFieldWrite
                };

                property.DefaultValue = fc.DefaultValue;
                property.Order = fc.Order;
            }
            else if (_configuration.RecordFieldConfigurations.Any(f => f.Name == propertyFullName))
            {
                var fc = _configuration.RecordFieldConfigurations.First(f => f.Name == propertyFullName) as ChoFileRecordFieldConfiguration;
                property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                {
                    Reader = Reader,
                    CallbackRecordFieldRead = CallbackRecordFieldRead,
                    Writer = Writer,
                    CallbackRecordFieldWrite = CallbackRecordFieldWrite
                };

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
                    else if (pd.Attributes.OfType<DisplayAttribute>().Any())
                        property.Order = pd.Attributes.OfType<DisplayAttribute>().First().Order;
                    else if (pd.Attributes.OfType<ColumnAttribute>().Any())
                        property.Order = pd.Attributes.OfType<ColumnAttribute>().First().Order;

                    property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(null, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                    {
                        Reader = Reader,
                        CallbackRecordFieldRead = CallbackRecordFieldRead,
                        Writer = Writer,
                        CallbackRecordFieldWrite = CallbackRecordFieldWrite
                    };
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
            if (pd == null)
                return true;
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
                var dict = _configuration.GetRecordConfigDictionaryForType(type);
                if (dict != null && dict.ContainsKey(jsonPropertyName))
                {
                    newJsonPropertyName = ((ChoFileRecordFieldConfiguration)dict[jsonPropertyName]).FieldName;
                    return true;
                }
            }

            if (_configuration.RecordFieldConfigurations.Any(f => f.DeclaringMember == propertyFullName))
            {
                newJsonPropertyName = _configuration.RecordFieldConfigurations.OfType<ChoFileRecordFieldConfiguration>().First(f => f.DeclaringMember == propertyFullName).FieldName;
                return true;
            }

            if (_configuration.RecordFieldConfigurations.Any(f => f.Name == propertyName))
            {
                newJsonPropertyName = _configuration.RecordFieldConfigurations.OfType<ChoFileRecordFieldConfiguration>().First(f => f.Name == propertyName).FieldName;
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
        private ChoFileRecordFieldConfiguration _fc = null;
        private CultureInfo _culture;
        private Type _objType;
        private ChoObjectValidationMode _validationMode;
        private MemberInfo _mi;
        public IChoNotifyRecordFieldWrite CallbackRecordFieldWrite
        {
            get;
            set;
        }
        public ChoWriter Writer
        {
            get;
            set;
        }
        public IChoNotifyRecordFieldRead CallbackRecordFieldRead { get; set; }
        public ChoReader Reader { get; set; }

        public ChoContractResolverJsonConverter(ChoFileRecordFieldConfiguration fc, CultureInfo culture, Type objType, ChoObjectValidationMode validationMode, MemberInfo mi)
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

            var crs = Reader.ContractResolverState;
            var fc = crs.FieldConfig;
            crs.Name = _fc == null ? _mi.GetFullName() : crs.Name;

            var rec = ChoType.GetMemberObjectMatchingType(crs.Name, crs.Record);
            var name = ChoType.GetFieldName(crs.Name);

            retValue = reader;
            if (!RaiseBeforeRecordFieldLoad(rec, crs.Index, name, ref retValue))
            {
                if (_fc != null)
                {
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

                    ValidateORead(ref retValue);
                    //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);

                    if (retValue != null)
                        retValue = ChoConvert.ConvertFrom(retValue, objectType, null, _fc.PropConverters, _fc.PropConverterParams, _culture);
                }
                else
                {
                    retValue = serializer.Deserialize(reader, objectType);
                    ValidateORead(ref retValue);

                    if (retValue != null)
                        retValue = ChoConvert.ConvertFrom(retValue, objectType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                }
            }
            if (!RaiseAfterRecordFieldLoad(rec, crs.Index, name, retValue))
                return null;

            return retValue == reader ? null : retValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var crs = Writer.ContractResolverState;
            var fc = crs.FieldConfig;
            crs.Name = _fc == null ? _mi.GetFullName() : crs.Name;

            var rec = ChoType.GetMemberObjectMatchingType(crs.Name, crs.Record);
            var name = ChoType.GetFieldName(crs.Name);

            if (RaiseBeforeRecordFieldWrite(rec, crs.Index, name, ref value))
            {
                if (_fc != null)
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
                            ValidateOnWrite(ref retValue);

                            //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);
                            JToken t = JToken.FromObject(retValue);
                            t.WriteTo(writer);
                        }
                    }
                    else
                    {
                        object retValue = _fc.CustomSerializer(writer);
                        ValidateOnWrite(ref retValue);
                        JToken t = JToken.FromObject(retValue);
                        t.WriteTo(writer);
                    }
                }
                else
                {
                    if (value != null && _objType != null)
                        value = ChoConvert.ConvertTo(value, _objType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);

                    if (ValidateOnWrite(ref value))
                    {
                        JToken t = JToken.FromObject(value);
                        t.WriteTo(writer);
                    }
                    else
                    {
                        JToken t = JToken.FromObject(null);
                        t.WriteTo(writer);
                    }
                }

                RaiseAfterRecordFieldWrite(rec, crs.Index, name, value);
            }
            else
            {
                JToken t = JToken.FromObject(null);
                t.WriteTo(writer);
            }
        }

        private bool ValidateORead(ref object value)
        {
            var crs = Reader.ContractResolverState;
            var fc = crs.FieldConfig;
            crs.Name = _fc == null ? _mi.GetFullName() : crs.Name;

            var rec = ChoType.GetMemberObjectMatchingType(crs.Name, crs.Record);
            var name = ChoType.GetFieldName(crs.Name);

            try
            {
                Validate(value);
            }
            catch (Exception ex)
            {
                ChoETLFramework.HandleException(ref ex);

                if (fc != null && fc.ErrorMode == ChoErrorMode.ThrowAndStop)
                    throw;

                if (fc != null)
                {
                    if (fc.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                    {
                        return false;
                    }
                    else
                    {
                        if (!RaiseRecordFieldLoadError(rec, crs.Index, name, ref value, ex))
                            throw new ChoWriterException($"Failed to load '{value}' value for '{crs.Name}' member.", ex);
                    }
                }
                else
                {
                    throw new ChoWriterException($"Failed to load '{value}' value for '{crs.Name}' member.", ex);
                }
            }

            return true;
        }

        private bool ValidateOnWrite(ref object value)
        {
            var crs = Writer.ContractResolverState;
            var fc = crs.FieldConfig;
            crs.Name = _fc == null ? _mi.GetFullName() : crs.Name;

            var rec = ChoType.GetMemberObjectMatchingType(crs.Name, crs.Record);
            var name = ChoType.GetFieldName(crs.Name);

            try
            {
                Validate(value);
            }
            catch (Exception ex)
            {
                ChoETLFramework.HandleException(ref ex);

                if (fc != null && fc.ErrorMode == ChoErrorMode.ThrowAndStop)
                    throw;

                if (fc != null)
                { 
                    if (fc.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                    {
                        return false;
                    }
                    else
                    {
                        if (!RaiseRecordFieldWriteError(rec, crs.Index, name, ref value, ex))
                            throw new ChoWriterException($"Failed to write '{value}' value of '{crs.Name}' member.", ex);
                    }
                }
                else
                {
                    throw new ChoWriterException($"Failed to write '{value}' value of '{crs.Name}' member.", ex);
                }
            }

            return true;
        }

        #region Event Raisers

        private bool RaiseAfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            if (Writer != null && Writer.HasAfterRecordFieldWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (CallbackRecordFieldWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldWrite.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (Writer != null && Writer.HasBeforeRecordFieldWriteSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (CallbackRecordFieldWrite != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldWrite.BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (Reader != null && Reader.HasBeforeRecordFieldLoadSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).BeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (CallbackRecordFieldRead != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldRead.BeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            if (Reader != null && Reader.HasAfterRecordFieldLoadSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (CallbackRecordFieldRead != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldRead.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = true;
            object state = value;
            if (Reader != null && Reader.HasRecordFieldLoadErrorSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).RecordFieldLoadError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            else if (CallbackRecordFieldRead != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldRead.RecordFieldLoadError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            return retValue;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = true;
            object state = value;
            if (Writer != null && Writer.HasRecordFieldWriteErrorSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).RecordFieldWriteError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            else if (CallbackRecordFieldWrite != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => CallbackRecordFieldWrite.RecordFieldWriteError(target, index, propName, ref state, ex), true);
                if (retValue)
                    value = state;
            }
            return retValue;
        }

        #endregion Event Raisers

        private void Validate(object value)
        {
            if (_validationMode == ChoObjectValidationMode.MemberLevel)
            {
                var results = new List<ValidationResult>();
                var context = new ValidationContext(value, null, null);
                context.MemberName = _mi.Name;

                bool vResult = false;
                if (_fc != null)
                    vResult = Validator.TryValidateValue(value, context, results, _fc.Validators.IsNullOrEmpty() ? _mi.GetCustomAttributes<ValidationAttribute>() : _fc.Validators);
                else
                    vResult = Validator.TryValidateValue(value, context, results, _mi.GetCustomAttributes<ValidationAttribute>());

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
