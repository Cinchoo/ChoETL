using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
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

namespace ChoETL
{
    [DataContract]
    public class ChoAvroRecordConfiguration : ChoFileRecordConfiguration
    {
        private readonly Dictionary<string, dynamic> _indexMapDict = new Dictionary<string, dynamic>();
        internal readonly Dictionary<Type, Dictionary<string, ChoAvroRecordFieldConfiguration>> AvroRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoAvroRecordFieldConfiguration>>();

        private readonly Lazy<bool> _initializer = null;

        private AvroSerializerSettings _defaultAvroSerializerSettings = new AvroSerializerSettings();
        private AvroSerializerSettings _avroSerializerSettings = null;
        public AvroSerializerSettings AvroSerializerSettings
        {
            get { return _avroSerializerSettings == null ? _defaultAvroSerializerSettings : _avroSerializerSettings; }
            set { _avroSerializerSettings = value; }
        }
        internal object AvroSerializer
        {
            get;
            set;
        }
        public ICollection<Type> KnownTypes { get; set; } = new HashSet<Type>();

        public int SyncNumberOfObjects { get; set; }

        private bool _useAvroSerializer = false;
        public bool UseAvroSerializer
        {
            get { return _useAvroSerializer; }
            set { }
        }

        public string RecordSchema
        {
            get;
            set;
        }

        public bool LeaveOpen
        {
            get;
            set;
        }

        public CodecFactory CodecFactory
        {
            get;
            set;
        }

        public Codec Codec
        {
            get;
            set;
        }

        public bool AllowNullable
        {
            get;
            set;
        }

