using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
    {
        public static readonly JsonConverter[] BuiltInConverters = new JsonConverter[10]
        {
            new EntityKeyMemberConverter(),
            new ExpandoObjectConverter(),
            new XmlNodeConverter(),
            new BinaryConverter(),
            new DataSetConverter(),
            new DataTableConverter(),
            new DiscriminatedUnionConverter(),
            new KeyValuePairConverter(),
            new BsonObjectIdConverter(),
            new RegexConverter()
        };

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
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public ChoPropertyRenameAndIgnoreSerializerContractResolver(ChoFileRecordConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool CanIncludeConverter(PropertyDescriptor pd)
        {
            Type mt = pd.PropertyType;
            if (mt.IsSimple())
                return true;
            else
            {
                bool? disableImplcityOp = null;
                if (ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(mt) != null)
                    disableImplcityOp = ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(mt).Flag;

                if (disableImplcityOp != null)
                {
                    if (!disableImplcityOp.Value)
                    {
                        Type to = null;
                        if (mt.CanCastToPrimitiveType(out to))
                            return true;
                        else if (mt.GetImplicitTypeCastOps().Any())
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            return true;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var propertyFullName = member.GetFullName();
            var propertyName = member.Name;

            if (IsIgnored(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName))
            {
                property.ShouldSerialize = i => false;
                property.ShouldDeserialize = i => false;
                property.Ignored = true;
                return property;
            }

            if (IsRenamed(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName, out var newJsonPropertyName))
            {
                if (!newJsonPropertyName.IsNullOrWhiteSpace())
                    property.PropertyName = newJsonPropertyName;
            }
            RemapToRefTypePropertiesIfAny(property.DeclaringType, propertyName, property);

            ChoFileRecordFieldConfiguration fc = null;
            var rfc = _configuration.RecordFieldConfigurations.ToArray();
            if (_configuration.ContainsRecordConfigForType(property.DeclaringType))
            {
                var dict = _configuration.GetRecordConfigDictionaryForType(property.DeclaringType);
                if (dict != null && dict.ContainsKey(property.UnderlyingName))
                {
                    var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                    fc = dict[property.UnderlyingName] as ChoFileRecordFieldConfiguration;
                    if (CanIncludeConverter(pd))
                    {
                        property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture,
                            property.PropertyType, _configuration.ObjectValidationMode, member)
                        {
                            Configuration = _configuration as ChoFileRecordConfiguration,
                            Reader = Reader,
                            CallbackRecordFieldRead = CallbackRecordFieldRead,
                            Writer = Writer,
                            CallbackRecordFieldWrite = CallbackRecordFieldWrite
                        };
                    }
                }
            }
            else if (rfc.Any(f => f.DeclaringMember == propertyFullName))
            {
                var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                fc = rfc.First(f => f.DeclaringMember == propertyFullName) as ChoFileRecordFieldConfiguration;
                if (CanIncludeConverter(pd))
                {
                    property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                    {
                        Configuration = _configuration as ChoFileRecordConfiguration,
                        Reader = Reader,
                        CallbackRecordFieldRead = CallbackRecordFieldRead,
                        Writer = Writer,
                        CallbackRecordFieldWrite = CallbackRecordFieldWrite
                    };
                }
            }
            else if (rfc.Any(f => f.Name == propertyName))
            {
                var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                fc = rfc.First(f => f.Name == propertyName) as ChoFileRecordFieldConfiguration;
                if (CanIncludeConverter(pd))
                {
                    property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(fc, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                    {
                        Configuration = _configuration as ChoFileRecordConfiguration,
                        Reader = Reader,
                        CallbackRecordFieldRead = CallbackRecordFieldRead,
                        Writer = Writer,
                        CallbackRecordFieldWrite = CallbackRecordFieldWrite
                    };
                }
            }
            else
            {
                var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                if (pd != null)
                {
                    if (pd.Attributes.OfType<DefaultValueAttribute>().Any())
                        property.DefaultValue = pd.Attributes.OfType<DefaultValueAttribute>().First().Value;
                    if (pd.Attributes.OfType<ChoFileRecordFieldAttribute>().Any())
                    {
                        var jp = pd.Attributes.OfType<ChoFileRecordFieldAttribute>().First();

                        property.Order = jp.Order;
                        property.PropertyName = jp.FieldName;
                    }
                    else if (pd.Attributes.OfType<DisplayAttribute>().Any())
                    {
                        var jp = pd.Attributes.OfType<DisplayAttribute>().First();

                        property.Order = jp.Order;
                        if (!jp.ShortName.IsNullOrWhiteSpace())
                            property.PropertyName = jp.ShortName.Trim();
                        else if (!jp.Name.IsNullOrWhiteSpace())
                            property.PropertyName = jp.Name.Trim();
                    }
                    else if (pd.Attributes.OfType<ColumnAttribute>().Any())
                    {
                        var jp = pd.Attributes.OfType<ColumnAttribute>().First();

                        property.Order = jp.Order;
                        if (!jp.Name.IsNullOrWhiteSpace())
                            property.PropertyName = jp.Name.Trim();
                    }

                    if (pd.Attributes.OfType<JsonPropertyAttribute>().Any())
                    {
                        var jp = pd.Attributes.OfType<JsonPropertyAttribute>().First();
                        property.PropertyName = jp.PropertyName;
                        property.Order = jp.Order;
                        property.Required = jp.Required;
                        property.ReferenceLoopHandling = jp.ItemReferenceLoopHandling;
                        property.IsReference = jp.IsReference;
                        property.TypeNameHandling = jp.TypeNameHandling;
                        property.ObjectCreationHandling = jp.ObjectCreationHandling;
                        property.ReferenceLoopHandling = jp.ReferenceLoopHandling;
                        property.DefaultValueHandling = jp.DefaultValueHandling;
                        property.NullValueHandling = jp.NullValueHandling;
                        property.ItemTypeNameHandling = jp.ItemTypeNameHandling;
                        property.ItemIsReference = jp.ItemIsReference;
                    }
                    else if (pd.Attributes.OfType<ChoJSONPathAttribute>().Any())
                        property.PropertyName = pd.Attributes.OfType<ChoJSONPathAttribute>().First().JSONPath;

                    if (CanIncludeConverter(pd))
                    {
                        property.Converter = property.MemberConverter = new ChoContractResolverJsonConverter(null, _configuration.Culture, property.PropertyType, _configuration.ObjectValidationMode, member)
                        {
                            Configuration = _configuration as ChoFileRecordConfiguration,
                            Reader = Reader,
                            CallbackRecordFieldRead = CallbackRecordFieldRead,
                            Writer = Writer,
                            CallbackRecordFieldWrite = CallbackRecordFieldWrite
                        };
                    }
                }
            }

            if (_configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                property.NullValueHandling = NullValueHandling.Ignore;
            else
                property.NullValueHandling = NullValueHandling.Include;

            if (fc != null)
            {
                property.DefaultValue = fc.DefaultValue;
                property.Order = fc.Order;
            }

            return property;
        }

        private void RemapToRefTypePropertiesIfAny(Type type, string propertyName, JsonProperty prop)
        {
            var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
            if (pd != null)
            {
                var jattr = pd.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                if (jattr != null && !jattr.PropertyName.IsNullOrWhiteSpace())
                {
                    prop.ItemReferenceLoopHandling = jattr.ItemReferenceLoopHandling;
                    prop.Required = jattr.Required;
                    prop.Order = jattr.Order;
                    prop.IsReference = jattr.IsReference;
                    prop.TypeNameHandling = jattr.TypeNameHandling;
                    prop.ObjectCreationHandling = jattr.ObjectCreationHandling;
                    prop.ReferenceLoopHandling = jattr.ItemReferenceLoopHandling;
                    prop.DefaultValueHandling = jattr.DefaultValueHandling;
                    prop.NullValueHandling = jattr.NullValueHandling;
                    //prop.NamingStrategyParameters = jattr.NamingStrategyParameters;
                    //prop.NamingStrategyType = jattr.NamingStrategyType;
                    try
                    {
                        if (jattr.ItemConverterType != null)
                            prop.ItemConverter = ChoActivator.CreateInstance(jattr.ItemConverterType, jattr.ItemConverterParameters) as JsonConverter;
                    }
                    catch { }
                    prop.ItemTypeNameHandling = jattr.ItemTypeNameHandling;
                    prop.ItemIsReference = jattr.ItemIsReference;

                }
            }
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
                if (dict != null && dict.ContainsKey(propertyName))
                {
                    newJsonPropertyName = ((ChoFileRecordFieldConfiguration)dict[propertyName]).FieldName;
                    return propertyName != newJsonPropertyName;
                }
            }

            var rfc = _configuration.RecordFieldConfigurations.ToArray();

            if (rfc.Any(f => f.DeclaringMember == propertyFullName))
            {
                newJsonPropertyName = rfc.OfType<ChoFileRecordFieldConfiguration>().First(f => f.DeclaringMember == propertyFullName).FieldName;
                return propertyName != newJsonPropertyName;
            }

            if (rfc.Any(f => f.Name == propertyName))
            {
                newJsonPropertyName = rfc.OfType<ChoFileRecordFieldConfiguration>().First(f => f.Name == propertyName).FieldName;
                return propertyName != newJsonPropertyName;
            }

            var pd = ChoTypeDescriptor.GetProperty(type, propertyName);
            if (pd != null)
            {
                var jattr = pd.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                if (jattr != null && !jattr.PropertyName.IsNullOrWhiteSpace())
                {
                    newJsonPropertyName = jattr.PropertyName.Trim();
                    return true;
                }
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
        public ChoFileRecordConfiguration Configuration { get; set; }
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

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

        private ChoJSONRecordFieldConfiguration GetFieldConfiguration(Type rt, string fn)
        {
            if (Configuration != null)
            {
                var lrt = Configuration.GetRecordConfigDictionaryForType(rt);
                if (lrt == null)
                    ((ChoJSONRecordConfiguration)Configuration).MapRecordFieldsForType(rt);

                lrt = Configuration.GetRecordConfigDictionaryForType(rt);
                if (lrt != null)
                {
                    if (lrt.ContainsKey(fn))
                        return lrt[fn] as ChoJSONRecordFieldConfiguration;
                }
                else
                {
                    return Configuration.RecordFieldConfigurations.Select(fc => fc.Name == fn).OfType<ChoJSONRecordFieldConfiguration>().FirstOrDefault();
                }
            }
            return null;
        }

        private object[] GetTypeConverters(Type rt, string fn)
        {
            var fc = _fc == null ? GetFieldConfiguration(rt, fn) : _fc;
            if (fc == null)
                return null;

            var conv = fc.GetConverters();
            if (fc == null)
            {
                conv = ChoTypeDescriptor.GetTypeConverters(_mi);
            }
            return conv;
        }

        private IContractResolver GetContractResolver(ChoJSONRecordFieldConfiguration config)
        {
            return config == null ? null : config.ContractResolver;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object retValue = null;

            var crs = Reader != null ? Reader.ContractResolverState : null;
            if (Configuration.TurnOffContractResolverState)
                crs = null;

            if (_fc == null)
            {
                _fc = GetFieldConfiguration(_mi.ReflectedType, _mi.Name);
            }

            if (crs == null || _fc == null)
            {
                try
                {
                    var jo = JObject.Load(reader);
                    try
                    {
                        using (var jObjectReader = reader.CopyReaderForObject(jo))
                        {
                            return serializer.Deserialize(jObjectReader, objectType);
                        }
                        //return serializer.Deserialize(reader, objectType);
                    }
                    catch
                    {
                        var c = Configuration as ChoJSONRecordConfiguration;
                        if (c != null)
                        {
                            var ut = c.UnknownType;
                            var utc = c.UnknownTypeConverter;
                            if (utc != null)
                                return utc(jo);
                            else if (ut != null)
                                return jo.ToObject(ut);
                        }
                        return jo;
                    }
                }
                catch
                {
                    return serializer.Deserialize(reader, objectType);
                }
            }
            else
            {
                IContractResolver contractResolver = GetContractResolver(_fc as ChoJSONRecordFieldConfiguration);
                var savedContractResolver = serializer.ContractResolver;
                try
                {
                    if (contractResolver != null)
                        serializer.ContractResolver = contractResolver;

                    crs.Name = _fc.Name;
                    crs.FieldConfig = _fc;
                    crs.Record = crs.Record == null ? ChoActivator.CreateInstanceNCache(_mi.ReflectedType) : crs.Record;

                    Type mt = ChoType.GetMemberType(_mi);
                    var name = ChoType.GetFieldName(crs.Name);
                    var rec = ChoType.GetMemberObjectMatchingType(name, crs.Record);

                    try
                    {
                        try
                        {
                            retValue = JObject.Load(reader);
                        }
                        catch
                        {
                            if (_fc.ValueConverter == null)
                                retValue = serializer.Deserialize(reader, objectType);
                            else
                            {
                                retValue = serializer.Deserialize(reader, typeof(string));
                                retValue = _fc.ValueConverter(retValue);
                            }

                        }

                        var st = ChoType.GetMemberAttribute(_mi, typeof(ChoSourceTypeAttribute)) as ChoSourceTypeAttribute;
                        if (st != null && st.Type != null)
                            _objType = st.Type;
                        if (_fc != null && _fc.SourceType != null)
                            _objType = _fc.SourceType;
                        else if (_objType.GetImplicitTypeCastOps().Any())
                        {
                            bool disableImplcityOp = false;
                            if (ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(_objType) != null)
                                disableImplcityOp = ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(_objType).Flag;

                            if (!disableImplcityOp)
                            {
                                if (retValue is JToken)
                                {
                                    var castTypes = _objType.GetImplicitTypeCastOps();

                                    foreach (var ct in castTypes)
                                    {
                                        try
                                        {
                                            retValue = ((JToken)retValue).ToObject(ct);
                                            break;
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        if (!RaiseBeforeRecordFieldLoad(crs.Record, crs.Index, name, ref retValue))
                        {
                            if (_fc != null)
                            {
                                if (_fc.CustomSerializer == null && retValue is JObject)
                                {
                                    if (_fc.ValueConverter == null)
                                    {
                                        if (retValue is JObject)
                                            retValue = ((JObject)retValue).ToObject(objectType);
                                    }
                                    else
                                        retValue = _fc.ValueConverter(retValue);
                                }
                                else
                                {
                                    retValue = _fc.CustomSerializer(retValue);
                                }

                                //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);

                                if (retValue is JObject && GetTypeConverters(_objType, name).IsNullOrEmpty()) //  ChoTypeDescriptor.GetTypeConverters(_mi).IsNullOrEmpty())
                                    retValue = ((JObject)retValue).ToObject(objectType);

                                if (retValue != null)
                                {
                                    if (_fc != null)
                                        ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture);
                                    else
                                        retValue = ChoConvert.ConvertFrom(retValue, objectType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                                }
                                ValidateORead(ref retValue);
                            }
                            else
                            {
                                if (retValue != null)
                                {
                                    if (retValue is JObject && GetTypeConverters(_objType, name).IsNullOrEmpty())
                                        retValue = ((JObject)retValue).ToObject(objectType);

                                    if (_fc != null)
                                        ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture);
                                    else
                                        retValue = ChoConvert.ConvertFrom(retValue, objectType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                                }

                                ValidateORead(ref retValue);
                            }
                        }
                        else
                        {
                            if (retValue != null)
                            {
                                if (retValue is JObject && GetTypeConverters(_objType, name).IsNullOrEmpty())
                                    retValue = ((JObject)retValue).ToObject(objectType);

                                if (_fc != null)
                                    ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture);
                                else
                                    retValue = ChoConvert.ConvertFrom(retValue, objectType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                            }

                            ValidateORead(ref retValue);
                        }
                        if (!RaiseAfterRecordFieldLoad(rec, crs.Index, name, retValue))
                            return null;
                    }
                    catch (ChoParserException)
                    {
                        Reader.IsValid = false;
                        throw;
                    }
                    catch (ChoMissingRecordFieldException)
                    {
                        Reader.IsValid = false;
                        if (Configuration.ThrowAndStopOnMissingField)
                            throw;
                    }
                    catch (Exception ex)
                    {
                        Reader.IsValid = false;
                        ChoETLFramework.HandleException(ref ex);

                        if (_fc.ErrorMode == ChoErrorMode.ThrowAndStop)
                            throw;

                        if (_fc.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring field...".FormatString(ex.Message));
                        }
                        else
                        {
                            if (!RaiseRecordFieldLoadError(rec, crs.Index, name, ref retValue, ex))
                            {
                                throw;
                            }
                            else
                            {
                            }
                        }
                    }
                }
                finally
                {
                    if (contractResolver != null)
                        serializer.ContractResolver = savedContractResolver;
                }

                return retValue; // == reader ? serializer.Deserialize(reader, objectType) : retValue;}
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var crs = Writer != null ? Writer.ContractResolverState : null;
            if (Configuration.TurnOffContractResolverState)
                crs = null;
            if (crs == null)
            {
                var t = serializer.SerializeToJToken(value);
                t.WriteTo(writer);
                //serializer.Serialize(writer, value);
                return;
            }

            if (_fc == null)
            {
                _fc = GetFieldConfiguration(_mi.ReflectedType, _mi.Name);
            }

            crs.Name = _fc.Name;
            crs.FieldConfig = _fc;
            crs.Record = crs.Record == null ? ChoActivator.CreateInstanceNCache(_mi.ReflectedType) : crs.Record;

            IContractResolver contractResolver = GetContractResolver(_fc as ChoJSONRecordFieldConfiguration);
            var savedContractResolver = serializer.ContractResolver;

            try
            {
                if (contractResolver != null)
                    serializer.ContractResolver = contractResolver;

                Type mt = ChoType.GetMemberType(_mi);
                var name = ChoType.GetFieldName(crs.Name);
                var rec = ChoType.GetMemberObjectMatchingType(name, crs.Record);

                var st = ChoType.GetMemberAttribute(_mi, typeof(ChoSourceTypeAttribute)) as ChoSourceTypeAttribute;
                if (st != null && st.Type != null)
                    _objType = st.Type;
                if (_fc != null && _fc.SourceType != null)
                    _objType = _fc.SourceType;

                if (RaiseBeforeRecordFieldWrite(rec, crs.Index, name, ref value))
                {
                    if (_fc != null)
                    {
                        if (_fc != null)
                            ChoETLRecordHelper.GetNConvertMemberValue(rec, name, _fc, _culture, ref value);
                        else
                        {
                            if (value != null && _objType != null)
                                value = ChoConvert.ConvertTo(value, _objType, null, _fc.PropConverters, _fc.PropConverterParams, _culture);
                        }

                        if (_fc.CustomSerializer == null)
                        {
                            if (_fc.ValueConverter == null)
                            {
                                var t = serializer.SerializeToJToken(value);
                                t.WriteTo(writer);
                            }
                            else
                            {
                                object retValue = _fc.ValueConverter(value);
                                ValidateOnWrite(ref retValue);

                                //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);
                                JToken t = JToken.FromObject(retValue, serializer);
                                t.WriteTo(writer);
                            }
                        }
                        else
                        {
                            object retValue = _fc.CustomSerializer(writer);
                            ValidateOnWrite(ref retValue);
                            JToken t = JToken.FromObject(retValue, serializer);
                            t.WriteTo(writer);
                        }
                    }
                    else
                    {
                        if (_fc != null)
                            ChoETLRecordHelper.GetNConvertMemberValue(rec, name, _fc, _culture, ref value);
                        else
                        {
                            if (value != null && _objType != null)
                                value = ChoConvert.ConvertTo(value, _objType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                        }

                        if (ValidateOnWrite(ref value))
                        {
                            var t = serializer.SerializeToJToken(value);
                            t.WriteTo(writer);
                        }
                        else
                        {
                            JToken t = JToken.FromObject(null, serializer);
                            t.WriteTo(writer);
                        }
                    }

                    RaiseAfterRecordFieldWrite(rec, crs.Index, name, value);
                }
                else
                {
                    JToken t = JToken.FromObject(null, serializer);
                    t.WriteTo(writer);
                }
            }
            finally
            {
                if (contractResolver != null)
                    serializer.ContractResolver = savedContractResolver;
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
            bool retValue = false;
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

    public class ChoJSONPathConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            object targetObj = Activator.CreateInstance(objectType);

            foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                                                .OfType<JsonPropertyAttribute>()
                                                .FirstOrDefault();

                string jsonPath = att != null ? att.PropertyName : prop.Name;

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath);
                }

                //if (!Regex.IsMatch(jsonPath, @"^[a-zA-Z0-9_.-]+$"))
                //{
                //    throw new InvalidOperationException($"JProperties of JsonPathConverter can have only letters, numbers, underscores, hiffens and dots but name was ${jsonPath}."); // Array operations not permitted
                //}

                JToken token = jo.SelectToken(jsonPath);
                if (token != null && token.Type != JTokenType.Null)
                {
                    object value = token.ToObject(prop.PropertyType, serializer);
                    prop.SetValue(targetObj, value, null);
                }
            }

            return targetObj;

            //var jo = JObject.Load(reader);
            //object targetObj = existingValue ?? Activator.CreateInstance(objectType);

            //foreach (var prop in objectType.GetProperties().Where(p => p.CanRead))
            //{
            //    var pathAttribute = prop.GetCustomAttributes(true).OfType<JsonPropertyAttribute>().FirstOrDefault();
            //    var converterAttribute = prop.GetCustomAttributes(true).OfType<JsonConverterAttribute>().FirstOrDefault();

            //    string jsonPath = pathAttribute?.PropertyName ?? prop.Name;
            //    var token = jo.SelectToken(jsonPath);

            //    if (token != null && token.Type != JTokenType.Null)
            //    {
            //        bool done = false;

            //        if (converterAttribute != null)
            //        {
            //            var args = converterAttribute.ConverterParameters ?? Array.Empty<object>();
            //            var converter = Activator.CreateInstance(converterAttribute.ConverterType, args) as JsonConverter;
            //            if (converter != null && converter.CanRead)
            //            {
            //                using (var sr = new StringReader(token.ToString()))
            //                using (var jr = new JsonTextReader(sr))
            //                {
            //                    var value = converter.ReadJson(jr, prop.PropertyType, prop.GetValue(targetObj), serializer);
            //                    if (prop.CanWrite)
            //                    {
            //                        prop.SetValue(targetObj, value);
            //                    }
            //                    done = true;
            //                }
            //            }
            //        }

            //        if (!done)
            //        {
            //            if (prop.CanWrite)
            //            {
            //                object value = token.ToObject(prop.PropertyType, serializer);
            //                prop.SetValue(targetObj, value);
            //            }
            //            else
            //            {
            //                using (var sr = new StringReader(token.ToString()))
            //                {
            //                    serializer.Populate(sr, prop.GetValue(targetObj));
            //                }
            //            }
            //        }
            //    }
            //}

            //return targetObj;
        }

        /// <remarks>
        /// CanConvert is not called when <see cref="JsonConverterAttribute">JsonConverterAttribute</see> is used.
        /// </remarks>
        public override bool CanConvert(Type objectType) => false;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override void WriteJson
        (
            JsonWriter writer,
            object value,
            JsonSerializer serializer
        )
        {
            throw new NotImplementedException();
        }
    }
}
