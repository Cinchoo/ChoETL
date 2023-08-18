using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SharpYaml;
using SharpYaml.Events;
using SharpYaml.Serialization;
using SharpYaml.Serialization.Serializers;

namespace ChoETL
{
    [DataContract]
    public class ChoYamlRecordConfiguration : ChoFileRecordConfiguration, IChoDynamicObjectRecordConfiguration, IChoJSONRecordConfiguration
    {
        private readonly object _padLock = new object();
        internal readonly Dictionary<Type, Dictionary<string, ChoYamlRecordFieldConfiguration>> YamlRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoYamlRecordFieldConfiguration>>();
        internal ChoTypeConverterFormatSpec CreateTypeConverterSpecsIfNull()
        {
            if (_typeConverterFormatSpec == null)
                _typeConverterFormatSpec = new ChoTypeConverterFormatSpec();

            return _typeConverterFormatSpec;
        }

        private StringComparer _stringComparer = StringComparer.CurrentCultureIgnoreCase;
        public StringComparer StringComparer
        {
            get { return _stringComparer == null ? StringComparer.CurrentCultureIgnoreCase : _stringComparer; }
            set { _stringComparer = value; }
        }

        //public bool AllowComplexYamlPath
        //{
        //    get;
        //    set;
        //}
        public bool FlattenNode
        {
            get;
            set;
        }
        [DataMember]
        public List<ChoYamlRecordFieldConfiguration> YamlRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public string YamlPath
        {
            get;
            set;
        }
        [DataMember]
        public bool UseYamlSerialization
        {
            get;
            set;
        }
        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }

        private Lazy<SharpYaml.Serialization.Serializer> _yamlSerializer = null;
        public SharpYaml.Serialization.Serializer YamlSerializer
        {
            get
            {
                var x = _yamlTagMapAutoRegister.Value;
                return ReuseSerializerObject ? _yamlSerializer.Value : new SharpYaml.Serialization.Serializer(YamlSerializerSettings);
            }
        }
        private Lazy<SerializerSettings> _yamlSerializerSettings = null;
        public SerializerSettings YamlSerializerSettings
        {
            get { return _yamlSerializerSettings.Value; }
        }
        public bool ReuseSerializerObject
        {
            get;
            set;
        }
        internal bool FlatToNestedObjectSupport
        {
            get;
            set;
        }
        public bool? SingleDocument
        {
            get;
            set;
        }
        public Func<string, string> YamlTagMapResolver
        {
            get;
            set;
        }
        public bool TurnOffAutoRegisterTagMap
        {
            get;
            set;
        }
        public bool UseJsonSerialization
        {
            get;
            set;
        }

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
                        _jsonSerializerSettings.ContractResolver = new ChoPropertyRenameAndIgnoreSerializerContractResolver(this);
                        _jsonSerializerSettings.Converters = new List<JsonConverter>()
                        {
                            new ExpandoObjectConverter(),
                            ChoDynamicObjectConverter.Instance,
                    };
                        //_jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    }

