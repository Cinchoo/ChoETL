using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Flags]
    public enum ChoJObjectLoadOptions
    {
        All = 0,
        None = 1,
        ExcludeArrays = 2,
        ExcludeNestedObjects = 4,
    }

    [DataContract]
    public class ChoJSONRecordConfiguration : ChoFileRecordConfiguration, IChoDynamicObjectRecordConfiguration, IChoJSONRecordConfiguration
    {
        internal readonly Dictionary<Type, Dictionary<string, ChoJSONRecordFieldConfiguration>> JSONRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoJSONRecordFieldConfiguration>>();
        internal readonly Dictionary<Type, Func<object, object>> NodeConvertersForType = new Dictionary<Type, Func<object, object>>();

        public Func<string, object, bool?> JsonArrayQualifier
        {
            get;
            set;
        }
        public Func<string, bool?> IsNodeCanBeArray
        {
            get;
            set;
        }
        public Func<string, string, string> NestedKeyResolver
        {
            get;
            set;
        }
        public long MaxJArrayItemsLoad
        {
            get;
            set;
        }
        public ChoJObjectLoadOptions? JObjectLoadOptions
        {
            get;
            set;
        }

        public Func<JsonReader, JsonLoadSettings, JObject> CustomJObjectLoader
        {
            get;
            set;
        }

        public Func<JsonReader, JsonLoadSettings, JArray> CustomJArrayLoader
        {
            get;
            set;
        }
        public bool UseImplicitJArrayLoader { get; set; } = true;

        public JsonLoadSettings JsonLoadSettings
        {
            get;
            set;
        }

        public string LineBreakChars
        {
            get;
            set;
        }

        public bool FlattenNode
        {
            get;
            set;
        }

        public bool? DefaultArrayHandling
        {
            get;
            set;
        }

        public bool AllowComplexJSONPath
        {
            get;
            set;
        }

        public Type UnknownType
        {
            get;
            set;
        }

        public Func<JObject, object> UnknownTypeConverter
        {
            get;
            set;
        }

        [DataMember]
        public List<ChoJSONRecordFieldConfiguration> JSONRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public string JSONPath
        {
            get;
            set;
        }
        [DataMember]
        public bool UseJSONSerialization
        {
            get;
            set;
        }
        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }
        public Action<JsonSerializerSettings> InspectConverters { get; set; }
        public ChoPropertyRenameAndIgnoreSerializerContractResolver JSONSerializerContractResolver
        {
            get
            {
                return JsonSerializerSettings == null ? null : JsonSerializerSettings.ContractResolver as ChoPropertyRenameAndIgnoreSerializerContractResolver;
            }
        }
        private readonly object _padLock = new object();
        private JsonSerializerSettings _jsonSerializerSettings = null;
        public JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                if (_jsonSerializerSettings != null)
                    return _jsonSerializerSettings;

                lock (_padLock)
                {
                    if (_jsonSerializerSettings != null)
                        return _jsonSerializerSettings;

                    if (true) //JSONRecordFieldConfigurationsForType.Count > 0)
                    {
                        _jsonSerializerSettings = new JsonSerializerSettings();
                        var useDefaultContractResolver = false;
                        if (UseDefaultContractResolver == null)
                        {
                            if (!UseJSONSerialization)
                                useDefaultContractResolver = true;
                        }
                        else
                            useDefaultContractResolver = UseDefaultContractResolver.Value;

                        if (useDefaultContractResolver)
                        {
                            var jsonResolver = new ChoPropertyRenameAndIgnoreSerializerContractResolver(this);
                            if (DefaultContractResolverSetup != null)
                                DefaultContractResolverSetup(jsonResolver);

                            _jsonSerializerSettings.ContractResolver = jsonResolver;
                        }

                        //Add built-in converters
                        if (!TurnOffBuiltInJsonConverters)
                        {
                            foreach (var conv in ChoPropertyRenameAndIgnoreSerializerContractResolver.BuiltInConverters)
                            {
                                try
                                {
                                    if (conv != null && !_jsonSerializerSettings.Converters.Contains(conv))
                                        _jsonSerializerSettings.Converters.Add(conv);
                                }
                                catch { }
                            }
                        }

                        //_jsonSerializerSettings.Converters = GetJSONConverters();
                    }
                    //Attach field converters if any
                    foreach (var fc in JSONRecordFieldConfigurations)
                    {
                        try
                        {
                            var conv = fc.GetJsonConverterIfAny();
                            if (conv != null && !_jsonSerializerSettings.Converters.Contains(conv))
                                _jsonSerializerSettings.Converters.Add(conv);
                        }
                        catch { }
                    }

                    if (_jsonSerializerSettings.Context.Context == null)
                        _jsonSerializerSettings.Context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All, new ChoDynamicObject());

                    if (InspectConverters != null)
                        InspectConverters(_jsonSerializerSettings);

                    return _jsonSerializerSettings;
                }
            }
            set
            {
                _jsonSerializerSettings = value;
                if (value != null && _formatting == null)
                    _formatting = _jsonSerializerSettings.Formatting;
            }
        }
        internal ChoTypeConverterFormatSpec CreateTypeConverterSpecsIfNull()
        {
            if (_typeConverterFormatSpec == null)
                _typeConverterFormatSpec = new ChoTypeConverterFormatSpec();

            return _typeConverterFormatSpec;
        }

        internal bool? IsArray(ChoJSONRecordFieldConfiguration fc, object fieldValue = null)
        {
            if (IsNodeCanBeArray == null)
            {
                if (fc == null || fc.IsArray == null)
                {
                    var arrayHandling = DefaultArrayHandling;
                    arrayHandling = ChoDynamicObjectSettings.IsJsonArray(fc.FieldName, fieldValue, JsonArrayQualifier);
                    return arrayHandling;
                }
                else
                    return fc.IsArray;
            }
            else
                return IsNodeCanBeArray(fc.FieldName);
        }

        private readonly Lazy<List<JsonConverter>> _JSONConverters;
        private List<JsonConverter> GetJSONConverters()
        {
            return _JSONConverters.Value;
        }

        private bool? _turnOnAutoDiscoverJsonConverters = null;
        public bool TurnOnAutoDiscoverJsonConverters
        {
            get { return _turnOnAutoDiscoverJsonConverters == null ? ChoETLFrxBootstrap.TurnOnAutoDiscoverJsonConverters : _turnOnAutoDiscoverJsonConverters.Value; }
            set { _turnOnAutoDiscoverJsonConverters = value; }
        }

        private Lazy<JsonSerializer> _JsonSerializer = null;
        private JsonSerializer _externalJsonSerializer = null;
        public JsonSerializer JsonSerializer
        {
            get { return _externalJsonSerializer == null ? _JsonSerializer.Value : _externalJsonSerializer; }
            set { _externalJsonSerializer = value; }
        }

        [DataMember]
        public bool? SupportMultipleContent
        {
            get;
            set;
        }

        private Newtonsoft.Json.Formatting? _formatting = null;
        [DataMember]
        public Newtonsoft.Json.Formatting Formatting
        {
            get { return _formatting == null ? Formatting.Indented : _formatting.Value; }
            set { _formatting = value; }
        }
        internal bool ResetMapping { get; set; }

        private bool _flatToNestedObjectSupport;
        public bool FlatToNestedObjectSupport
        {
            get { return _flatToNestedObjectSupport; }
            set
            {
                if (_flatToNestedObjectSupport != value)
                {
                    _flatToNestedObjectSupport = value;
                    ResetMapping = true;
                }
            }
        }
        //public override bool IsDynamicObject
        //{
        //    get { return base.IsDynamicObject || RecordType == typeof(object); }
        //    set { base.IsDynamicObject  = value; }
        //}

        public bool IgnoreNodeName
        {
            get;
            set;
        }
        public string NodeName
        {
            get;
            set;
        }
        public bool IgnoreRootName
        {
            get;
            set;
        }
        public string RootName
        {
            get;
            set;
        }
        public bool? SingleElement
        {
            get;
            set;
        }
        public bool EnableXmlAttributePrefix { get; set; }
        public bool KeepNSPrefix { get; set; }
        public Func<Type, MemberInfo, string, bool?> IgnoreProperty { get; set; }
        public Func<Type, MemberInfo, string, string> RenameProperty { get; set; }
        public Action<Type, MemberInfo, string, JsonProperty> RemapJsonProperty { get; set; }

        private Func<JObject, JObject> _customNodeSelecter = null;
        public Func<JObject, JObject> CustomNodeSelecter
        {
            get { return _customNodeSelecter; }
            set { if (value == null) return; _customNodeSelecter = value; }
        }

        internal Dictionary<string, ChoJSONRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }

        internal bool IsInitialized
        {
            get;
            set;
        }
        internal string DataSetName
        {
            get;
            set;
        }
        public bool? UseDefaultContractResolver
        {
            get;
            set;
        }
        public Action<ChoPropertyRenameAndIgnoreSerializerContractResolver> DefaultContractResolverSetup
        {
            get;
            set;
        }
        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in JSONRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public bool TurnOffBuiltInJsonConverters { get; private set; }
        public bool IgnoreArrayIndex { get; set; } = true;
        public string FlattenByNodeName { get; set; }
        public string FlattenByJsonPath { get; set; }
        public Func<object, JToken> ObjectToJTokenConverter { get; set; }
        public bool FlattenIfJArrayWhenReading { get; set; } = true;
        internal bool IsDynamicObjectInternal
        {
            get => IsDynamicObject;
            set => IsDynamicObject = value;
        }
        public static new int MaxLineSize
        {
            get { throw new NotSupportedException(); }
        }
        //public static new string EOLDelimiter
        //{
        //    get { throw new NotSupportedException(); }
        //}
        public static new string MayContainEOLInData
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool IgnoreEmptyLine
        {
            get { throw new NotSupportedException(); }
        }
        //public static new bool ColumnCountStrict
        //{
        //    get { throw new NotSupportedException(); }
        //}
        public static new bool ColumnOrderStrict
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool EscapeQuoteAndDelimiter
        {
            get { throw new NotSupportedException(); }
        }
        public static new string Comment
        {
            get { throw new NotSupportedException(); }
        }
        public static new string[] Comments
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool LiteParsing
        {
            get;
            set;
        }
        public static new bool? QuoteAllFields
        {
            get;
            set;
        }
        public static new bool? QuoteChar
        {
            get;
            set;
        }
        public static new bool? QuoteEscapeChar
        {
            get;
            set;
        }
        public static new bool QuoteLeadingAndTrailingSpaces
        {
            get;
            set;
        }
        public static new bool? MayHaveQuotedFields
        {
            get { return QuoteAllFields; }
            set { QuoteAllFields = value; }
        }
        public static new ChoStringSplitOptions StringSplitOptions
        {
            get;
            set;
        }
        internal Type SourceTypeInternal
        {
            get => SourceType;
            set => SourceType = value;
        }
        internal bool RecordTypeMappedInternal
        {
            get => RecordTypeMapped;
            set => RecordTypeMapped = value;
        }
        internal Type RecordTypeInternal
        {
            get => RecordType;
            set => RecordType = value;
        }
        internal Type RecordMapTypeInternal => RecordMapType;
        internal Dictionary<string, PropertyInfo> PIDictInternal
        {
            get => PIDict;
            set => PIDict = value;
        }
        internal Dictionary<string, PropertyDescriptor> PDDictInternal
        {
            get => PDDict;
            set => PDDict = value;
        }

        public ChoJSONRecordFieldConfiguration this[string name]
        {
            get
            {
                return JSONRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoJSONRecordConfiguration() : this(null)
        {

        }

        internal ChoJSONRecordConfiguration(Type recordType) : base(recordType)
        {
            _JsonSerializer = new Lazy<JsonSerializer>(() =>
            {
                var se = JsonSerializerSettings == null ? null : JsonSerializer.Create(JsonSerializerSettings);
                return se;
            });
            _JSONConverters = new Lazy<List<JsonConverter>>(() =>
            {
                List<JsonConverter> converters = new List<JsonConverter>();
                converters.Add(new ExpandoObjectConverter());
                converters.Add(ChoDynamicObjectConverter.Instance);

                //foreach (var kvp in NodeConvertersForType)
                //{
                //    if (kvp.Value == null) continue;
                //    converters.Add(ChoActivator.CreateInstance(typeof(ChoJSONNodeConverter<>).MakeGenericType(kvp.Key), kvp.Value) as JsonConverter);

                //    ChoTypeConverter.Global.Add(kvp.Key, ChoActivator.CreateInstance(typeof(ChoJSONTypeConverter<>).MakeGenericType(kvp.Key), kvp.Value) as IChoValueConverter);
                //}

                return converters;
            });

            DefaultArrayHandling = recordType == null || recordType.IsDynamicType() ? true : false;
            JSONRecordFieldConfigurations = new List<ChoJSONRecordFieldConfiguration>();
            WithJSONConverter(ChoDynamicObjectConverter.Instance);

            LineBreakChars = Environment.NewLine;
            Formatting = Newtonsoft.Json.Formatting.Indented;
            if (recordType != null)
            {
                Init(recordType);
            }
        }

        internal void AddFieldForType(Type rt, ChoJSONRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!JSONRecordFieldConfigurationsForType.ContainsKey(rt))
                JSONRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoJSONRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (JSONRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                JSONRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                JSONRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        protected virtual new bool ContainsRecordConfigForType(Type rt)
        {
            return JSONRecordFieldConfigurationsForType.ContainsKey(rt);
        }
        internal bool ContainsRecordConfigForTypeInternal(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }
        protected override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return JSONRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        protected override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return JSONRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }
        internal Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForTypeInternal(Type rt)
        {
            return GetRecordConfigDictionaryForType(rt);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            if (recordType == null)
                return;


            var pd = ChoTypeDescriptor.GetTypeAttribute<ChoJSONPathAttribute>(recordType);
            if (pd != null)
            {
                JSONPath = pd.JSONPath;
                AllowComplexJSONPath = pd.AllowComplexJSONPath;
            }

            ChoJSONRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoJSONRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
            }

            if (JSONRecordFieldConfigurations.Count == 0)
                MapRecordFields(); // DiscoverRecordFields(recordType, false);
        }

        internal void Reset()
        {
            IsInitialized = false;
            JSONRecordFieldConfigurations.Clear();
        }

        internal void UpdateFieldTypesIfAny(IDictionary<string, Type> dict)
        {
            if (dict == null || RecordFieldConfigurationsDict == null)
                return;

            foreach (var key in dict.Keys)
            {
                if (RecordFieldConfigurationsDict.ContainsKey(key) && dict[key] != null)
                    RecordFieldConfigurationsDict[key].FieldType = dict[key];
            }
            AreAllFieldTypesNull = RecordFieldConfigurationsDict.All(kvp => kvp.Value.FieldType == null);
        }

        public ChoJSONRecordConfiguration MapRecordFields<T>()
        {
            MapRecordFields(typeof(T));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                JSONRecordFieldConfigurationsForType.Remove(rt);
        }

        public void ClearRecordFieldsForType<T>()
        {
            ClearRecordFieldsForType(typeof(T));
        }

        public void MapRecordFieldsForType<T>()
        {
            MapRecordFieldsForType(typeof(T));
        }

        public ChoJSONRecordConfiguration ConfigureTypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            CreateTypeConverterSpecsIfNull();
            spec?.Invoke(TypeConverterFormatSpec);
            return this;
        }

        public void MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                return;

            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = new List<ChoJSONRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            JSONRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        public ChoJSONRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault());
            foreach (var rt in recordTypes.Where(rt => rt != null).Skip(1))
                DiscoverRecordFields(rt, false, JSONRecordFieldConfigurations);
            return this;
        }

        //public ChoJSONRecordConfiguration UseDefaultContractResolver(bool flag = true, Action<ChoPropertyRenameAndIgnoreSerializerContractResolver> setup = null)
        //{
        //    if (flag)
        //    {
        //        var jsonResolver = new ChoPropertyRenameAndIgnoreSerializerContractResolver(this);
        //        JsonSerializerSettings.ContractResolver = jsonResolver;
        //        if (setup != null)
        //            setup(jsonResolver);
        //    }
        //    else
        //        JsonSerializerSettings.ContractResolver = null;

        //    return this;
        //}
        public ChoJSONRecordConfiguration ConfigureContractResolver(Action<IContractResolver> setup = null)
        {
            if (setup != null && JsonSerializerSettings.ContractResolver != null)
                setup(JsonSerializerSettings.ContractResolver);
            return this;
        }
        public void MapRecordFields()
        {
            /*RecordType =*/
            DiscoverRecordFields(RecordType, false, null, true);
        }

        private Type DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = JSONRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            return DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any(), recordFieldConfigurations, isTop);
        }

        private Type DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false,
            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        bool optIn1 = ChoTypeDescriptor.GetProperties(pt).Where(pd1 => pd1.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any();
                        if (optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn1, recordFieldConfigurations, false);
                        }
                        else if (pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoJSONRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                {
                                    LoadFieldConfigurationAttributes(obj, recordType);
                                    recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (isTop)
                    {
                        if (typeof(IList).IsAssignableFrom(recordType) || (recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>)))
                        {
                            throw new ChoParserException("Record type not supported.");
                        }
                        else if (typeof(IDictionary<string, object>).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                        else if (typeof(IDictionary).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                    }

                    if (recordType.IsSimple())
                    {
                        var obj = new ChoJSONRecordFieldConfiguration("Value", "$.Value");
                        obj.FieldType = recordType;

                        LoadFieldConfigurationAttributes(obj, recordType);
                        recordFieldConfigurations.Add(obj);
                        return recordType;
                    }

                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        JsonIgnoreAttribute jiAttr = pd.Attributes.OfType<JsonIgnoreAttribute>().FirstOrDefault();
                        if (jiAttr != null)
                            continue;

                        pt = pd.PropertyType.GetUnderlyingType();
                        if (FlatToNestedObjectSupport && ((pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            || (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()))))
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, recordFieldConfigurations, false);
                        }
                        else
                        {
                            var obj = new ChoJSONRecordFieldConfiguration(pd.Name, ChoTypeDescriptor.GetPropetyAttribute<ChoJSONRecordFieldAttribute>(pd),
                                pd.Attributes.OfType<Attribute>().ToArray());

                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;
                            ChoUseJSONSerializationAttribute sAttr = pd.Attributes.OfType<ChoUseJSONSerializationAttribute>().FirstOrDefault();
                            if (sAttr != null)
                                obj.UseJSONSerialization = sAttr.Flag;
                            ChoJSONPathAttribute jpAttr = pd.Attributes.OfType<ChoJSONPathAttribute>().FirstOrDefault();
                            if (jpAttr != null)
                                obj.JSONPath = jpAttr.JSONPath;

                            JsonPropertyAttribute jAttr = pd.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                            if (jAttr != null && !jAttr.PropertyName.IsNullOrWhiteSpace())
                            {
                                obj.FieldName = jAttr.PropertyName;
                                if (obj.JSONPath.IsNullOrWhiteSpace())
                                    obj.JSONPath = jAttr.PropertyName;
                                obj.Order = jAttr.Order;
                                obj.NullValueHandling = jAttr.NullValueHandling;
                            }
                            else
                            {
                                DisplayNameAttribute dnAttr = pd.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                                if (dnAttr != null && !dnAttr.DisplayName.IsNullOrWhiteSpace())
                                {
                                    obj.FieldName = dnAttr.DisplayName.Trim();
                                }
                                else
                                {
                                    DisplayAttribute dpAttr = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                                    if (dpAttr != null)
                                    {
                                        if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                                            obj.FieldName = dpAttr.ShortName;
                                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                                            obj.FieldName = dpAttr.Name;

                                        obj.Order = dpAttr.Order;
                                    }
                                    else
                                    {
                                        ColumnAttribute clAttr = pd.Attributes.OfType<ColumnAttribute>().FirstOrDefault();
                                        if (clAttr != null)
                                        {
                                            obj.Order = clAttr.Order;
                                            if (!clAttr.Name.IsNullOrWhiteSpace())
                                                obj.FieldName = clAttr.Name;
                                        }
                                    }
                                }
                            }
                            DisplayFormatAttribute dfAttr = pd.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
                            if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                            {
                                obj.FormatText = dfAttr.DataFormatString;
                            }
                            if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                            {
                                obj.NullValue = dfAttr.NullDisplayText;
                            }

                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                {
                                    LoadFieldConfigurationAttributes(obj, recordType);
                                    recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
            return recordType;
        }
        internal void ValidateInternal(object state)
        {
            Validate(state);
        }

        protected override void Validate(object state)
        {
            if (TurnOnAutoDiscoverJsonConverters)
                ChoJSONConvertersCache.Init();

            if (_jsonSerializerSettings != null)
            {
                foreach (var conv in GetJSONConverters())
                {
                    if (!_jsonSerializerSettings.Converters.Contains(conv))
                    {
                        if (conv is IChoJSONConverter)
                        {
                            var c1 = ChoActivator.CreateInstance(conv.GetType()) as JsonConverter;
                            if (c1 != null)
                                _jsonSerializerSettings.Converters.Add(c1);
                        }
                        else
                            _jsonSerializerSettings.Converters.Add(conv);
                    }
                }
                if (TurnOnAutoDiscoverJsonConverters)
                {
                    foreach (var conv in ChoJSONConvertersCache.GetAll().Select(kvp => kvp.Value))
                    {
                        if (!_jsonSerializerSettings.Converters.Contains(conv))
                        {
                            if (conv is IChoJSONConverter)
                            {
                                var c1 = ChoActivator.CreateInstance(conv.GetType()) as JsonConverter;
                                if (c1 != null)
                                    _jsonSerializerSettings.Converters.Add(c1);
                            }
                            else
                                _jsonSerializerSettings.Converters.Add(conv);
                        }
                    }
                }
                if (InspectConverters != null)
                    InspectConverters(_jsonSerializerSettings);

                foreach (var conv in _jsonSerializerSettings.Converters.OfType<IChoJSONConverter>())
                {
                    conv.Serializer = JsonSerializer;
                    conv.Context = new ChoDynamicObject();
                    conv.Context.Configuration = this;
                }

                foreach (var conv in ChoTypeConverter.Global.GetAll().Select(kvp => kvp.Value).ToArray().OfType<IChoJSONConverter>())
                {
                    conv.Serializer = JsonSerializer;
                    conv.Context = new ChoDynamicObject();
                    conv.Context.Configuration = this;
                }
                
                foreach (var conv in _jsonSerializerSettings.Converters)
                {
                    if (!JsonSerializer.Converters.Contains(conv))
                        JsonSerializer.Converters.Add(conv);
                }
            }

            if (RecordType != null)
            {
                if (ResetMapping)
                {
                    JSONRecordFieldConfigurations.Clear();
                    Init(RecordType);
                    ResetMapping = false;
                }
            }

            base.Validate(state);

            string[] fieldNames = null;
            JObject jObject = null;
            if (state is Tuple<long, JObject>)
                jObject = ((Tuple<long, JObject>)state).Item2;
            else
                fieldNames = state as string[];

            if (fieldNames != null && JSONRecordFieldConfigurations.Count > 0 && FlattenNode)
                JSONRecordFieldConfigurations.Clear();

            if (AutoDiscoverColumns
                && JSONRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    /*&& ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any()*/)
                {
                    MapRecordFields(RecordType);
                }
                else if (jObject != null)
                {
                    Dictionary<string, ChoJSONRecordFieldConfiguration> dict = new Dictionary<string, ChoJSONRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    foreach (var attr in jObject.Properties())
                    {
                        name = attr.Name;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoJSONRecordFieldConfiguration(name, (string)null));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    foreach (ChoJSONRecordFieldConfiguration obj in dict.Values)
                    {
                        LoadFieldConfigurationAttributes(obj, RecordType);
                        JSONRecordFieldConfigurations.Add(obj);
                    }
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoJSONRecordFieldConfiguration(fn, (string)null);
                        LoadFieldConfigurationAttributes(obj, RecordType);
                        JSONRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            else
            {
                foreach (var fc in JSONRecordFieldConfigurations)
                {
                    fc.ComplexJPathUsed = !(fc.JSONPath.IsNullOrWhiteSpace() || String.Compare(fc.FieldName, fc.JSONPath, true) == 0);
                }
            }

            //if (JSONRecordFieldConfigurations.Count <= 0)
            //    throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            foreach (var fieldConfig in JSONRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = JSONRecordFieldConfigurations.GroupBy(i => i.FieldName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(String.Join(",", dupFields)));

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in JSONRecordFieldConfigurations)
            {
                var pd1 = fc.DeclaringMemberInternal.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
                    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMemberInternal);
                if (pd1 != null)
                    fc.PropertyDescriptorInternal = pd1;

                if (fc.PropertyDescriptorInternal == null)
                    fc.PropertyDescriptorInternal = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptorInternal == null)
                    continue;

                PIDict.Add(fc.Name, fc.PropertyDescriptorInternal.ComponentType.GetProperty(fc.PropertyDescriptorInternal.Name));
                PDDict.Add(fc.Name, fc.PropertyDescriptorInternal);
            }

            RecordFieldConfigurationsDict = JSONRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            LoadNCacheMembers(JSONRecordFieldConfigurations);

            //Load converters and attach them to 
        }

        #region Fluent API

        public ChoJSONRecordConfiguration WithJSONConverter(JsonConverter converter)
        {
            ChoJSONConvertersCache.Add(converter);
            return this;
        }

        public ChoJSONRecordConfiguration RegisterNodConverterForType<ModelType>(Func<object, object> selector)
        {
            return RegisterNodeConverterForType(typeof(ModelType), selector);
        }

        public ChoJSONRecordConfiguration RegisterNodeConverterForType(Type type, Func<object, object> selector)
        {
            if (type == null || selector == null)
                return this;

            if (NodeConvertersForType.ContainsKey(type))
                NodeConvertersForType[type] = selector;
            else
                NodeConvertersForType.Add(type, selector);

            return this;
        }

        internal bool HasNodeConverterForType(Type type, out Func<object, object> selector)
        {
            selector = null;
            if (type == null)
                return false;

            if (NodeConvertersForType.ContainsKey(type))
            {
                selector = NodeConvertersForType[type];
                return true;
            }

            return false;
        }

        public ChoJSONRecordConfiguration Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoJSONRecordConfiguration ClearFields()
        {
            JSONRecordFieldConfigurationsForType.Clear();
            JSONRecordFieldConfigurations.Clear();
            base.ClearFields();
            return this;
        }

        public ChoJSONRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (field != null)
            {
                if (JSONRecordFieldConfigurations.Count == 0)
                    MapRecordFields<T>();

                var fc = JSONRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == field.GetFullyQualifiedMemberName()).FirstOrDefault();
                if (fc != null)
                    JSONRecordFieldConfigurations.Remove(fc);
            }

            return this;
        }

        public ChoJSONRecordConfiguration IgnoreField(string fieldName)
        {
            if (fieldName != null)
            {
                var fc = JSONRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == fieldName || f.FieldName == fieldName).FirstOrDefault();
                if (fc != null)
                    JSONRecordFieldConfigurations.Remove(fc);
                else
                    IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoJSONRecordConfiguration Map(string propertyName, string jsonPath = null, string fieldName = null, Type fieldType = null)
        {
            Map(propertyName, m => m.JSONPath(jsonPath).FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoJSONRecordConfiguration Map(string propertyName, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoJSONRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoJSONRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string jsonPath = null, string fieldName = null)
        {
            Map(field, m => m.JSONPath(jsonPath).FieldName(fieldName));
            return this;
        }

        public ChoJSONRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoJSONRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, null /* subType == typeof(T) ? null : subType */);
            mapper?.Invoke(new ChoJSONRecordFieldConfigurationMap(cf));
            return this;
        }

        #endregion Fluent API

        internal void WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool? isArray = null, string nullValue = null, Type recordType = null,
            Type subRecordType = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null, IChoValueConverter propertyConverter = null
            )
        {
            ChoGuard.ArgumentNotNull(recordType, nameof(recordType));

            if (!name.IsNullOrEmpty())
            {
                if (subRecordType != null)
                    MapRecordFieldsForType(subRecordType);

                string fnTrim = name.NTrim();
                ChoJSONRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    JSONRecordFieldConfigurations.Remove(fc);
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }
                else if (subRecordType != null)
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, pd != null ? ChoTypeDescriptor.GetPropetyAttribute<ChoJSONRecordFieldAttribute>(pd) : null,
                                pd != null ? pd.Attributes.OfType<Attribute>().ToArray() : null)
                {
                };
                nfc.JSONPath = !jsonPath.IsNullOrWhiteSpace() ? jsonPath : nfc.JSONPath;
                nfc.FieldType = fieldType != null ? fieldType : nfc.FieldType;
                nfc.FieldValueTrimOption = fieldValueTrimOption;
                nfc.FieldName = !fieldName.IsNullOrWhiteSpace() ? fieldName : (!nfc.FieldName.IsNullOrWhiteSpace() ? nfc.FieldName : name);
                nfc.ValueConverter = valueConverter != null ? valueConverter : nfc.ValueConverter;
                nfc.CustomSerializer = customSerializer != null ? customSerializer : nfc.CustomSerializer;
                nfc.DefaultValue = defaultValue != null ? defaultValue : nfc.DefaultValue;
                nfc.FallbackValue = fallbackValue != null ? fallbackValue : nfc.FallbackValue;
                nfc.FormatText = !formatText.IsNullOrWhiteSpace() ? formatText : nfc.FormatText;
                nfc.ItemConverter = itemConverter != null ? itemConverter : nfc.ItemConverter;
                nfc.IsArray = isArray != null ? isArray : nfc.IsArray;
                nfc.NullValue = !nullValue.IsNullOrWhiteSpace() ? nullValue : nfc.NullValue;
                nfc.FieldTypeSelector = fieldTypeSelector != null ? fieldTypeSelector : nfc.FieldTypeSelector;
                nfc.ItemRecordTypeSelector = itemTypeSelector != null ? itemTypeSelector : nfc.ItemRecordTypeSelector;
                nfc.FieldTypeDiscriminator = fieldTypeDiscriminator != null ? fieldTypeDiscriminator : nfc.FieldTypeDiscriminator;
                nfc.ItemTypeDiscriminator = itemTypeDiscriminator != null ? itemTypeDiscriminator : nfc.ItemTypeDiscriminator;
                if (propertyConverter != null) nfc.AddConverter(propertyConverter);

                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : fullyQualifiedMemberName;
                }
                else
                {
                    if (subRecordType == null)
                        pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName);
                    else
                        pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName);

                    nfc.PropertyDescriptorInternal = pd;
                    nfc.DeclaringMemberInternal = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                if (subRecordType == null)
                {
                    LoadFieldConfigurationAttributes(nfc, recordType);
                    JSONRecordFieldConfigurations.Add(nfc);
                }
                else
                {
                    LoadFieldConfigurationAttributes(nfc, subRecordType);
                    AddFieldForType(subRecordType, nfc);
                }
            }
        }

        internal ChoJSONRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoJSONRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoJSONRecordFieldConfiguration(propertyName, attr, otherAttrs);
                fc.PropertyDescriptorInternal = pd;
                fc.DeclaringMemberInternal = fqm;
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                if (!JSONRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                {
                    var fc = new ChoJSONRecordFieldConfiguration(propertyName, attr, otherAttrs);
                    LoadFieldConfigurationAttributes(fc, subType);
                    JSONRecordFieldConfigurations.Add(fc);
                }

                var nfc = JSONRecordFieldConfigurations.First(fc => fc.Name == propertyName);
                nfc.PropertyDescriptorInternal = pd;
                nfc.DeclaringMemberInternal = fqm;

                return nfc;
            }
        }
        internal new void LoadFieldConfigurationAttributes(ChoRecordFieldConfiguration fc, Type reflectedType)
        {
            base.LoadFieldConfigurationAttributes(fc, reflectedType);
        }

        internal ChoJSONRecordFieldConfiguration GetFieldConfiguration(string fn)
        {
            fn = fn.NTrim();
            if (!JSONRecordFieldConfigurations.Any(fc => fc.Name == fn))
            {
                var fc = new ChoJSONRecordFieldConfiguration(fn, (string)null);
                LoadFieldConfigurationAttributes(fc, RecordType);
                JSONRecordFieldConfigurations.Add(fc);
            }

            return JSONRecordFieldConfigurations.First(fc => fc.Name == fn);
        }

        internal ChoJSONRecordFieldConfiguration GetFieldConfigurationForType(Type type, string fn)
        {
            fn = fn.NTrim();
            if (ContainsRecordConfigForTypeInternal(type) && GetRecordConfigForType(type).Any(fc => fc.Name == fn))
                return GetRecordConfigForType(type).FirstOrDefault(fc => fc.Name == fn) as ChoJSONRecordFieldConfiguration;

            return null;
        }
        internal Encoding GetEncodingInternal(Stream inStream)
        {
            return GetEncoding(inStream);
        }
        internal Encoding GetEncodingInternal(string fileName)
        {
            return GetEncoding(fileName);
        }
        internal void ResetStatesInternal()
        {
            ResetStates();
        }

        bool IChoJSONRecordConfiguration.ContainsRecordConfigForType(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }

        Dictionary<string, ChoRecordFieldConfiguration> IChoJSONRecordConfiguration.GetRecordConfigDictionaryForType(Type rt)
        {
            return GetRecordConfigDictionaryForType(rt);
        }

        object[] IChoDynamicObjectRecordConfiguration.GetConvertersForType(Type fieldType, object value)
        {
            return GetConvertersForTypePrivate(fieldType, value);
        }

        object[] IChoDynamicObjectRecordConfiguration.GetConverterParamsForType(Type fieldType, object value = null)
        {
            return GetConverterParamsForTypePrivate(fieldType, value);
        }

        //protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        //{
        //    base.LoadNCacheMembers(fcs);

        //    if (!IsDynamicObject)
        //    {
        //        foreach (var fc in fcs.OfType<ChoJSONRecordFieldConfiguration>())
        //        {
        //            if (!PDDict.ContainsKey(fc.Name))
        //                continue;

        //            var attr = ChoType.GetMemberAttribute<JsonConverterAttribute>(PIDict[fc.Name]);
        //            if (attr != null && attr.ConverterType != null)
        //            {
        //                fc.JSONPropConverters = new object[] { Activator.CreateInstance(attr.ConverterType) };
        //                fc.JSONPropConverterParams = new object[] { attr.ConverterParameters };
        //            }
        //        }
        //    }
        //}
    }

    public class ChoJSONRecordConfiguration<T> : ChoJSONRecordConfiguration
    {
        public ChoJSONRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoJSONRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoJSONRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoJSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string jsonPath = null, string fieldName = null)
        {
            base.Map(field, jsonPath, fieldName);
            return this;
        }

        public ChoJSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, Action<ChoJSONRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoJSONRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string jsonPath = null, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoJSONRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoJSONRecordFieldConfigurationMap(cf).FieldName(fieldName).JSONPath(jsonPath);

            return this;
        }

        public ChoJSONRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoJSONRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            mapper?.Invoke(new ChoJSONRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoJSONRecordConfiguration<T> Configure(Action<ChoJSONRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoJSONRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoJSONRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }
    }
}