        [DataMember]
        public List<ChoAvroRecordFieldConfiguration> AvroRecordFieldConfigurations
        {
            get;
            private set;
        }

        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }
        internal Dictionary<string, string> AlternativeKeys
        {
            get;
            set;
        }
        internal Dictionary<string, ChoAvroRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoAvroRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }
        internal int MaxFieldPosition
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in AvroRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public bool IgnoreHeader { get; internal set; }
        internal bool IsDynamicObjectInternal
        {
            get => IsDynamicObject;
            set => IsDynamicObject = value;
        }
        public static new int MaxLineSize
        {
            get { throw new NotSupportedException(); }
        }
        public static new string EOLDelimiter
        {
            get { throw new NotSupportedException(); }
        }
        public static new string MayContainEOLInData
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool IgnoreEmptyLine
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool ColumnCountStrict
        {
            get { throw new NotSupportedException(); }
        }
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

        public ChoAvroRecordFieldConfiguration this[string name]
        {
            get
            {
                return AvroRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoAvroRecordConfiguration() : this(null)
        {
        }

        internal ChoAvroRecordConfiguration(Type recordType) : base(recordType)
        {
            AvroRecordFieldConfigurations = new List<ChoAvroRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }
            NestedKeySeparator = ChoETLSettings.NestedKeySeparator;
            UseAvroSerializer = true;
            SyncNumberOfObjects = 24;


            _initializer = new Lazy<bool>(() =>
            {
                AvroSerializerSettings.Resolver = new ChoAvroPublicMemberContractResolver(this.AllowNullable) { Configuration = this };
                if (KnownTypes != null && KnownTypes.Count > 0)
                    AvroSerializerSettings.KnownTypes = KnownTypes;
                return true;
            });
        }
        internal ChoTypeConverterFormatSpec CreateTypeConverterSpecsIfNull()
        {
            if (_typeConverterFormatSpec == null)
                _typeConverterFormatSpec = new ChoTypeConverterFormatSpec();

            return _typeConverterFormatSpec;
        }

        internal void Init()
        {
            var result = _initializer.Value;
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoAvroRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoAvroRecordObjectAttribute>(recordType);
            if (IgnoreFieldValueMode == null)
                IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Empty;

            if (AvroRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType);
        }

        public ChoAvroRecordConfiguration ConfigureTypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            CreateTypeConverterSpecsIfNull();
            spec?.Invoke(TypeConverterFormatSpec);
            return this;
        }

        public ChoAvroRecordConfiguration MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
            return this;
        }

        public ChoAvroRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            int pos = 0;
            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault(), ref pos, true);
            foreach (var rt in recordTypes.Skip(1).Where(rt => rt != null))
                DiscoverRecordFields(rt, ref pos, false);

            return this;
        }

        private void DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoAvroRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = AvroRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            int position = 0;
            DiscoverRecordFields(recordType, ref position, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().Any()).Any(),
                null, recordFieldConfigurations);
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

        private void DiscoverRecordFields(Type recordType)
        {
            int pos = 0;
            DiscoverRecordFields(recordType, ref pos, false);
        }

        private void DiscoverRecordFields(Type recordType, ref int pos, bool clear,
            List<ChoAvroRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = AvroRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                ClearFields();

            DiscoverRecordFields(recordType, ref pos, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().Any()).Any(),
                null, recordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null,
            bool optIn = false, PropertyDescriptor propDesc = null, List<ChoAvroRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn)
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt)
                            && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, null, recordFieldConfigurations);
                        else if (pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoAvroRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                recordFieldConfigurations.Add(obj);
                        }
                    }
                }
                else
                {
                    if ((recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(recordType))
                        && !typeof(ArrayList).IsAssignableFrom(recordType)
                        /*&& !recordType.IsInterface*/)
                    {
                        if (propDesc != null)
                        {
                            RangeAttribute dnAttr = propDesc.Attributes.OfType<RangeAttribute>().FirstOrDefault();

                            if (dnAttr == null)
                            {
                                ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                recordFieldConfigurations.Add(obj);
                            }
                            else if (dnAttr != null && dnAttr.Minimum.CastTo<int>() >= 0 && dnAttr.Maximum.CastTo<int>() >= 0
                                && dnAttr.Minimum.CastTo<int>() <= dnAttr.Maximum.CastTo<int>())
                            {
                                recordType = recordType.GetItemType().GetUnderlyingType();

                                if (recordType.IsSimple())
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range < dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, range);
                                        recordFieldConfigurations.Add(obj);
                                    }
                                }
                                else
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range < dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                                        {
                                            pt = pd.PropertyType.GetUnderlyingType();
                                            if (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()) /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                            {
                                                //DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd);
                                            }
                                            else
                                            {
                                                ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, range, propDesc.GetDisplayName());

                                                recordFieldConfigurations.Add(obj);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (recordType.IsGenericType && (recordType.GetGenericTypeDefinition() == typeof(Dictionary<,>) || recordType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        /*&& typeof(string) == recordType.GetGenericArguments()[0]*/)
                    {
                        if (propDesc != null)
                        {
                            ChoDictionaryKeyAttribute[] dnAttrs = propDesc.Attributes.OfType<ChoDictionaryKeyAttribute>().ToArray();
                            if (dnAttrs.IsNullOrEmpty())
                            {
                                ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                recordFieldConfigurations.Add(obj);
                            }
                            else
                            {
                                var keys = (from a in dnAttrs
                                            where a != null && !a.Keys.IsNullOrWhiteSpace()
                                            select a.Keys.SplitNTrim()).SelectMany(a => a).ToArray();

                                foreach (var key in keys)
                                {
                                    if (!key.IsNullOrWhiteSpace())
                                    {
                                        ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);

                                        recordFieldConfigurations.Add(obj);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (recordType == typeof(object)
                            //|| typeof(IEnumerable).IsAssignableFrom(recordType)
                            //|| typeof(ICollection).IsAssignableFrom(recordType)
                            )
                        {

                        }
                        else if (recordType.IsSimple())
                        {
                            ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, propDesc);
                            if (!recordFieldConfigurations.Any(c => c.Name == propDesc.Name))
                                recordFieldConfigurations.Add(obj);
                        }
                        else
                        {
                            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                            {
                                pt = pd.PropertyType.GetUnderlyingType();

                                if (pt == typeof(object)
                                    || typeof(ArrayList).IsAssignableFrom(pt)
                                    || typeof(Hashtable).IsAssignableFrom(pt)
                                    )
                                {
                                    continue;
                                }

                                if (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()) /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                {
                                    if (declaringMember == pd.Name)
                                    {

                                    }
                                    else
                                    {
                                        if (propDesc == null)
                                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                        else
                                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                    }
                                }
                                else
                                {
                                    ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                    if (!recordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal ChoAvroRecordFieldConfiguration NewFieldConfiguration(ref int position, string declaringMember, PropertyDescriptor pd,
            int? arrayIndex = null, string displayName = null, string dictKey = null, bool ignoreAttrs = false, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            ChoAvroRecordFieldConfiguration obj = null;

            if (displayName.IsNullOrEmpty())
            {
                if (pd != null)
                    obj = new ChoAvroRecordFieldConfiguration(declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name));
                else
                    obj = new ChoAvroRecordFieldConfiguration("Value");
            }
            else if (pd != null)
            {
                if (displayName.IsNullOrWhiteSpace())
                    obj = new ChoAvroRecordFieldConfiguration("{0}".FormatString(pd.Name));
                else
                    obj = new ChoAvroRecordFieldConfiguration("{0}.{1}".FormatString(displayName, pd.Name));
            }
            else
                obj = new ChoAvroRecordFieldConfiguration(displayName);

            //obj.FieldName = pd != null ? pd.Name : displayName;

            mapper?.Invoke(new ChoAvroRecordFieldConfigurationMap(obj));

            obj.DictKey = dictKey;
            obj.ArrayIndex = arrayIndex;
            obj.FieldType = pd != null ? pd.PropertyType : null; // pt;
            obj.PropertyDescriptorInternal = pd;
            if (pd != null)
                obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
            else
                obj.DeclaringMemberInternal = displayName;

            if (arrayIndex == null && pd != null)
            {
                if (!ignoreAttrs)
                {
                    ChoFieldPositionAttribute fpAttr = pd.Attributes.OfType<ChoFieldPositionAttribute>().FirstOrDefault();
                }
            }
            else
            {

            }

            if (!ignoreAttrs && pd != null)
            {
                StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                if (slAttr != null && slAttr.MaximumLength > 0)
                    obj.Size = slAttr.MaximumLength;
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
                            obj.FieldName = dpAttr.ShortName.Trim();
                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                            obj.FieldName = dpAttr.Name.Trim();

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
                DisplayFormatAttribute dfAttr = pd.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
                if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                {
                    obj.FormatText = dfAttr.DataFormatString;
                }
                if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                {
                    obj.NullValue = dfAttr.NullDisplayText;
                }
            }

            if (pd != null && pd.ComponentType != null)
            {
                if (ContainsRecordConfigForTypeInternal(pd.ComponentType))
                {
                    var st = GetRecordConfigForType(pd.ComponentType).OfType<ChoAvroRecordFieldConfiguration>();
                    if (st != null && st.Any(fc => fc.Name == pd.Name))
                    {
                        var f = st.FirstOrDefault(fc => fc.Name == pd.Name);
                        if (f != null)
                        {
                            obj.FieldName = f.FieldName;
                        }
                    }
                }
            }

            if (arrayIndex != null)
            {
                var arrayIndexSeparator = GetArrayIndexSeparatorInternal(); // ArrayIndexSeparator == null ? ChoETLSettings.ArrayIndexSeparator : ArrayIndexSeparator.Value;

                if (_recObject.Value is IChoArrayItemFieldNameOverrideable)
                {
                    obj.Name = obj.FieldName = ((IChoArrayItemFieldNameOverrideable)_recObject.Value).GetFieldName(displayName.IsNullOrWhiteSpace() ? declaringMember : displayName, obj.FieldName, arrayIndexSeparator, arrayIndex.Value);
                }
                else
                {
                    obj.Name = obj.FieldName = obj.FieldName + arrayIndexSeparator + arrayIndex.Value;
                }
            }
            else if (!dictKey.IsNullOrWhiteSpace())
            {
                obj.FieldName = dictKey;
            }

            return obj;
        }
        internal string GetArrayIndexSeparatorInternal()
        {
            return GetArrayIndexSeparator();
        }
        internal char GetArrayIndexSeparatorCharInternal()
        {
            return GetArrayIndexSeparatorChar();
        }

        protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        {
            if (!IsDynamicObject)
            {
                string name = null;
                object defaultValue = null;
                object fallbackValue = null;
                foreach (var fc in fcs.OfType<ChoAvroRecordFieldConfiguration>())
                {
                    name = fc.Name;

                    if (!PDDict.ContainsKey(name))
                    {
                        if (!PDDict.ContainsKey(fc.FieldName))
                            continue;

                        name = fc.FieldName;
                    }

                    fc.PDInternal = PDDict[name];
                    fc.PIInternal = PIDict[name];

                    //Load default value
                    defaultValue = ChoType.GetRawDefaultValue(PDDict[name]);
                    if (defaultValue != null)
                    {
                        fc.DefaultValue = defaultValue;
                        fc.IsDefaultValueSpecified = true;
                    }
                    //Load fallback value
                    fallbackValue = ChoType.GetRawFallbackValue(PDDict[name]);
                    if (fallbackValue != null)
                    {
                        fc.FallbackValue = fallbackValue;
                        fc.IsFallbackValueSpecified = true;
                    }

                    //Load Converters
                    fc.PropConvertersInternal = ChoTypeDescriptor.GetTypeConverters(fc.PIInternal);
                    fc.PropConverterParamsInternal = ChoTypeDescriptor.GetTypeConverterParams(fc.PIInternal);

                }
            }
            base.LoadNCacheMembers(fcs);
        }
        internal void ValidateInternal(object state)
        {
            Validate(state);
        }

        protected override void Validate(object state)
        {
            if (UseAvroSerializer)
                ChoETLLog.Info("Using Avro Serializer...");
            else
                ChoETLLog.Info("Using Avro Sequencial Writer...");

            base.Validate(state);

            string[] fieldNames = null;
            IDictionary<string, object> jObject = null;
            if (state is Tuple<long, IDictionary<string, object>>)
                jObject = ((Tuple<long, IDictionary<string, object>>)state).Item2;
            else
                fieldNames = state as string[];

            if (AutoDiscoverColumns
                && AvroRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    /*&& ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().Any()).Any()*/)
                {
                    MapRecordFields(RecordType);
                }
                else if (jObject != null)
                {
                    Dictionary<string, ChoAvroRecordFieldConfiguration> dict = new Dictionary<string, ChoAvroRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    int index = 0;
                    foreach (var kvp in jObject)
                    {
                        name = kvp.Key;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoAvroRecordFieldConfiguration(name));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    foreach (ChoAvroRecordFieldConfiguration obj in dict.Values)
                        AvroRecordFieldConfigurations.Add(obj);
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    int index = 0;

                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoAvroRecordFieldConfiguration(fn);
                        AvroRecordFieldConfigurations.Add(obj);
                    }
                }

                //if (headers != null && IsDynamicObject)
                //{
                //    int index = 0;
                //    ParquetRecordFieldConfigurations = (from header in headers
                //                                    where !IgnoredFields.Contains(header)
                //                                    select new ChoParquetRecordFieldConfiguration(header, ++index)
                //                                    ).ToList();
                //}
                //else
                //{
                //    MapRecordFields(RecordType);
                //}
            }
            else
            {
            }

            //Index map initialization
            foreach (var value in _indexMapDict.Values)
            {
                BuildIndexMap(value.fieldName, value.fieldType, value.minumum, value.maximum,
                    value.fieldName, value.displayName,
                    value.mapper);
            }

            //Validate each record field
            foreach (var fieldConfig in AvroRecordFieldConfigurations)
                fieldConfig.Validate(this);

            if (false)
            {
            }
            else
            {
                //Check if any field has empty names 
                if (AvroRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = AvroRecordFieldConfigurations.GroupBy(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in AvroRecordFieldConfigurations)
            {
                if (fc.PropertyDescriptorInternal == null && !IsDynamicObject)
                {
                    var pd = ChoTypeDescriptor.GetProperty(RecordType, fc.Name);
                    if (pd == null)
                        pd = ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMemberInternal);

                    if (pd != null)
                    {
                        fc.PropertyDescriptorInternal = pd;
                        if (fc.FieldType == null)
                            fc.FieldType = pd.PropertyType.GetUnderlyingType();
                    }
                }

                var pd1 = fc.DeclaringMemberInternal.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
                    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMemberInternal);
                if (pd1 != null)
                    fc.PropertyDescriptorInternal = pd1;

                if (fc.PropertyDescriptorInternal == null)
                    fc.PropertyDescriptorInternal = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptorInternal == null)
                    continue;

                PIDict.Add(fc.FieldName, fc.PropertyDescriptorInternal.ComponentType.GetProperty(fc.PropertyDescriptorInternal.Name));
                PDDict.Add(fc.FieldName, fc.PropertyDescriptorInternal);
            }

            RecordFieldConfigurationsDict = AvroRecordFieldConfigurations.Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);
            //RecordFieldConfigurationsDictGroup = RecordFieldConfigurationsDict.GroupBy(kvp => kvp.Key.Contains(".") ? kvp.Key.SplitNTrim(".").First() : kvp.Key).ToDictionary(i => i.Key, i => i.ToArray());
            RecordFieldConfigurationsDict2 = AvroRecordFieldConfigurations.Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);

            try
            {
                if (IsDynamicObject)
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp =>
                    {
                        if (kvp.Key == kvp.Value.Name)
                            return kvp.Value.Name.ToValidVariableName();
                        else
                            return kvp.Value.Name;
                    }, kvp => kvp.Key/*, FileHeaderConfiguration.StringComparer*/);
                else
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name/*, FileHeaderConfiguration.StringComparer*/);
            }
            catch { }

            //FCArray = RecordFieldConfigurationsDict.ToArray();

            LoadNCacheMembers(AvroRecordFieldConfigurations);
        }

        private void ValidateChar(char src, string name)
        {
            if (src == ChoCharEx.NUL)
                throw new ChoRecordConfigurationException("Invalid 'NUL' {0} specified.".FormatString(name));
            if (EOLDelimiter.Contains(src))
                throw new ChoRecordConfigurationException("{2} [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(src, EOLDelimiter, name));
            if ((from comm in Comments
                 where comm.Contains(src.ToString())
                 select comm).Any())
                throw new ChoRecordConfigurationException("One of the Comments contains {0}. Not allowed.".FormatString(name));
        }

        public new ChoAvroRecordConfiguration ClearFields()
        {
            _indexMapDict.Clear();
            AvroRecordFieldConfigurationsForType.Clear();
            AvroRecordFieldConfigurations.Clear();
            base.ClearFields();
            return this;
        }

        public ChoAvroRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (AvroRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = AvroRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                AvroRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoAvroRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = AvroRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                AvroRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoAvroRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            Map(field, m => m.FieldName(fieldName));
            return this;
        }

        public ChoAvroRecordConfiguration Map(string propertyName, string fieldName = null, Type fieldType = null)
        {
            Map(propertyName, m => m.FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoAvroRecordConfiguration Map(string propertyName, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoAvroRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoAvroRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm/*, subType == typeof(T) ? null : subType*/);
            mapper?.Invoke(new ChoAvroRecordFieldConfigurationMap(cf));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                AvroRecordFieldConfigurationsForType.Remove(rt);
        }

        public void ClearRecordFieldsForType<T>()
        {
            ClearRecordFieldsForType(typeof(T));
        }

        public void MapRecordFieldsForType<T>()
        {
            MapRecordFieldsForType(typeof(T));
        }

        public void MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                return;

            List<ChoAvroRecordFieldConfiguration> recordFieldConfigurations = new List<ChoAvroRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            AvroRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        internal void AddFieldForType(Type rt, ChoAvroRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!AvroRecordFieldConfigurationsForType.ContainsKey(rt))
                AvroRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoAvroRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (AvroRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                AvroRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                AvroRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }
        internal void ResetStatesInternal()
        {
            ResetStates();
        }

        protected virtual new bool ContainsRecordConfigForType(Type rt)
        {
            return AvroRecordFieldConfigurationsForType.ContainsKey(rt);
        }
        internal bool ContainsRecordConfigForTypeInternal(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }

        protected override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return AvroRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        protected override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return AvroRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }
        internal Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForTypeInternal(Type rt)
        {
            return GetRecordConfigDictionaryForType(rt);
        }
        internal Encoding GetEncodingInternal(Stream inStream)
        {
            return GetEncoding(inStream);
        }
        internal Encoding GetEncodingInternal(string fileName)
        {
            return GetEncoding(fileName);
        }

        public ChoAvroRecordConfiguration MapForType<T, TField>(Expression<Func<T, TField>> field,
            string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoAvroRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            var cf1 = new ChoAvroRecordFieldConfigurationMap(cf).FieldName(fieldName);

            return this;
        }

        internal void WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption? fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Type recordType = null, Type subRecordType = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            IChoValueConverter propertyConverter = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (subRecordType == recordType)
                    subRecordType = null;

                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;
                if (subRecordType != null)
                    MapRecordFieldsForType(subRecordType);

                string fnTrim = name.NTrim();
                ChoAvroRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (AvroRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    AvroRecordFieldConfigurations.Remove(fc);
                }
                else if (subRecordType != null)
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }

                var nfc = new ChoAvroRecordFieldConfiguration(fnTrim)
                {
                    FieldType = fieldType,
                    //QuoteField = quoteField,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldValueJustification = fieldValueJustification,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    ValueSelector = valueSelector,
                    HeaderSelector = headerSelector,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    NullValue = nullValue,
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
                if (propertyConverter != null)
                    nfc.AddConverter(propertyConverter);

                if (subRecordType == null)
                    AvroRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoAvroRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoAvroRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoAvroRecordFieldConfiguration(propertyName, attr, otherAttrs);
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                //if (!ParquetRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                //    ParquetRecordFieldConfigurations.Add(new ChoParquetRecordFieldConfiguration(propertyName, attr, otherAttrs));

                //return ParquetRecordFieldConfigurations.First(fc => fc.Name == propertyName);
                if (fqm == null)
                    fqm = propertyName;

                propertyName = propertyName.SplitNTrim(".").LastOrDefault();
                if (!AvroRecordFieldConfigurations.Any(fc => fc.DeclaringMemberInternal == fqm && fc.ArrayIndex == null))
                {
                    int fieldPosition = 0;

                    var c = new ChoAvroRecordFieldConfiguration(propertyName, attr, otherAttrs);
                    if (pd != null)
                    {
                        c.PropertyDescriptorInternal = pd;
                        c.FieldType = pd.PropertyType.GetUnderlyingType();
                    }

                    c.DeclaringMemberInternal = fqm;

                    AvroRecordFieldConfigurations.Add(c);
                }

                return AvroRecordFieldConfigurations.First(fc => fc.DeclaringMemberInternal == fqm && fc.ArrayIndex == null);
            }
        }

        public ChoAvroRecordConfiguration IndexMap(string fieldName, Type fieldType, int minumum, int maximum, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            IndexMapInternal(fieldName, fieldType, minumum, maximum, fieldName, fieldName, mapper);
            return this;
        }

        public ChoAvroRecordConfiguration IndexMap<T, TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            var dn = field.GetPropertyDescriptor().GetDisplayName();

            if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(fieldType))
                && !typeof(ArrayList).IsAssignableFrom(fieldType)
                && minumum >= 0 /*&& maximum >= 0 && minumum <= maximum*/)
            {
                IndexMapInternal(fqn, fieldType, minumum, maximum,
                    field.GetFullyQualifiedMemberName(), field.GetPropertyDescriptor().GetDisplayName(), mapper);
            }
            return this;
        }

        internal void IndexMapInternal(string fieldName, Type fieldType, int minumum, int maximum,
            string fullyQualifiedMemberName = null, string displayName = null,
            Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            if (_indexMapDict.ContainsKey(fieldName))
                _indexMapDict.Remove(fieldName);
            _indexMapDict.AddOrUpdate(fieldName, new
            {
                fieldType,
                minumum,
                maximum,
                fieldName,
                displayName,
                mapper
            });
        }

        internal void BuildIndexMap(string fieldName, Type fieldType, int minumum, int maximum,
            string fullyQualifiedMemberName = null, string displayName = null,
            Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            if (fullyQualifiedMemberName == null)
                fullyQualifiedMemberName = fieldName;

            fieldName = fieldName.SplitNTrim(".").LastOrDefault();

            if (fieldType == null)
                return;

            if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                fullyQualifiedMemberName = fieldName;
            //if (displayName.IsNullOrWhiteSpace())
            //    displayName = fieldName;

            Type recordType = fieldType;
            var fqn = fieldName;

            if ((recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(recordType))
                && !typeof(ArrayList).IsAssignableFrom(recordType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum
                /*&& !fieldType.IsInterface*/)
            {
                var itemType = recordType.GetItemType().GetUnderlyingType();
                if (itemType.IsSimple())
                {
                    var fcs1 = AvroRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fullyQualifiedMemberName).ToArray();
                    foreach (var fc in fcs1)
                    {
                        displayName = fcs1.First().FieldName;
                        AvroRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index < maximum; index++)
                    {
                        var nfc = new ChoAvroRecordFieldConfiguration(fieldName) { ArrayIndex = index };
                        mapper?.Invoke(new ChoAvroRecordFieldConfigurationMap(nfc));

                        if (displayName != null)
                            nfc.FieldName = displayName;

                        string lFieldName = null;
                        //if (ArrayIndexSeparator == null)
                        //    lFieldName = nfc.FieldName + "_" + index;
                        //else
                            lFieldName = nfc.FieldName + GetArrayIndexSeparatorInternal() + index;

                        nfc.DeclaringMemberInternal = nfc.Name;
                        nfc.Name = lFieldName;
                        nfc.FieldName = lFieldName;
                        nfc.ArrayIndex = index;

                        nfc.FieldType = recordType;
                        AvroRecordFieldConfigurations.Add(nfc);
                    }
                }
                else
                {
                    int priority = 0;

                    //Remove collection config member
                    var fcs1 = AvroRecordFieldConfigurations.Where(o => o.Name == fqn).ToArray();
                    foreach (var fc in fcs1)
                    {
                        AvroRecordFieldConfigurations.Remove(fc);
                    }

                    //Remove any unused config
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                    {
                        var fcs = AvroRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                        && o.ArrayIndex != null && (o.ArrayIndex < minumum || o.ArrayIndex > maximum)).ToArray();

                        foreach (var fc in fcs)
                            AvroRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                        {
                            var fc = AvroRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                            && o.ArrayIndex != null && o.ArrayIndex == index).FirstOrDefault();

                            if (fc != null) continue;

                            Type pt = pd.PropertyType.GetUnderlyingType();
                            if (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()))
                            {
                            }
                            else
                            {
                                int fieldPosition = 0;
                                ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, fullyQualifiedMemberName, pd, index, displayName, ignoreAttrs: false,
                                    mapper: mapper);

                                //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                AvroRecordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        public ChoAvroRecordConfiguration DictionaryMap(string fieldName, Type fieldType,
            string[] keys, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            DictionaryMapInternal(fieldName, fieldType, fieldName, keys, null, mapper);
            return this;
        }

        public ChoAvroRecordConfiguration DictionaryMap<T, TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            PropertyDescriptor pd = field.GetPropertyDescriptor();

            DictionaryMapInternal(pd.Name, fieldType, fqn, keys, pd, mapper);
            return this;
        }

        internal ChoAvroRecordConfiguration DictionaryMapInternal(string fieldName, Type fieldType, string fqn,
            string[] keys, PropertyDescriptor pd = null, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            List<ChoAvroRecordFieldConfiguration> fcsList = new List<ChoAvroRecordFieldConfiguration>();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                && typeof(string) == fieldType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
                //Remove collection config member
                var fcs1 = AvroRecordFieldConfigurations.Where(o => o.Name == fqn).ToArray();
                foreach (var fc in fcs1)
                    AvroRecordFieldConfigurations.Remove(fc);

                //Remove any unused config
                var fcs = AvroRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fieldName
                && !o.DictKey.IsNullOrWhiteSpace() && !keys.Contains(o.DictKey)).ToArray();

                foreach (var fc in fcs)
                    AvroRecordFieldConfigurations.Remove(fc);

                foreach (var key in keys)
                {
                    if (!key.IsNullOrWhiteSpace())
                    {
                        var fc = AvroRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fieldName
                            && !o.DictKey.IsNullOrWhiteSpace() && key == o.DictKey).FirstOrDefault();

                        if (fc != null) continue;

                        //ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);
                        int fieldPosition = 0;
                        ChoAvroRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, null, pd, displayName: fieldName, dictKey: key, mapper: mapper);

                        //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                        //ParquetRecordFieldConfigurations.Add(obj);
                        AvroRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            return this;
        }

        #region Fluent API

        public ChoAvroRecordConfiguration Configure(Action<ChoAvroRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion 
    }

    public class ChoAvroRecordConfiguration<T> : ChoAvroRecordConfiguration
    {
        public ChoAvroRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoAvroRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoAvroRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoAvroRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            base.Map(field, fieldName);
            return this;
        }

        public ChoAvroRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field,
            Action<ChoAvroRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoAvroRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoAvroRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoAvroRecordFieldConfigurationMap(cf).FieldName(fieldName);
            return this;
        }

        public ChoAvroRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, Action<ChoAvroRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoAvroRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);
            mapper?.Invoke(new ChoAvroRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoAvroRecordConfiguration<T> IndexMap<TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            base.IndexMap(field, minumum, maximum, mapper);
            return this;
        }

        public ChoAvroRecordConfiguration<T> DictionaryMap<TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoAvroRecordFieldConfigurationMap> mapper = null)
        {
            base.DictionaryMap(field, keys, mapper);
            return this;
        }

        #region Fluent API

        public ChoAvroRecordConfiguration<T> Configure(Action<ChoAvroRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoAvroRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoAvroRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }

        #endregion
    }
}