                    return _jsonSerializerSettings;
                }
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        private Func<IDictionary<string, object>, IDictionary<string, object>> _customNodeSelecter = null;
        public Func<IDictionary<string, object>, IDictionary<string, object>> CustomNodeSelecter
        {
            get { return _customNodeSelecter; }
            set { if (value == null) return; _customNodeSelecter = value; }
        }

        internal Dictionary<string, ChoYamlRecordFieldConfiguration> RecordFieldConfigurationsDict
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

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in YamlRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public string RootName { get; internal set; }
        public Type TargetRecordType { get; internal set; }
        public Func<Type, MemberInfo, string, bool?> IgnoreProperty { get; set; }
        public Func<Type, MemberInfo, string, string> RenameProperty { get; set; }
        public Action<Type, MemberInfo, string, JsonProperty> RemapJsonProperty { get; set; }
        public JsonLoadSettings JsonLoadSettings { get; set; }
        public ChoJObjectLoadOptions? JObjectLoadOptions { get; set; }
        public Func<JsonReader, JsonLoadSettings, JObject> CustomJObjectLoader { get; set; }
        public Func<JsonReader, JsonLoadSettings, JArray> CustomJArrayLoader { get; set; }
        public Type UnknownType { get; set; }
        public Func<JObject, object> UnknownTypeConverter { get; set; }
        public bool EnableXmlAttributePrefix { get; set; }
        public bool KeepNSPrefix { get; set; }
        public Formatting Formatting { get; set; }
        public Func<object, JToken> ObjectToJTokenConverter { get; set; }
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
        //public static new bool ColumnOrderStrict
        //{
        //    get { throw new NotSupportedException(); }
        //}
        public static new bool EscapeQuoteAndDelimiter
        {
            get { throw new NotSupportedException(); }
        }
        internal bool IsDynamicObjectInternal
        {
            get => IsDynamicObject;
            set => IsDynamicObject = value;
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
        //internal object[] GetConvertersForType(Type fieldType, object value = null)
        //{
        //    return GetConvertersForTypePrivate(fieldType, value);
        //}
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

        public ChoYamlRecordFieldConfiguration this[string name]
        {
            get
            {
                return YamlRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }
        private Lazy<bool> _yamlTagMapAutoRegister = null;

        public ChoYamlRecordConfiguration() : this(null)
        {

        }

        internal ChoYamlRecordConfiguration(Type recordType) : base(recordType)
        {
            UseJsonSerialization = false;

            _yamlSerializerSettings = new Lazy<SerializerSettings>(() =>
            {
                var yamlSettings = new SerializerSettings();
                yamlSettings.EmitTags = false;
                yamlSettings.ComparerForKeySorting = null;
                return yamlSettings;
            });

            _yamlSerializer = new Lazy<SharpYaml.Serialization.Serializer>(() =>
            {
                return new SharpYaml.Serialization.Serializer(YamlSerializerSettings);
            });

            _yamlTagMapAutoRegister = new Lazy<bool>(() =>
            {
                if (!TurnOffAutoRegisterTagMap)
                {
                    if (!RecordType.IsDynamicType())
                    {
                        RegisterYamlTagMapForType(RecordType);
                    }
                }
                return true;
            });

            YamlRecordFieldConfigurations = new List<ChoYamlRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }
        }

        private readonly HashSet<Type> _refDict = new HashSet<Type>();
        private void RegisterYamlTagMapForType(Type recordType)
        {
            if (_refDict.Contains(recordType))
                return;
            else
                _refDict.Add(recordType);

            if (recordType.IsDynamicType() || recordType.IsSpecialCollectionType())
                return;

            var tagMapAttrs = ChoTypeDescriptor.GetTypeAttributes<ChoYamlTagMapAttribute>(recordType).ToArray();
            if (tagMapAttrs.Length > 0)
            {
                foreach (var tagMapAttr in tagMapAttrs)
                {
                    if (tagMapAttr != null && !tagMapAttr.TagMap.IsNullOrWhiteSpace())
                    {
                        WithTagMapping(tagMapAttr.TagMap, recordType, tagMapAttr.Alias);
                    }
                }
            }
            else
                WithTagMapping("!", recordType, false);

            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
            {
                if (pd.PropertyType.IsSimple()) continue;

                RegisterYamlTagMapForType(pd.PropertyType);
            }
        }

        internal void AddFieldForType(Type rt, ChoYamlRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!YamlRecordFieldConfigurationsForType.ContainsKey(rt))
                YamlRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoYamlRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (YamlRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                YamlRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                YamlRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        protected virtual new bool ContainsRecordConfigForType(Type rt)
        {
            return YamlRecordFieldConfigurationsForType.ContainsKey(rt);
        }
        internal bool ContainsRecordConfigForTypeInternal(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }

        protected override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return YamlRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        protected override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return YamlRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
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

            ChoYamlRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoYamlRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
            }
            else
            {
                NullValue = String.Empty;
            }

            if (YamlRecordFieldConfigurations.Count == 0)
                MapRecordFields(); // DiscoverRecordFields(recordType, false);
        }

        internal void Reset()
        {
            IsInitialized = false;
            YamlRecordFieldConfigurations.Clear();
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

        public ChoYamlRecordConfiguration MapRecordFields<T>()
        {
            MapRecordFields(typeof(T));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                YamlRecordFieldConfigurationsForType.Remove(rt);
        }

        public void ClearRecordFieldsForType<T>()
        {
            ClearRecordFieldsForType(typeof(T));
        }

        public void MapRecordFieldsForType<T>()
        {
            MapRecordFieldsForType(typeof(T));
        }

        public ChoYamlRecordConfiguration ConfigureTypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
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

            List<ChoYamlRecordFieldConfiguration> recordFieldConfigurations = new List<ChoYamlRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            YamlRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        public ChoYamlRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault(), false);
            foreach (var rt in recordTypes.Where(rt => rt != null).Skip(1))
                DiscoverRecordFields(rt, false, YamlRecordFieldConfigurations, false);
            return this;
        }

        public void MapRecordFields()
        {
            RecordType = DiscoverRecordFields(RecordType, false, null, true);
        }

        private Type DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoYamlRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = YamlRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            return DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().Any()).Any(), recordFieldConfigurations, isTop);
        }

        private Type DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false,
            List<ChoYamlRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        bool optIn1 = ChoTypeDescriptor.GetProperties(pt).Where(pd1 => pd1.Attributes.OfType<ChoYamlRecordFieldAttribute>().Any()).Any();
                        if (optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn1, recordFieldConfigurations, false);
                        }
                        else if (pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoYamlRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                    recordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
                else
                {
                    if (isTop)
                    {
                        if (typeof(IList).IsAssignableFrom(recordType))
                        {
                            throw new ChoParserException("Record type not supported.");
                        }
                        else if (typeof(IDictionary<string, object>).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                        else if (typeof(IDictionary).IsAssignableFrom(recordType)
                            || typeof(IDictionary<string, object>).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                    }

                    if (recordType.IsSimple())
                    {
                        var obj = new ChoYamlRecordFieldConfiguration("Value", "$.Value");
                        obj.FieldType = recordType;

                        recordFieldConfigurations.Add(obj);
                        return recordType;
                    }

                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        ChoYamlIgnoreAttribute jiAttr = pd.Attributes.OfType<ChoYamlIgnoreAttribute>().FirstOrDefault();
                        if (jiAttr != null)
                            continue;

                        pt = pd.PropertyType.GetUnderlyingType();
                        //if ((pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        //    || (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo())))
                        if (false)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, recordFieldConfigurations, false);
                        }
                        else
                        {
                            var obj = new ChoYamlRecordFieldConfiguration(pd.Name, ChoTypeDescriptor.GetPropetyAttribute<ChoYamlRecordFieldAttribute>(pd),
                                pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;
                            //ChoUseYamlSerializationAttribute sAttr = pd.Attributes.OfType<ChoUseYamlSerializationAttribute>().FirstOrDefault();
                            //if (sAttr != null)
                            //    obj.UseYamlSerialization = sAttr.Flag;
                            ChoYamlPathAttribute jpAttr = pd.Attributes.OfType<ChoYamlPathAttribute>().FirstOrDefault();
                            if (jpAttr != null)
                                obj.YamlPath = jpAttr.YamlPath;

                            ChoYamPropertyAttribute jAttr = pd.Attributes.OfType<ChoYamPropertyAttribute>().FirstOrDefault();
                            if (jAttr != null && !jAttr.PropertyName.IsNullOrWhiteSpace())
                            {
                                obj.FieldName = jAttr.PropertyName;
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
                                    recordFieldConfigurations.Add(obj);
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
            if (RecordType != null)
            {
                Init(RecordType);
            }

            base.Validate(state);

            string[] fieldNames = null;
            IDictionary<string, object> yamlNode = null;
            if (state is Tuple<long, IDictionary<string, object>>)
                yamlNode = ((Tuple<long, IDictionary<string, object>>)state).Item2;
            else
                fieldNames = state as string[];

            if (fieldNames != null && YamlRecordFieldConfigurations.Count > 0 && FlattenNode)
                YamlRecordFieldConfigurations.Clear();

            if (AutoDiscoverColumns
                && YamlRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    /*&& ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().Any()).Any()*/)
                {
                    MapRecordFields(RecordType);
                }
                else if (yamlNode != null)
                {
                    Dictionary<string, ChoYamlRecordFieldConfiguration> dict = new Dictionary<string, ChoYamlRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    foreach (var entry in yamlNode)
                    {
                        if (!dict.ContainsKey(entry.Key))
                            dict.Add(entry.Key, new ChoYamlRecordFieldConfiguration(entry.Key, (string)null));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(entry.Key));
                        }
                    }

                    foreach (ChoYamlRecordFieldConfiguration obj in dict.Values)
                        YamlRecordFieldConfigurations.Add(obj);
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoYamlRecordFieldConfiguration(fn, (string)null);
                        YamlRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            else
            {
                foreach (var fc in YamlRecordFieldConfigurations)
                {
                    fc.ComplexYamlPathUsed = !(fc.YamlPath.IsNullOrWhiteSpace() || String.Compare(fc.FieldName, fc.YamlPath, true) == 0);
                }
            }

            //if (YamlRecordFieldConfigurations.Count <= 0)
            //    throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            foreach (var fieldConfig in YamlRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = YamlRecordFieldConfigurations.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(String.Join(",", dupFields)));

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in YamlRecordFieldConfigurations)
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

            RecordFieldConfigurationsDict = YamlRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            LoadNCacheMembers(YamlRecordFieldConfigurations);
        }

        #region Fluent API

        public ChoYamlRecordConfiguration WithTagMapping(string tagName, Type tagType, bool isAlias = false)
        {
            if (tagName.IsNullOrWhiteSpace() || tagType == null)
                return this;

            var yamlTagMapResolver = YamlTagMapResolver;
            tagName = tagName.NTrim();
            string tagMapOut = tagName;
            if (yamlTagMapResolver != null)
            {
                tagMapOut = yamlTagMapResolver(tagName);
            }

            if (tagName == tagMapOut)
            {
                if (tagName == "!")
                {
                    tagName = $"!{tagType.Name}";
                }
                else if (tagName == "!!")
                {
                    tagName = $"tag:yaml.org,2002:{tagType.Name}";
                }
                else if (tagName.StartsWith("!!"))
                {
                    tagName = $"tag:yaml.org,2002:{tagName.Substring(2)}";
                }
            }

            YamlSerializerSettings.RegisterTagMapping(tagName, tagType, isAlias);
            return this;
        }

        public ChoYamlRecordConfiguration ConfigureYamlSerializerSettings(Action<SerializerSettings> settings)
        {
            settings?.Invoke(YamlSerializerSettings);
            return this;
        }

        public ChoYamlRecordConfiguration Configure(Action<ChoYamlRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoYamlRecordConfiguration ClearFields()
        {
            YamlRecordFieldConfigurationsForType.Clear();
            YamlRecordFieldConfigurations.Clear();
            base.ClearFields();
            return this;
        }

        public ChoYamlRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (field != null)
            {
                if (YamlRecordFieldConfigurations.Count == 0)
                    MapRecordFields<T>();

                var fc = YamlRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == field.GetFullyQualifiedMemberName()).FirstOrDefault();
                if (fc != null)
                    YamlRecordFieldConfigurations.Remove(fc);
            }

            return this;
        }

        public ChoYamlRecordConfiguration IgnoreField(string fieldName)
        {
            if (fieldName != null)
            {
                var fc = YamlRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == fieldName || f.FieldName == fieldName).FirstOrDefault();
                if (fc != null)
                    YamlRecordFieldConfigurations.Remove(fc);
                else
                    IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoYamlRecordConfiguration Map(string propertyName, string yamlPath = null, string fieldName = null, Type fieldType = null)
        {
            Map(propertyName, m => m.YamlPath(yamlPath).FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoYamlRecordConfiguration Map(string propertyName, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoYamlRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoYamlRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string yamlPath = null, string fieldName = null)
        {
            Map(field, m => m.YamlPath(yamlPath).FieldName(fieldName));
            return this;
        }

        public ChoYamlRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoYamlRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType == typeof(T) ? null : subType);
            mapper?.Invoke(new ChoYamlRecordFieldConfigurationMap(cf));
            return this;
        }

        #endregion Fluent API

        internal void WithField(string name, string yamlPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null, Type recordType = null,
            Type subRecordType = null, Func<IDictionary<string, object>, Type> fieldTypeSelector = null,
            Func<object, Type> itemRecordTypeSelector = null
            )
        {
            ChoGuard.ArgumentNotNull(recordType, nameof(recordType));

            if (!name.IsNullOrEmpty())
            {
                if (subRecordType != null)
                    MapRecordFieldsForType(subRecordType);

                string fnTrim = name.NTrim();
                ChoYamlRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (YamlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = YamlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    YamlRecordFieldConfigurations.Remove(fc);
                }
                else if (subRecordType != null)
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoYamlRecordFieldConfiguration(fnTrim, yamlPath)
                {
                    FieldType = fieldType,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldName = fieldName.IsNullOrWhiteSpace() ? name : fieldName,
                    ValueConverter = valueConverter,
                    CustomSerializer = customSerializer,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    ItemConverter = itemConverter,
                    IsArray = isArray,
                    NullValue = nullValue,
                    FieldTypeSelector = fieldTypeSelector,
                    ItemRecordTypeSelector = itemRecordTypeSelector,
                };
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
                    YamlRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoYamlRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoYamlRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoYamlRecordFieldConfiguration(propertyName, attr, otherAttrs);
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                if (!YamlRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                    YamlRecordFieldConfigurations.Add(new ChoYamlRecordFieldConfiguration(propertyName, attr, otherAttrs));

                return YamlRecordFieldConfigurations.First(fc => fc.Name == propertyName);
            }
        }

        internal ChoYamlRecordFieldConfiguration GetFieldConfiguration(string fn)
        {
            fn = fn.NTrim();
            if (!YamlRecordFieldConfigurations.Any(fc => fc.Name == fn))
                YamlRecordFieldConfigurations.Add(new ChoYamlRecordFieldConfiguration(fn, (string)null));

            return YamlRecordFieldConfigurations.First(fc => fc.Name == fn);
        }

        internal ChoYamlRecordFieldConfiguration GetFieldConfigurationForType(Type type, string fn)
        {
            fn = fn.NTrim();
            if (ContainsRecordConfigForTypeInternal(type) && GetRecordConfigForType(type).Any(fc => fc.Name == fn))
                return GetRecordConfigForType(type).OfType<ChoYamlRecordFieldConfiguration>().FirstOrDefault(fc => fc.Name == fn);

            return null;
        }
        internal void ResetStatesInternal()
        {
            ResetStates();
        }
        internal Encoding GetEncodingInternal(Stream inStream)
        {
            return GetEncoding(inStream);
        }
        internal Encoding GetEncodingInternal(string fileName)
        {
            return GetEncoding(fileName);
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
        //        foreach (var fc in fcs.OfType<ChoYamlRecordFieldConfiguration>())
        //        {
        //            if (!PDDict.ContainsKey(fc.Name))
        //                continue;

        //            var attr = ChoType.GetMemberAttribute<YamlConverterAttribute>(PIDict[fc.Name]);
        //            if (attr != null && attr.ConverterType != null)
        //            {
        //                fc.YamlPropConverters = new object[] { Activator.CreateInstance(attr.ConverterType) };
        //                fc.YamlPropConverterParams = new object[] { attr.ConverterParameters };
        //            }
        //        }
        //    }
        //}
    }

    public class ChoYamlRecordConfiguration<T> : ChoYamlRecordConfiguration
    {
        public ChoYamlRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoYamlRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoYamlRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoYamlRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string yamlPath = null, string fieldName = null)
        {
            base.Map(field, yamlPath, fieldName);
            return this;
        }

        public ChoYamlRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, Action<ChoYamlRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoYamlRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string yamlPath = null, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoYamlRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoYamlRecordFieldConfigurationMap(cf).FieldName(fieldName).YamlPath(yamlPath);

            return this;
        }

        public ChoYamlRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoYamlRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoYamlRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            mapper?.Invoke(new ChoYamlRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoYamlRecordConfiguration<T> Configure(Action<ChoYamlRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoYamlRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoYamlRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }
    }
}
