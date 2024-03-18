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
using System.Xml.Linq;

namespace ChoETL
{
    public class ChoPropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver, IChoJsonContractResolver
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

        private readonly Dictionary<MemberInfo, JsonProperty> _jsonProperties = new Dictionary<MemberInfo, JsonProperty>();
        private readonly Dictionary<JsonProperty, string> _jsonPropertyJsonPaths = new Dictionary<JsonProperty, string>();
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

        public ChoFileRecordConfiguration RecordConfiguration => _configuration;

        public Dictionary<MemberInfo, JsonProperty> JsonProperties => _jsonProperties;
        public Dictionary<JsonProperty, string> JsonPropertiesJsonPaths => _jsonPropertyJsonPaths;

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

            if (IsIgnored(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName, member))
            {
                property.ShouldSerialize = i => false;
                property.ShouldDeserialize = i => false;
                property.Ignored = true;
                return property;
            }

            if (IsRenamed(property.DeclaringType, property.PropertyName, property.UnderlyingName, propertyFullName, member, out var newJsonPropertyName))
            {
                if (!newJsonPropertyName.IsNullOrWhiteSpace())
                    property.PropertyName = newJsonPropertyName;
            }
            if (NamingStrategy != null)
                property.PropertyName = NamingStrategy.GetPropertyName(property.PropertyName, false);

            RemapToRefTypePropertiesIfAny(property.DeclaringType, propertyName, property, member);

