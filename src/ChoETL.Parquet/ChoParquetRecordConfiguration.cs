using Newtonsoft.Json;
using Parquet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoParquetRecordConfiguration : ChoFileRecordConfiguration
    {
        private readonly Dictionary<string, dynamic> _indexMapDict = new Dictionary<string, dynamic>();
        internal readonly Dictionary<Type, Dictionary<string, ChoParquetRecordFieldConfiguration>> ParquetRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoParquetRecordFieldConfiguration>>();
        internal ChoTypeConverterFormatSpec CreateTypeConverterSpecsIfNull()
        {
            if (_typeConverterFormatSpec == null)
                _typeConverterFormatSpec = new ChoTypeConverterFormatSpec();

            return _typeConverterFormatSpec;
        }

        public ParquetOptions ParquetOptions
        {
            get;
        }
        public CompressionMethod CompressionMethod { get; set; }
        public CompressionLevel CompressionLevel { get; set; }
        public IReadOnlyDictionary<string, string> CustomMetadata { get; set; }
        public long RowGroupSize { get; set; } = 5000;
        public Func<Type, Type> MapParquetType { get; set; }

        public Func<string, object, object> ParquetFieldValueConverter
        {
            get;
            set;
        }

        public bool IgnoreRootNodeName
        {
            get;
            set;
        }

        public bool AutoArrayDiscovery
        {
            get;
            set;
        }

        public Parquet.Schema.ParquetSchema Schema
        {
            get;
            set;
        }

        public Func<Parquet.Schema.Field[], Parquet.Schema.ParquetSchema> SchemaGenerator
        {
            get;
            set;
        }

        //public bool AllowNestedArrayConversion
        //{
        //    get;
        //    set;
        //}

        [DataMember]
        public List<ChoParquetRecordFieldConfiguration> ParquetRecordFieldConfigurations
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
        internal Dictionary<string, ChoParquetRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoParquetRecordFieldConfiguration> RecordFieldConfigurationsDict2
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
                foreach (var fc in ParquetRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public bool IgnoreHeader { get; set; }
        public bool AutoFlush { get; set; } = true;
        public bool TreatDateTimeAsDateTimeOffset { get; set; }
        public bool TreatDateTimeAsString { get; set; }
        public TimeSpan? DateTimeOffset { get; set; }
        public Func<object, string> CustomSerializer { get; set; }
        public Func<string, Type, object> CustomDeserializer { get; set; }
        public Formatting Formatting { get; set; }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
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
        public bool LeaveStreamOpen { get; set; } = true;
        public bool TreatDateTimeOffsetAsString { get; set; } = true;

        public ChoParquetRecordFieldConfiguration this[string name]
        {
            get
            {
                return ParquetRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoParquetRecordConfiguration() : this(null)
        {
        }

        internal ChoParquetRecordConfiguration(Type recordType) : base(recordType)
        {
            ParquetOptions = new ParquetOptions();
            ParquetRecordFieldConfigurations = new List<ChoParquetRecordFieldConfiguration>();
            RowGroupSize = 0;
            if (recordType != null)
            {
                Init(recordType);
            }
            ConverterForType = (t, o) =>
            {
                if (o is ChoDynamicObject)
                    return new ChoParquetDynamicObjectConverter();
                else
                    return null;
            };
            NestedKeySeparator = '/';

            //RecordSelector = new Func<object, Type>((value) =>
            //{
            //    Tuple<long, string> kvp = value as Tuple<long, string>;
            //    string line = kvp.Item2;
            //    if (line.IsNullOrEmpty()) return RecordTypeConfiguration.DefaultRecordType;

            //    if (RecordTypeCodeExtractor != null)
            //    {
            //        string rt = RecordTypeCodeExtractor(line);
            //        return RecordTypeConfiguration[rt];
            //    }
            //    else
            //    {
            //        if (RecordTypeConfiguration.Position <= 0)
            //            return RecordTypeConfiguration.DefaultRecordType;

            //        string[] fieldValues = line.Split(Delimiter, StringSplitOptions, QuoteChar);
            //        if (fieldValues.Length > 0 && RecordTypeConfiguration.Position - 1 < fieldValues.Length)
            //        {
            //            if (RecordTypeConfiguration.Contains(fieldValues[RecordTypeConfiguration.Position - 1]))
            //                return RecordTypeConfiguration[fieldValues[RecordTypeConfiguration.Position - 1]];
            //        }

            //        return RecordTypeConfiguration.DefaultRecordType;
            //    }
            //});
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoParquetRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoParquetRecordObjectAttribute>(recordType);
            //if (IgnoreFieldValueMode == null)
            //    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Empty;

            if (ParquetRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType);
        }

        public ChoParquetRecordConfiguration ConfigureTypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            CreateTypeConverterSpecsIfNull();
            spec?.Invoke(TypeConverterFormatSpec);
            return this;
        }

        public ChoParquetRecordConfiguration MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
            return this;
        }

        public ChoParquetRecordConfiguration MapRecordFields(params Type[] recordTypes)
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
            List<ChoParquetRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = ParquetRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            int position = 0;
            DiscoverRecordFields(recordType, ref position, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().Any()).Any(), 
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
            List<ChoParquetRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = ParquetRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                ClearFields(); // ParquetRecordFieldConfigurations.Clear();

            DiscoverRecordFields(recordType, ref pos, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().Any()).Any(), 
                null, recordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null,
            bool optIn = false, PropertyDescriptor propDesc = null, List<ChoParquetRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (false) //!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, null, recordFieldConfigurations);
                        else if (pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoParquetRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
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
                                ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
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
                                        ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, range);
                                        //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                                                ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, range, propDesc.GetDisplayName());

                                                //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                                ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
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
                                        ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);

                                        //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                            ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, propDesc);
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

                                if (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo())  /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                {
                                    if (declaringMember == pd.Name)
                                    {

                                    }
                                    else
                                    {
                                        //if (propDesc == null)
                                        //    DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                        //else
                                        //    DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                        ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                        if (!recordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                            recordFieldConfigurations.Add(obj);
                                    }
                                }
                                else
                                {
                                    ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                    if (!recordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal ChoParquetRecordFieldConfiguration NewFieldConfiguration(ref int position, string declaringMember, PropertyDescriptor pd,
            int? arrayIndex = null, string displayName = null, string dictKey = null, bool ignoreAttrs = false, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            ChoParquetRecordFieldConfiguration obj = null;

            if (displayName.IsNullOrEmpty())
            {
                if (pd != null)
                    obj = new ChoParquetRecordFieldConfiguration(declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), ++position);
                else
                    obj = new ChoParquetRecordFieldConfiguration("Value", ++position);
            }
            else if (pd != null)
            {
                if (displayName.IsNullOrWhiteSpace())
                    obj = new ChoParquetRecordFieldConfiguration("{0}".FormatString(pd.Name), ++position);
                else
                    obj = new ChoParquetRecordFieldConfiguration("{0}.{1}".FormatString(displayName, pd.Name), ++position);
            }
            else
                obj = new ChoParquetRecordFieldConfiguration(displayName, ++position);

            //obj.FieldName = pd != null ? pd.Name : displayName;

            mapper?.Invoke(new ChoParquetRecordFieldConfigurationMap(obj));

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
                    if (fpAttr != null && fpAttr.Position >= 0)
                        obj.FieldPosition = fpAttr.Position;
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
                    var st = GetRecordConfigForType(pd.ComponentType).OfType<ChoParquetRecordFieldConfiguration>();
                    if (st != null && st.Any(fc => fc.Name == pd.Name))
                    {
                        var f = st.FirstOrDefault(fc => fc.Name == pd.Name);
                        if (f != null)
                        {
                            obj.FieldName = f.FieldName;
                            if (f.FieldPosition > 0 && arrayIndex == null)
                                obj.FieldPosition = f.FieldPosition;
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

        //protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        //{
        //    if (!IsDynamicObject)
        //    {
        //        string name = null;
        //        object defaultValue = null;
        //        object fallbackValue = null;
        //        foreach (var fc in fcs.OfType<ChoParquetRecordFieldConfiguration>())
        //        {
        //            name = fc.Name;

        //            if (!PDDict.ContainsKey(name))
        //            {
        //                if (!PDDict.ContainsKey(fc.FieldName))
        //                    continue;

        //                name = fc.FieldName;
        //            }

        //            fc.PD = PDDict[name];
        //            fc.PI = PIDict[name];

        //            //Load default value
        //            defaultValue = ChoType.GetRawDefaultValue(PDDict[name]);
        //            if (defaultValue != null)
        //            {
        //                fc.DefaultValue = defaultValue;
        //                fc.IsDefaultValueSpecified = true;
        //            }
        //            //Load fallback value
        //            fallbackValue = ChoType.GetRawFallbackValue(PDDict[name]);
        //            if (fallbackValue != null)
        //            {
        //                fc.FallbackValue = fallbackValue;
        //                fc.IsFallbackValueSpecified = true;
        //            }

        //            //Load Converters
        //            fc.PropConverters = ChoTypeDescriptor.GetTypeConverters(fc.PI);
        //            fc.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fc.PI);

        //        }
        //    }
        //    base.LoadNCacheMembers(fcs);
        //}
        internal void ValidateInternal(object state)
        {
            Validate(state);
        }

        protected override void Validate(object state)
        {
            base.Validate(state);

            string[] fieldNames = null;
            IDictionary<string, object> jObject = null;
            if (state is Tuple<long, IDictionary<string, object>>)
                jObject = ((Tuple<long, IDictionary<string, object>>)state).Item2;
            else
                fieldNames = state as string[];

            if (AutoDiscoverColumns
                && ParquetRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    /*&& ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().Any()).Any()*/)
                {
                    MapRecordFields(RecordType);
                }
                else if (jObject != null)
                {
                    Dictionary<string, ChoParquetRecordFieldConfiguration> dict = new Dictionary<string, ChoParquetRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    int index = 0;
                    foreach (var kvp in jObject)
                    {
                        name = kvp.Key;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoParquetRecordFieldConfiguration(name, ++index));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    foreach (ChoParquetRecordFieldConfiguration obj in dict.Values)
                        ParquetRecordFieldConfigurations.Add(obj);
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    int index = 0;

                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoParquetRecordFieldConfiguration(fn, ++index);
                        ParquetRecordFieldConfigurations.Add(obj);
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
                int maxFieldPos = ParquetRecordFieldConfigurations.Max(r => r.FieldPosition);
                foreach (var fieldConfig in ParquetRecordFieldConfigurations)
                {
                    if (fieldConfig.FieldPosition > 0) continue;
                    fieldConfig.FieldPosition = ++maxFieldPos;
                }
            }

            if (ParquetRecordFieldConfigurations.Count > 0)
                MaxFieldPosition = ParquetRecordFieldConfigurations.Max(r => r.FieldPosition);
            else
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Index map initialization
            foreach (var value in _indexMapDict.Values)
            {
                BuildIndexMap(value.fieldName, value.fieldType, value.minumum, value.maximum,
                    value.fieldName, value.displayName,
                    value.mapper);
            }

            //Validate each record field
            foreach (var fieldConfig in ParquetRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check if any field has 0 
            if (ParquetRecordFieldConfigurations.Where(i => i.FieldPosition <= 0).Count() > 0)
                throw new ChoRecordConfigurationException("Some fields contain invalid field position. All field positions must be > 0.");

            //Check field position for duplicate
            int[] dupPositions = ParquetRecordFieldConfigurations.GroupBy(i => i.FieldPosition)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupPositions.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field position(s) [Index: {0}] found.".FormatString(String.Join(",", dupPositions)));

            if (false)
            {
            }
            else
            {
                //Check if any field has empty names 
                if (ParquetRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = ParquetRecordFieldConfigurations.GroupBy(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in ParquetRecordFieldConfigurations)
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

            RecordFieldConfigurationsDict = ParquetRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);
            //RecordFieldConfigurationsDictGroup = RecordFieldConfigurationsDict.GroupBy(kvp => kvp.Key.Contains(".") ? kvp.Key.SplitNTrim(".").First() : kvp.Key).ToDictionary(i => i.Key, i => i.ToArray());
            RecordFieldConfigurationsDict2 = ParquetRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);

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

            LoadNCacheMembers(ParquetRecordFieldConfigurations);
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

        public new ChoParquetRecordConfiguration ClearFields()
        {
            _indexMapDict.Clear();
            ParquetRecordFieldConfigurationsForType.Clear();
            ParquetRecordFieldConfigurations.Clear();
            base.ClearFields();
            return this;
        }

        public ChoParquetRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (ParquetRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = ParquetRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                ParquetRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoParquetRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = ParquetRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                ParquetRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoParquetRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            Map(field, m => m.Position(position));
            return this;
        }

        public ChoParquetRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            Map(field, m => m.FieldName(fieldName));
            return this;
        }

        public ChoParquetRecordConfiguration Map(string propertyName, int position, Type fieldType = null)
        {
            Map(propertyName, m => m.Position(position).FieldType(fieldType));
            return this;
        }

        public ChoParquetRecordConfiguration Map(string propertyName, string fieldName = null, Type fieldType = null)
        {
            Map(propertyName, m => m.FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoParquetRecordConfiguration Map(string propertyName, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoParquetRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoParquetRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(), 
                pd, fqm/*, subType == typeof(T) ? null : subType*/);
            mapper?.Invoke(new ChoParquetRecordFieldConfigurationMap(cf));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                ParquetRecordFieldConfigurationsForType.Remove(rt);
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

            List<ChoParquetRecordFieldConfiguration> recordFieldConfigurations = new List<ChoParquetRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            ParquetRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        internal void AddFieldForType(Type rt, ChoParquetRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!ParquetRecordFieldConfigurationsForType.ContainsKey(rt))
                ParquetRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoParquetRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (ParquetRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                ParquetRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                ParquetRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        protected virtual new bool ContainsRecordConfigForType(Type rt)
        {
            return ParquetRecordFieldConfigurationsForType.ContainsKey(rt);
        }
        internal bool ContainsRecordConfigForTypeInternal(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }

        protected override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return ParquetRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
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

        protected override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return ParquetRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }
        internal Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForTypeInternal(Type rt)
        {
            return GetRecordConfigDictionaryForType(rt);
        }

        public ChoParquetRecordConfiguration MapForType<T, TField>(Expression<Func<T, TField>> field, int? position = null, 
            string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoParquetRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            var cf1 = new ChoParquetRecordFieldConfigurationMap(cf).FieldName(fieldName);
            if (position != null)
                cf1.Position(position.Value);

            return this;
        }

        internal void WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption? fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Type recordType = null, Type subRecordType = null,
            ChoFieldValueJustification? fieldValueJustification = null)
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
                ChoParquetRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (ParquetRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = ParquetRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    if (position == null || position <= 0)
                        position = fc.FieldPosition;

                    ParquetRecordFieldConfigurations.Remove(fc);
                }
                else if (subRecordType != null)
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                    if (position == null || position <= 0)
                    {
                        position = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        position++;
                    }
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                    if (position == null || position <= 0)
                    {
                        position = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        position++;
                    }
                }

                var nfc = new ChoParquetRecordFieldConfiguration(fnTrim, position.Value)
                {
                    FieldType = fieldType,
                    //QuoteField = quoteField,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldValueJustification = fieldValueJustification,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    ValueSelector = valueSelector,
                    CustomSerializer = customSerializer,
                    HeaderSelector = headerSelector,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    AltFieldNames = altFieldNames,
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

                if (subRecordType == null)
                    ParquetRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoParquetRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoParquetRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoParquetRecordFieldConfiguration(propertyName, attr, otherAttrs);
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
                if (!ParquetRecordFieldConfigurations.Any(fc => fc.DeclaringMemberInternal == fqm && fc.ArrayIndex == null))
                {
                    int fieldPosition = 0;
                    fieldPosition = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                    fieldPosition++;

                    var c = new ChoParquetRecordFieldConfiguration(propertyName, attr, otherAttrs);
                    c.FieldPosition = fieldPosition;
                    if (pd != null)
                    {
                        c.PropertyDescriptorInternal = pd;
                        c.FieldType = pd.PropertyType.GetUnderlyingType();
                    }

                    c.DeclaringMemberInternal = fqm;

                    ParquetRecordFieldConfigurations.Add(c);
                }

                return ParquetRecordFieldConfigurations.First(fc => fc.DeclaringMemberInternal == fqm && fc.ArrayIndex == null);
            }
        }

        public ChoParquetRecordConfiguration IndexMap(string fieldName, Type fieldType, int minumum, int maximum, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            IndexMapInternal(fieldName, fieldType, minumum, maximum, fieldName, fieldName, mapper);
            return this;
        }

        public ChoParquetRecordConfiguration IndexMap<T, TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
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
            Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
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
            Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
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
                    var fcs1 = ParquetRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fullyQualifiedMemberName).ToArray();
                    int priority = 0;
                    foreach (var fc in fcs1)
                    {
                        priority = fcs1.First().Priority;
                        displayName = fcs1.First().FieldName;
                        ParquetRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index < maximum; index++)
                    {
                        int fieldPosition = 0;
                        fieldPosition = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;

                        var nfc = new ChoParquetRecordFieldConfiguration(fieldName, fieldPosition) { ArrayIndex = index, Priority = priority };
                        mapper?.Invoke(new ChoParquetRecordFieldConfigurationMap(nfc));

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
                        nfc.FieldPosition = fieldPosition;
                        nfc.ArrayIndex = index;

                        nfc.FieldType = recordType;
                        ParquetRecordFieldConfigurations.Add(nfc);
                    }
                }
                else
                {
                    int priority = 0;

                    //Remove collection config member
                    var fcs1 = ParquetRecordFieldConfigurations.Where(o => o.Name == fqn).ToArray();
                    foreach (var fc in fcs1)
                    {
                        priority = fc.Priority;
                        ParquetRecordFieldConfigurations.Remove(fc);
                    }

                    //Remove any unused config
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                    {
                        var fcs = ParquetRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                        && o.ArrayIndex != null && (o.ArrayIndex < minumum || o.ArrayIndex > maximum)).ToArray();

                        foreach (var fc in fcs)
                            ParquetRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                        {
                            var fc = ParquetRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                            && o.ArrayIndex != null && o.ArrayIndex == index).FirstOrDefault();

                            if (fc != null) continue;

                            Type pt = pd.PropertyType.GetUnderlyingType();
                            if (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()))
                            {
                            }
                            else
                            {
                                int fieldPosition = 0;
                                fieldPosition = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                                //fieldPosition++;
                                ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, fullyQualifiedMemberName, pd, index, displayName, ignoreAttrs: false,
                                    mapper: mapper);

                                obj.Priority = priority;
                                //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                ParquetRecordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        public ChoParquetRecordConfiguration DictionaryMap(string fieldName, Type fieldType,
            string[] keys, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            DictionaryMapInternal(fieldName, fieldType, fieldName, keys, null, mapper);
            return this;
        }

        public ChoParquetRecordConfiguration DictionaryMap<T, TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            PropertyDescriptor pd = field.GetPropertyDescriptor();

            DictionaryMapInternal(pd.Name, fieldType, fqn, keys, pd, mapper);
            return this;
        }

        internal ChoParquetRecordConfiguration DictionaryMapInternal(string fieldName, Type fieldType, string fqn,
            string[] keys, PropertyDescriptor pd = null, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            List<ChoParquetRecordFieldConfiguration> fcsList = new List<ChoParquetRecordFieldConfiguration>();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                && typeof(string) == fieldType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
                //Remove collection config member
                var fcs1 = ParquetRecordFieldConfigurations.Where(o => o.Name == fqn).ToArray();
                foreach (var fc in fcs1)
                    ParquetRecordFieldConfigurations.Remove(fc);

                //Remove any unused config
                var fcs = ParquetRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fieldName
                && !o.DictKey.IsNullOrWhiteSpace() && !keys.Contains(o.DictKey)).ToArray();

                foreach (var fc in fcs)
                    ParquetRecordFieldConfigurations.Remove(fc);

                foreach (var key in keys)
                {
                    if (!key.IsNullOrWhiteSpace())
                    {
                        var fc = ParquetRecordFieldConfigurations.Where(o => o.DeclaringMemberInternal == fieldName
                            && !o.DictKey.IsNullOrWhiteSpace() && key == o.DictKey).FirstOrDefault();

                        if (fc != null) continue;

                        //ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);
                        int fieldPosition = 0;
                        fieldPosition = ParquetRecordFieldConfigurations.Count > 0 ? ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;
                        ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, null, pd, displayName: fieldName, dictKey: key, mapper: mapper);

                        //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                        //ParquetRecordFieldConfigurations.Add(obj);
                        ParquetRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            return this;
        }

        #region Fluent API

        public ChoParquetRecordConfiguration Configure(Action<ChoParquetRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion
    }

    public class ChoParquetRecordConfiguration<T> : ChoParquetRecordConfiguration
    {
        public ChoParquetRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoParquetRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoParquetRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoParquetRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            base.Map(field, position);
            return this;
        }

        public ChoParquetRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            base.Map(field, fieldName);
            return this;
        }

        public ChoParquetRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field,
            Action<ChoParquetRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoParquetRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoParquetRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoParquetRecordFieldConfigurationMap(cf).FieldName(fieldName);
            return this;
        }

        public ChoParquetRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, int position)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoParquetRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoParquetRecordFieldConfigurationMap(cf).Position(position);
            return this;
        }

        public ChoParquetRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, Action<ChoParquetRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoParquetRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);
            mapper?.Invoke(new ChoParquetRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoParquetRecordConfiguration<T> IndexMap<TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            base.IndexMap(field, minumum, maximum, mapper);
            return this;
        }

        public ChoParquetRecordConfiguration<T> DictionaryMap<TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoParquetRecordFieldConfigurationMap> mapper = null)
        {
            base.DictionaryMap(field, keys, mapper);
            return this;
        }

        #region Fluent API

        public ChoParquetRecordConfiguration<T> Configure(Action<ChoParquetRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoParquetRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoParquetRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }

        #endregion
    }

    public class ChoParquetDynamicObjectConverter : IChoValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ConvertToObject(targetType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