            ChoFileRecordFieldConfiguration fc = null;
            var rfc = _configuration.RecordFieldConfigurations.ToArray();
            if (_configuration is IChoJSONRecordConfiguration config)
            {
                if (config.ContainsRecordConfigForType(property.DeclaringType))
                {
                    var dict = config.GetRecordConfigDictionaryForType(property.DeclaringType);
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
                        if (fc is IChoJSONRecordFieldConfiguration JSONRecordFieldConfiguration)
                        {
                            ExtractJsonPathIfAny(property, pd, JSONRecordFieldConfiguration.JSONPath);
                        }
                    }
                }
                else if (rfc.OfType<IChoJSONRecordFieldConfiguration>().Any(f => f.DeclaringMember == propertyFullName) 
                    && rfc.OfType<IChoJSONRecordFieldConfiguration>().First(f => f.DeclaringMember == propertyFullName)
                    .CastTo<IChoJSONRecordFieldConfiguration>()?.PD?.ComponentType == property.DeclaringType)
                {
                    var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                    fc = rfc.OfType<IChoJSONRecordFieldConfiguration>().First(f => f.DeclaringMember == propertyFullName) as ChoFileRecordFieldConfiguration;
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
                        ExtractJsonPathIfAny(property, pd);
                    }
                }
                else if (rfc.Any(f => f.Name == propertyName) && rfc.First(f => f.Name == propertyName).CastTo<IChoJSONRecordFieldConfiguration>()?.PD?.ComponentType == property.DeclaringType)
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
                        ExtractJsonPathIfAny(property, pd);
                    }
                }
                else
                {
                    var pd = ChoTypeDescriptor.GetProperty(property.DeclaringType, property.UnderlyingName);
                    property.PropertyName = pd.DisplayName;
                    if (pd != null)
                    {
                        if (pd.Attributes.OfType<DefaultValueAttribute>().Any())
                            property.DefaultValue = pd.Attributes.OfType<DefaultValueAttribute>().First().Value;
                        if (pd.Attributes.OfType<ChoFileRecordFieldAttribute>().Any())
                        {
                            var jp = pd.Attributes.OfType<ChoFileRecordFieldAttribute>().First();

                            property.Order = jp.Order;
                            if (!jp.FieldName.IsNullOrWhiteSpace())
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

                        ExtractJsonPathIfAny(property, pd);

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

            if (!_jsonProperties.ContainsKey(member))
                _jsonProperties.Add(member, property);
            return property;
        }

        private void ExtractJsonPathIfAny(JsonProperty property, PropertyDescriptor pd,
            string jsonPath = null)
        {
            if (jsonPath == null)
            {
                if (pd.Attributes.OfType<ChoJSONPathAttribute>().Any())
                {
                    jsonPath = pd.Attributes.OfType<ChoJSONPathAttribute>().First().JSONPath;
                    if (!_jsonPropertyJsonPaths.ContainsKey(property))
                        _jsonPropertyJsonPaths.Add(property, jsonPath);
                }
            }
            else
            {
                if (!_jsonPropertyJsonPaths.ContainsKey(property))
                    _jsonPropertyJsonPaths.Add(property, jsonPath);
            }
        }

        private void RemapToRefTypePropertiesIfAny(Type type, string propertyName, JsonProperty prop, MemberInfo member)
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
            var cf = _configuration as IChoJSONRecordConfiguration;
            if (cf != null && cf.RemapJsonProperty != null)
            {
                cf.RemapJsonProperty(type, member, propertyName, prop);
            }
        }

        private bool IsIgnored(Type type, string jsonPropertyName, string propertyName, string propertyFullName, MemberInfo member)
        {
            var cf = _configuration as IChoJSONRecordConfiguration;
            if (cf != null && cf.IgnoreProperty != null)
            {
                var ret = cf.IgnoreProperty(type, member, jsonPropertyName);
                if (ret != null)
                    return ret.Value;
            }

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

        private bool IsRenamed(Type type, string jsonPropertyName, string propertyName, string propertyFullName, MemberInfo member, out string newJsonPropertyName)
        {
            newJsonPropertyName = null;
            var cf = _configuration as IChoJSONRecordConfiguration;
            if (cf != null && cf.RenameProperty != null)
            {
                var ret = cf.RenameProperty(type, member, jsonPropertyName);
                if (!ret.IsNullOrWhiteSpace())
                {
                    newJsonPropertyName = ret;
                    return propertyName != newJsonPropertyName;
                }
            }

            if (_configuration is IChoJSONRecordConfiguration config)
            {
                if (config != null && config.ContainsRecordConfigForType(type))
                {
                    var dict = config.GetRecordConfigDictionaryForType(type);
                    if (dict != null && dict.ContainsKey(propertyName))
                    {
                        newJsonPropertyName = ((ChoFileRecordFieldConfiguration)dict[propertyName]).FieldName;
                        return propertyName != newJsonPropertyName;
                    }
                }
            }
            var rfc = _configuration.RecordFieldConfigurations.ToArray();

            if (rfc.OfType<IChoJSONRecordFieldConfiguration>().Any(f => f.DeclaringMember == propertyFullName))
            {
                var fc = rfc.OfType<IChoJSONRecordFieldConfiguration>().First(f => f.DeclaringMember == propertyFullName) as ChoFileRecordFieldConfiguration;
                newJsonPropertyName = fc?.FieldName;
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
        public IChoJSONRecordConfiguration JSONConfiguration { get { return Configuration as IChoJSONRecordConfiguration; } }
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

        private ChoFileRecordFieldConfiguration GetFieldConfiguration(Type rt, string fn)
        {
            if (Configuration != null)
            {
                if (Configuration is IChoJSONRecordConfiguration config)
                {
                    var lrt = config.GetRecordConfigDictionaryForType(rt);
                    if (lrt == null)
                        config.MapRecordFieldsForType(rt);

                    lrt = config.GetRecordConfigDictionaryForType(rt);
                    if (lrt != null)
                    {
                        if (lrt.ContainsKey(fn))
                            return lrt[fn] as ChoFileRecordFieldConfiguration;
                    }
                    else
                    {
                        return Configuration.RecordFieldConfigurations.Select(fc => fc.Name == fn).OfType<ChoFileRecordFieldConfiguration>().FirstOrDefault();
                    }
                }
            }
            return null;
        }

        private object[] GetTypeConverters(Type rt, string fn)
        {
            var fc = _fc == null ? GetFieldConfiguration(rt, fn) : _fc;

            object[] conv = null;
            if (fc is IChoJSONRecordFieldConfiguration fc1)
                conv = fc1.GetConverters();

            if (fc == null)
            {
                conv = ChoTypeDescriptor.GetTypeConverters(_mi);
            }
            return conv;
        }

        private IContractResolver GetContractResolver(IChoJSONRecordFieldConfiguration config)
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

            var jsonConfig = Configuration as IChoJSONRecordConfiguration;
            if (crs == null || _fc == null)
            {
                try
                {
                    var jo = jsonConfig == null ? JToken.Load(reader):
                        ChoJObjectLoader.InvokeJObjectLoader(reader, jsonConfig.JsonLoadSettings, jsonConfig.JObjectLoadOptions,
                            jsonConfig.CustomJObjectLoader);
                    if (jo != null)
                    {
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
                            var c = Configuration as IChoJSONRecordConfiguration;
                            if (c != null)
                            {
                                var ut = c.UnknownType;
                                var utc = c.UnknownTypeConverter;
                                if (utc != null && jo is JObject jobj)
                                    return utc(jobj);
                                else if (ut != null)
                                    return jo.ToObject(ut);
                            }
                            return jo;
                        }
                    }
                    return null;
                }
                catch
                {
                    return serializer.Deserialize(reader, objectType);
                }
            }
            else
            {
                IContractResolver contractResolver = GetContractResolver(_fc as IChoJSONRecordFieldConfiguration);
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
                    //if (rec == null)
                    //{
                    //    rec = ChoActivator.CreateInstance(mt);
                    //    ChoType.SetMemberValue(crs.Record, name, rec);
                    //}

                    try
                    {
                        retValue = jsonConfig == null ? JObject.Load(reader) : ChoJObjectLoader.InvokeJObjectLoader(reader, jsonConfig.JsonLoadSettings, jsonConfig.JObjectLoadOptions,
                            jsonConfig.CustomJObjectLoader);
                        if (RaiseBeforeRecordFieldLoad(crs.Record, crs.Index, name, ref retValue))
                        {
                            try
                            {
                                if (retValue is JToken jo)
                                {
                                    using (var jObjectReader = reader.CopyReaderForObject(jo))
                                    {
                                        retValue = serializer.Deserialize(jObjectReader, objectType);
                                    }
                                }
                            }
                            catch
                            {
                                if (_fc.ValueConverter == null)
                                    retValue = serializer.Deserialize(reader, objectType);
                                else
                                {
                                    retValue = serializer.Deserialize(reader, typeof(string));
                                    //retValue = _fc.ValueConverter(retValue);
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

                            if (_fc != null)
                            {
                                if (_fc.CustomSerializer == null && retValue is JObject)
                                {
                                    if (_fc.ValueConverter == null)
                                    {
                                        if (retValue is JObject)
                                            retValue = ((JObject)retValue).ToObject(objectType, serializer);
                                    }
                                    else
                                        retValue = _fc.ValueConverter(retValue);
                                }
                                else if (_fc.CustomSerializer != null)
                                {
                                    retValue = _fc.CustomSerializer(reader);
                                }

                                //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);

                                if (retValue is JObject && GetTypeConverters(_objType, name).IsNullOrEmpty()) //  ChoTypeDescriptor.GetTypeConverters(_mi).IsNullOrEmpty())
                                    retValue = ((JObject)retValue).ToObject(objectType, serializer);

                                if (retValue != null)
                                {
                                    if (_fc != null)
                                        ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture);
                                    else
                                        retValue = ChoConvert.ConvertFrom(retValue, objectType, null, 
                                            ChoTypeDescriptor.GetTypeConverters(_mi), 
                                            ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture, config: Configuration);
                                }
                                ValidateORead(ref retValue);
                            }
                            else
                            {
                                if (retValue != null)
                                {
                                    if (retValue is JObject && GetTypeConverters(_objType, name).IsNullOrEmpty())
                                        retValue = ((JObject)retValue).ToObject(objectType, serializer);

                                    if (_fc != null)
                                        ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture, config: Configuration);
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
                                    retValue = ((JObject)retValue).ToObject(objectType, serializer);

                                if (_fc != null)
                                    ChoETLRecordHelper.ConvertMemberValue(rec, name, _fc, ref retValue, _culture, config: Configuration);
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
            bool enableXmlAttributePrefix = JSONConfiguration != null ? JSONConfiguration.EnableXmlAttributePrefix : false;
            bool keepNSPrefix = JSONConfiguration != null ? JSONConfiguration.KeepNSPrefix : false;
            Formatting formatting = JSONConfiguration != null ? JSONConfiguration.Formatting : Formatting.Indented;
            JsonSerializerSettings jsonSerializerSettings = JSONConfiguration != null ? JSONConfiguration.JsonSerializerSettings : null;

            if (crs == null)
            {
                var t = serializer.SerializeToJToken(value, formatting, settings: jsonSerializerSettings, enableXmlAttributePrefix: enableXmlAttributePrefix,
                    keepNSPrefix: keepNSPrefix);
                //if (t != null)
                //    t.WriteTo(writer);
                //else
                    serializer.Serialize(writer, t);
                return;
            }

            if (_fc == null)
            {
                _fc = GetFieldConfiguration(_mi.ReflectedType, _mi.Name);
            }
            if (_fc == null)
            {
                var t = serializer.SerializeToJToken(value, formatting, settings: jsonSerializerSettings, enableXmlAttributePrefix: enableXmlAttributePrefix,
                    keepNSPrefix: keepNSPrefix);
                if (t != null)
                    t.WriteTo(writer);
                else
                    serializer.Serialize(writer, t);
                return;
            }

            crs.Name = _fc.Name;
            crs.FieldConfig = _fc;
            crs.Record = crs.Record == null ? ChoActivator.CreateInstanceNCache(_mi.ReflectedType) : crs.Record;

            IContractResolver contractResolver = GetContractResolver(_fc as IChoJSONRecordFieldConfiguration);
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

                IChoJSONRecordFieldConfiguration jsonFC = _fc as IChoJSONRecordFieldConfiguration;
                if (RaiseBeforeRecordFieldWrite(rec, crs.Index, name, ref value))
                {
                    if (_fc != null)
                    {
                        if (_fc != null)
                            ChoETLRecordHelper.GetNConvertMemberValue(rec, name, _fc, _culture, ref value, true, config: Configuration);
                        else
                        {
                            if (value != null && _objType != null)
                                value = ChoConvert.ConvertTo(value, _objType, null, jsonFC?.PropConverters, jsonFC?.PropConverterParams, _culture);
                        }

                        if (_fc.CustomSerializer == null)
                        {
                            if (_fc.ValueConverter == null)
                            {
                                var t = serializer.SerializeToJToken(value, formatting, settings: jsonSerializerSettings, enableXmlAttributePrefix: enableXmlAttributePrefix,
                    keepNSPrefix: keepNSPrefix);
                                t?.WriteTo(writer);
                            }
                            else
                            {
                                object retValue = _fc.ValueConverter(value);
                                ValidateOnWrite(ref retValue);

                                //ChoETLRecordHelper.DoMemberLevelValidation(retValue, _fc.Name, _fc, _validationMode);
                                JToken t = JToken.FromObject(retValue, serializer);
                                t?.WriteTo(writer);
                            }
                        }
                        else
                        {
                            object retValue = _fc.CustomSerializer(writer);
                            ValidateOnWrite(ref retValue);
                            JToken t = JToken.FromObject(retValue, serializer);
                            t?.WriteTo(writer);
                        }
                    }
                    else
                    {
                        if (_fc != null)
                            ChoETLRecordHelper.GetNConvertMemberValue(rec, name, _fc, _culture, ref value, config: Configuration);
                        else
                        {
                            if (value != null && _objType != null)
                                value = ChoConvert.ConvertTo(value, _objType, null, ChoTypeDescriptor.GetTypeConverters(_mi), ChoTypeDescriptor.GetTypeConverterParams(_mi), _culture);
                        }

                        if (ValidateOnWrite(ref value))
                        {
                            var t = serializer.SerializeToJToken(value, formatting, settings: jsonSerializerSettings, enableXmlAttributePrefix: enableXmlAttributePrefix,
                    keepNSPrefix: keepNSPrefix);
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
    public class ChoJsonPathJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jsonContract = serializer.ContractResolver.ResolveContract(value.GetType()) as JsonObjectContract;
            var contractResolver = serializer.ContractResolver as IChoJsonContractResolver;

            var properties = jsonContract.Properties.Where(e => !e.Ignored && e.Readable).ToArray();
            var result = new JObject();

            foreach (var jsonProperty in properties)
            {
                var propertyPath = jsonProperty.PropertyName.Split('.');
                if (contractResolver != null)
                {
                    var path = contractResolver.JsonPropertiesJsonPaths.Where(kvp => kvp.Key.PropertyName == jsonProperty.PropertyName).Select(kvp => kvp.Value).FirstOrDefault();
                    if (!path.IsObjectNullOrEmpty())
                        propertyPath = path.Split('.');
                }

                JObject currentLevel = result;
                for (int i = 0; i < propertyPath.Length - 1; ++i)
                {
                    if (currentLevel[propertyPath[i]] == null)
                        currentLevel[propertyPath[i]] = new JObject();

                    currentLevel = (JObject)currentLevel[propertyPath[i]];
                }

                JToken propretyValueToken;
                if (jsonProperty.Converter != null && jsonProperty.Converter.CanWrite)
                {
                    using (var stringWriter = new StringWriter())
                    using (var jsonWriter = new JsonTextWriter(stringWriter))
                    {
                        jsonProperty.Converter.WriteJson(jsonWriter, jsonProperty.ValueProvider.GetValue(value), serializer);
                        propretyValueToken = JToken.Parse(stringWriter.ToString());
                    }
                }
                else
                {
                    var val = jsonProperty.ValueProvider.GetValue(value);
                    propretyValueToken = val == null ? null : JToken.FromObject(val);
                }

                currentLevel[propertyPath[propertyPath.Length - 1]] = propretyValueToken;
            }

            serializer.Serialize(writer, result);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JToken.Load(reader);

            var jsonContract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            var contractResolver = serializer.ContractResolver as IChoJsonContractResolver;

            var result = jsonContract.DefaultCreator();
            var properties = jsonContract.Properties.Where(e => !e.Ignored && e.Writable).ToArray();

            foreach (JsonProperty prop in properties) //jsonContract.Properties.Where(p => p.Writable && !p.Ignored))
            {
                string jsonPath = prop.PropertyName;
                if (contractResolver != null)
                {
                    var path = contractResolver.JsonPropertiesJsonPaths.Where(kvp => kvp.Key.PropertyName == prop.PropertyName).Select(kvp => kvp.Value).FirstOrDefault();
                    if (!path.IsObjectNullOrEmpty())
                        jsonPath = path;
                }
                //if (!Regex.IsMatch(jsonPath, "^[a-zA-Z0-9_.-]+$"))
                //    throw new InvalidOperationException(
                //        string.Format("JProperties of JsonPathConverter can have only letters, numbers, underscores, hyphens and dots but name was {0}.", jsonPath)); // Array operations not permitted

                JToken token = jsonObject.SelectToken(jsonPath);

                if (token != null && token.Type != JTokenType.Null)
                {
                    object value;
                    if (prop.Converter == null || !prop.Converter.CanRead)
                    {
                        value = token.ToObject(prop.PropertyType, serializer);
                    }
                    else
                    {
                        var r = token.CreateReader();
                        r.Read();
                        value = prop.Converter.ReadJson(r, prop.PropertyType, null, serializer);
                    }

                    prop.ValueProvider.SetValue(result, value);
                }
            }

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            var contract =
                JsonSerializer.Create().ContractResolver.ResolveContract(objectType);

            return contract is JsonObjectContract &&
                   (contract as JsonObjectContract).Properties.Any(e => !e.Ignored && e.PropertyName.Contains('.'));
        }
    }

    public interface IChoJSONRecordConfiguration
    {
        Func<Type, MemberInfo, string, bool?> IgnoreProperty { get; set; }
        Func<Type, MemberInfo, string, string> RenameProperty { get; set; }
        Action<Type, MemberInfo, string, JsonProperty> RemapJsonProperty { get; set; }
        void MapRecordFieldsForType(Type rt);
        JsonLoadSettings JsonLoadSettings
        {
            get;
            set;
        }
        ChoJObjectLoadOptions? JObjectLoadOptions
        {
            get;
            set;
        }
        Func<JsonReader, JsonLoadSettings, JObject> CustomJObjectLoader
        {
            get;
            set;
        }

        Func<JsonReader, JsonLoadSettings, JArray> CustomJArrayLoader
        {
            get;
            set;
        }
        Type UnknownType
        {
            get;
            set;
        }

        Func<JObject, object> UnknownTypeConverter
        {
            get;
            set;
        }

        bool EnableXmlAttributePrefix { get; set; }
        bool KeepNSPrefix { get; set; }
        Formatting Formatting { get; set; }
        JsonSerializerSettings JsonSerializerSettings { get; set; }
        Func<object, JToken> ObjectToJTokenConverter { get; set; }
        bool ContainsRecordConfigForType(Type rt);
        Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt);
    }

    public interface IChoJSONRecordFieldConfiguration
    {
        IContractResolver ContractResolver { get; set; }
        string JSONPath { get; set; }
        PropertyDescriptor PD
        {
            get;
            set;
        }
        string DeclaringMember
        {
            get;
            set;
        }
        object[] GetConverters();
        object[] PropConverters
        {
            get;
            set;
        }
        object[] PropConverterParams
        {
            get;
            set;
        }
    }

    public interface IChoJsonContractResolver
    {
        ChoFileRecordConfiguration RecordConfiguration { get; }
        Dictionary<MemberInfo, JsonProperty> JsonProperties { get; }
        Dictionary<JsonProperty, string> JsonPropertiesJsonPaths { get; }
    }


}
