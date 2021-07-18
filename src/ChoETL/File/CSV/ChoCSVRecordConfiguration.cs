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
    [DataContract]
    public class ChoCSVRecordConfiguration : ChoFileRecordConfiguration
    {
        private readonly Dictionary<string, dynamic> _indexMapDict = new Dictionary<string, dynamic>();
        internal readonly Dictionary<Type, Dictionary<string, ChoCSVRecordFieldConfiguration>> CSVRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoCSVRecordFieldConfiguration>>();

        private char[] _autoDetectDelimiterChars = { ';', '|', '\t', ',' };
        public char[] AutoDetectDelimiterChars
        {
            get { return _autoDetectDelimiterChars; }
            set
            {
                if (value != null && value.Length > 0)
                    _autoDetectDelimiterChars = value;
            }
        }

        public bool ThrowAndStopOnMissingCSVColumn
        {
            get;
            set;
        }

        public bool AutoDetectDelimiter
        {
            get;
            set;
        }

        public bool AutoIncrementDuplicateColumnNames
        {
            get;
            set;
        }

        private int _autoIncrementStartIndex = 2;
        public int AutoIncrementStartIndex
        {
            get { return _autoIncrementStartIndex; }
            set
            {
                if (value < 0)
                    return;
                _autoIncrementStartIndex = value;
            }
        }

        public bool AutoIncrementAllDuplicateColumnNames
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

        public bool ImplicitExcelFieldValueHandling
        {
            get;
            set;
        }

        public int ArrayBaseIndex
        {
            get;
            set;
        }
        public bool AllowLoadingFieldByPosition
        {
            get;
            set;
        }
        public bool TurnOnMultiLineHeaderSupport
        {
            get;
            set;
        }
        [DataMember]
        public ChoCSVFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public ChoCSVRecordTypeConfiguration RecordTypeConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public string Delimiter
        {
            get;
            set;
        }
        [DataMember]
        public bool? HasExcelSeparator
        {
            get;
            set;
        }
        [DataMember]
        public List<ChoCSVRecordFieldConfiguration> CSVRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public bool Sanitize
        {
            get;
            set;
        }
        [DataMember]
        public string InjectionChars
        {
            get;
            set;
        }
        [DataMember]
        public char InjectionEscapeChar
        {
            get;
            set;
        }
        public readonly dynamic Context = new ChoDynamicObject();

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
        internal int MaxFieldPosition
        {
            get;
            set;
        }
        internal Dictionary<string, ChoCSVRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoCSVRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }
        //internal Dictionary<string, KeyValuePair<string, ChoCSVRecordFieldConfiguration>[]> RecordFieldConfigurationsDictGroup
        //{
        //    get;
        //    private set;
        //}

        private Func<string, string> _customTextSelecter = null;
        public Func<string, string> CustomTextSelecter
        {
            get { return _customTextSelecter; }
            set { if (value == null) return; _customTextSelecter = value; }
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in CSVRecordFieldConfigurations)
                    yield return fc;
            }
        }

        //internal KeyValuePair<string, ChoCSVRecordFieldConfiguration>[] FCArray;

        public ChoCSVRecordFieldConfiguration this[string name]
        {
            get
            {
                return CSVRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoCSVRecordConfiguration() : this(null)
        {
        }

        internal ChoCSVRecordConfiguration(Type recordType) : base(recordType)
        {
            CSVRecordFieldConfigurations = new List<ChoCSVRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            if (Delimiter.IsNullOrEmpty())
            {
                if (Culture != null)
                    Delimiter = Culture.TextInfo.ListSeparator;

                if (Delimiter.IsNullOrWhiteSpace())
                    Delimiter = ",";
            }

            Sanitize = false;
            InjectionChars = "=@+-";
            InjectionEscapeChar = '\t';

            FileHeaderConfiguration = new ChoCSVFileHeaderConfiguration(recordType, Culture);
            RecordTypeConfiguration = new ChoCSVRecordTypeConfiguration();
            RecordTypeConfiguration.DefaultRecordType = recordType;

            RecordSelector = new Func<object, Type>((value) =>
            {
                Tuple<long, string> kvp = value as Tuple<long, string>;
                string line = kvp.Item2;
                if (line.IsNullOrEmpty()) return RecordTypeConfiguration.DefaultRecordType;

                if (RecordTypeCodeExtractor != null)
                {
                    string rt = RecordTypeCodeExtractor(line);
                    return RecordTypeConfiguration[rt];
                }
                else
                {
                    if (RecordTypeConfiguration.Position <= 0)
                        return RecordTypeConfiguration.DefaultRecordType;

                    string[] fieldValues = line.Split(Delimiter, StringSplitOptions, QuoteChar);
                    if (fieldValues.Length > 0 && RecordTypeConfiguration.Position - 1 < fieldValues.Length)
                    {
                        if (RecordTypeConfiguration.Contains(fieldValues[RecordTypeConfiguration.Position - 1]))
                            return RecordTypeConfiguration[fieldValues[RecordTypeConfiguration.Position - 1]];
                    }

                    return RecordTypeConfiguration.DefaultRecordType;
                }
            });
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ThrowAndStopOnMissingCSVColumn = true;

            ChoCSVRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoCSVRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                Delimiter = recObjAttr.Delimiter;
                HasExcelSeparator = recObjAttr.HasExcelSeparatorInternal;
            }
            if (IgnoreFieldValueMode == null)
                IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Empty;

            if (CSVRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType);
        }

        public ChoCSVRecordConfiguration MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
            return this;
        }

        public ChoCSVRecordConfiguration MapRecordFields(params Type[] recordTypes)
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
            List<ChoCSVRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = CSVRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            int position = 0;
            DiscoverRecordFields(recordType, ref position, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any(), 
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
            List<ChoCSVRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = CSVRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                ClearFields(); // CSVRecordFieldConfigurations.Clear();

            DiscoverRecordFields(recordType, ref pos, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any(), 
                null, recordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null,
            bool optIn = false, PropertyDescriptor propDesc = null, List<ChoCSVRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, null, recordFieldConfigurations);
                        else if (pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoCSVRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
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
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
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
                                        ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, range);
                                        //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                                            if (pt != typeof(object) && !pt.IsSimple() /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                            {
                                                DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                            }
                                            else
                                            {
                                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, range, propDesc.GetDisplayName());

                                                //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
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
                                        ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);

                                        //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
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
                            ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, propDesc);
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

                                if (pt != typeof(object) && !pt.IsSimple()  /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
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
                                    ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                    if (!recordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal ChoCSVRecordFieldConfiguration NewFieldConfiguration(ref int position, string declaringMember, PropertyDescriptor pd,
            int? arrayIndex = null, string displayName = null, string dictKey = null, bool ignoreAttrs = false, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            ChoCSVRecordFieldConfiguration obj = null;

            if (displayName.IsNullOrEmpty())
            {
                if (pd != null)
                    obj = new ChoCSVRecordFieldConfiguration(declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), ++position);
                else
                    obj = new ChoCSVRecordFieldConfiguration("Value", ++position);
            }
            else if (pd != null)
            {
                if (displayName.IsNullOrWhiteSpace())
                    obj = new ChoCSVRecordFieldConfiguration("{0}".FormatString(pd.Name), ++position);
                else
                    obj = new ChoCSVRecordFieldConfiguration("{0}.{1}".FormatString(displayName, pd.Name), ++position);
            }
            else
                obj = new ChoCSVRecordFieldConfiguration(displayName, ++position);

            //obj.FieldName = pd != null ? pd.Name : displayName;

            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(obj));

            obj.DictKey = dictKey;
            obj.ArrayIndex = arrayIndex;
            obj.FieldType = pd != null ? pd.PropertyType : null; // pt;
            obj.PropertyDescriptor = pd;
            if (pd != null)
                obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
            else
                obj.DeclaringMember = displayName;

            if (arrayIndex == null && pd != null)
            {
                if (!ignoreAttrs)
                {
                    ChoFieldPositionAttribute fpAttr = pd.Attributes.OfType<ChoFieldPositionAttribute>().FirstOrDefault();
                    if (fpAttr != null && fpAttr.Position > 0)
                        obj.FieldPosition = fpAttr.Position;
                }
            }
            else
            {

            }

            if (!ignoreAttrs && pd != null)
            {
                obj.Optional = pd.Attributes.OfType<OptionalFieldAttribute>().Any();

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
                if (ContainsRecordConfigForType(pd.ComponentType))
                {
                    var st = GetRecordConfigForType(pd.ComponentType).OfType<ChoCSVRecordFieldConfiguration>();
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
                var arrayIndexSeparator = ArrayIndexSeparator == null ? ChoETLSettings.ArrayIndexSeparator : ArrayIndexSeparator.Value;

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

        protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        {
            if (!IsDynamicObject)
            {
                string name = null;
                object defaultValue = null;
                object fallbackValue = null;
                foreach (var fc in fcs.OfType<ChoCSVRecordFieldConfiguration>())
                {
                    name = fc.Name;

                    if (!PDDict.ContainsKey(name))
                    {
                        if (!PDDict.ContainsKey(fc.FieldName))
                            continue;

                        name = fc.FieldName;
                    }

                    fc.PD = PDDict[name];
                    fc.PI = PIDict[name];

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
                    fc.PropConverters = ChoTypeDescriptor.GetTypeConverters(fc.PI);
                    fc.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fc.PI);

                    //Load Custom Serializer
                    fc.PropCustomSerializer = ChoTypeDescriptor.GetCustomSerializer(fc.PI);
                    fc.PropCustomSerializerParams = ChoTypeDescriptor.GetCustomSerializerParams(fc.PI);

                }
            }
            base.LoadNCacheMembers(fcs);
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            if (Delimiter.IsNull())
                throw new ChoRecordConfigurationException("Delimiter can't be null or whitespace.");
            if (Delimiter == EOLDelimiter)
                throw new ChoRecordConfigurationException("Delimiter [{0}] can't be same as EODDelimiter [{1}]".FormatString(Delimiter, EOLDelimiter));
            if (Delimiter.Contains(QuoteChar))
                throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(QuoteChar, Delimiter));
            if (Comments != null && Comments.Contains(Delimiter))
                throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");

            //Validate Header
            if (FileHeaderConfiguration != null)
            {
                if (FileHeaderConfiguration.FillChar != null)
                {
                    ValidateChar(FileHeaderConfiguration.FillChar.Value, nameof(FileHeaderConfiguration.FillChar));
                }
            }

            string[] headers = state as string[];
            if (AutoDiscoverColumns
                && CSVRecordFieldConfigurations.Count == 0)
            {
                if (headers != null && IsDynamicObject)
                {
                    int index = 0;
                    CSVRecordFieldConfigurations = (from header in headers
                                                    where !IgnoredFields.Contains(header)
                                                    select new ChoCSVRecordFieldConfiguration(header, ++index)
                                                    ).ToList();
                }
                else
                {
                    MapRecordFields(RecordType);
                }
            }
            else
            {
                int maxFieldPos = CSVRecordFieldConfigurations.Max(r => r.FieldPosition);
                foreach (var fieldConfig in CSVRecordFieldConfigurations)
                {
                    if (fieldConfig.FieldPosition > 0) continue;
                    fieldConfig.FieldPosition = ++maxFieldPos;
                }
            }

            if (CSVRecordFieldConfigurations.Count > 0)
                MaxFieldPosition = CSVRecordFieldConfigurations.Max(r => r.FieldPosition);
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
            foreach (var fieldConfig in CSVRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check if any field has 0 
            if (CSVRecordFieldConfigurations.Where(i => i.FieldPosition <= 0).Count() > 0)
                throw new ChoRecordConfigurationException("Some fields contain invalid field position. All field positions must be > 0.");

            //Check field position for duplicate
            int[] dupPositions = CSVRecordFieldConfigurations.GroupBy(i => i.FieldPosition)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupPositions.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field position(s) [Index: {0}] found.".FormatString(String.Join(",", dupPositions)));

            if (!FileHeaderConfiguration.HasHeaderRecord)
            {
            }
            else
            {
                //Check if any field has empty names 
                if (CSVRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = CSVRecordFieldConfigurations.GroupBy(i => i.FieldName, FileHeaderConfiguration.StringComparer)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0)
                {
                    if (!AutoIncrementDuplicateColumnNames)
                        throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
                    else
                    {
                        var dupFieldConfigs = CSVRecordFieldConfigurations.GroupBy(i => i.FieldName, FileHeaderConfiguration.StringComparer)
                                            .Where(g => g.Count() > 1)
                                            .ToArray();

                        var arrayIndexSeparator = ArrayIndexSeparator == ChoCharEx.NUL ? ChoETLSettings.ArrayIndexSeparator : ArrayIndexSeparator;
                        int index = AutoIncrementStartIndex;
                        string fieldName = null;
                        foreach (var grp in dupFieldConfigs)
                        {
                            index = AutoIncrementStartIndex;
                            fieldName = grp.Key;
                            var g = AutoIncrementAllDuplicateColumnNames ? grp.ToArray() : grp.ToArray().Skip(1);
                            foreach (var fc in g)
                            {
                                fc.FieldName = $"{fc.FieldName}{arrayIndexSeparator}{index}";
                                index++;
                            }
                        }
                    }
                }
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(FileHeaderConfiguration.StringComparer);
            PDDict = new Dictionary<string, PropertyDescriptor>(FileHeaderConfiguration.StringComparer);
            foreach (var fc in CSVRecordFieldConfigurations)
            {
                if (fc.PropertyDescriptor == null && !IsDynamicObject)
                {
                    var pd = ChoTypeDescriptor.GetProperty(RecordType, fc.Name);
                    if (pd == null)
                        pd = ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMember);

                    if (pd != null)
                    {
                        fc.PropertyDescriptor = pd;
                        if (fc.FieldType == null)
                            fc.FieldType = pd.PropertyType.GetUnderlyingType();
                    }
                }

                var pd1 = fc.DeclaringMember.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
                    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMember);
                if (pd1 != null)
                    fc.PropertyDescriptor = pd1;

                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.FieldName, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.FieldName, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name, FileHeaderConfiguration.StringComparer);
            //RecordFieldConfigurationsDictGroup = RecordFieldConfigurationsDict.GroupBy(kvp => kvp.Key.Contains(".") ? kvp.Key.SplitNTrim(".").First() : kvp.Key).ToDictionary(i => i.Key, i => i.ToArray());
            RecordFieldConfigurationsDict2 = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);

            try
            {
                if (IsDynamicObject)
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp =>
                    {
                        if (kvp.Key == kvp.Value.Name)
                            return kvp.Value.Name.ToValidVariableName();
                        else
                            return kvp.Value.Name;
                    }, kvp => kvp.Key, FileHeaderConfiguration.StringComparer);
                else
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name, FileHeaderConfiguration.StringComparer);
            }
            catch { }

            //FCArray = RecordFieldConfigurationsDict.ToArray();

            LoadNCacheMembers(CSVRecordFieldConfigurations);

            if (Sanitize)
            {
                ValidateChar(InjectionEscapeChar, nameof(InjectionEscapeChar));
                foreach (char injectionChar in InjectionChars)
                {
                    ValidateChar(injectionChar, nameof(InjectionChars));
                    if (injectionChar.ToString().IsAlphaNumeric())
                        throw new ChoRecordConfigurationException("Invalid '{0}' injection char specified.".FormatString(injectionChar));
                }
            }

            if (RecordTypeConfiguration != null)
            {
                if (RecordSelector == null && RecordTypeCodeExtractor == null)
                {
                }
            }
        }

        private void ValidateChar(char src, string name)
        {
            if (src == ChoCharEx.NUL)
                throw new ChoRecordConfigurationException("Invalid 'NUL' {0} specified.".FormatString(name));
            if (Delimiter.Contains(src))
                throw new ChoRecordConfigurationException("{2} [{0}] can't be one of Delimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar, Delimiter, name));
            if (EOLDelimiter.Contains(src))
                throw new ChoRecordConfigurationException("{2} [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(src, EOLDelimiter, name));
            if ((from comm in Comments
                 where comm.Contains(src.ToString())
                 select comm).Any())
                throw new ChoRecordConfigurationException("One of the Comments contains {0}. Not allowed.".FormatString(name));
        }

        public ChoCSVRecordConfiguration ClearFields()
        {
            _indexMapDict.Clear();
            CSVRecordFieldConfigurationsForType.Clear();
            CSVRecordFieldConfigurations.Clear();
            return this;
        }

        public ChoCSVRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (CSVRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = CSVRecordFieldConfigurations.Where(f => f.DeclaringMember == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                CSVRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoCSVRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = CSVRecordFieldConfigurations.Where(f => f.DeclaringMember == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                CSVRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoCSVRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            Map(field, m => m.Position(position));
            return this;
        }

        public ChoCSVRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            Map(field, m => m.FieldName(fieldName));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, int position, Type fieldType = null)
        {
            Map(propertyName, m => m.Position(position).FieldType(fieldType));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, string fieldName, Type fieldType = null)
        {
            Map(propertyName, m => m.FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoCSVRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoCSVRecordFieldConfigurationMap<T>> mapper = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(), 
                pd, fqm/*, subType == typeof(T) ? null : subType*/);
            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap<T>(cf));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForType(rt))
                CSVRecordFieldConfigurationsForType.Remove(rt);
        }

        public void MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForType(rt))
                return;

            List<ChoCSVRecordFieldConfiguration> recordFieldConfigurations = new List<ChoCSVRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            CSVRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        internal void AddFieldForType(Type rt, ChoCSVRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!CSVRecordFieldConfigurationsForType.ContainsKey(rt))
                CSVRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoCSVRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (CSVRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                CSVRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                CSVRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        public override bool ContainsRecordConfigForType(Type rt)
        {
            return CSVRecordFieldConfigurationsForType.ContainsKey(rt);
        }

        public override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForType(rt))
                return CSVRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        public override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForType(rt))
                return CSVRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }

        public ChoCSVRecordConfiguration MapForType<T, TField>(Expression<Func<T, TField>> field, int? position = null, 
            string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoCSVRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            var cf1 = new ChoCSVRecordFieldConfigurationMap(cf).FieldName(fieldName);
            if (position != null)
                cf1.Position(position.Value);

            return this;
        }

        internal void WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption? fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false, Type recordType = null, Type subRecordType = null,
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
                ChoCSVRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    if (position == null || position <= 0)
                        position = fc.FieldPosition;

                    CSVRecordFieldConfigurations.Remove(fc);
                }
                else if (subRecordType != null)
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                    if (position == null || position <= 0)
                    {
                        position = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        position++;
                    }
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                    if (position == null || position <= 0)
                    {
                        position = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        position++;
                    }
                }

                var nfc = new ChoCSVRecordFieldConfiguration(fnTrim, position.Value)
                {
                    FieldType = fieldType,
                    QuoteField = quoteField,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldValueJustification = fieldValueJustification,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    ValueSelector = valueSelector,
                    HeaderSelector = headerSelector,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    AltFieldNames = altFieldNames,
                    FormatText = formatText,
                    Optional = optional,
                    NullValue = nullValue,
                    ExcelField = excelField
                };
                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : fullyQualifiedMemberName;
                }
                else
                {
                    if (subRecordType == null)
                        pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName);
                    else
                        pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName);

                    nfc.PropertyDescriptor = pd;
                    nfc.DeclaringMember = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                if (subRecordType == null)
                    CSVRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoCSVRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoCSVRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoCSVRecordFieldConfiguration(propertyName, attr, otherAttrs);
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                //if (!CSVRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                //    CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(propertyName, attr, otherAttrs));

                //return CSVRecordFieldConfigurations.First(fc => fc.Name == propertyName);
                if (fqm == null)
                    fqm = propertyName;

                if (CSVRecordFieldConfigurations.Any(o => o.Name == propertyName))
                {
                    var fc1 = CSVRecordFieldConfigurations.Where(o => o.Name == propertyName).First();
                    if (fc1 != null)
                        return fc1;
                }

                propertyName = propertyName.SplitNTrim(".").LastOrDefault();
                if (!CSVRecordFieldConfigurations.Any(fc => fc.DeclaringMember == fqm && fc.ArrayIndex == null))
                {
                    int fieldPosition = 0;
                    fieldPosition = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                    fieldPosition++;

                    var c = new ChoCSVRecordFieldConfiguration(propertyName, attr, otherAttrs);
                    c.FieldPosition = fieldPosition;
                    if (pd != null)
                    {
                        c.PropertyDescriptor = pd;
                        c.FieldType = pd.PropertyType.GetUnderlyingType();
                    }

                    c.DeclaringMember = fqm;

                    CSVRecordFieldConfigurations.Add(c);
                }

                return CSVRecordFieldConfigurations.First(fc => fc.DeclaringMember == fqm && fc.ArrayIndex == null);
            }
        }

        public ChoCSVRecordConfiguration IndexMap(string fieldName, Type fieldType, int minumum, int maximum, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            IndexMapInternal(fieldName, fieldType, minumum, maximum, fieldName, fieldName, mapper);
            return this;
        }

        public ChoCSVRecordConfiguration IndexMap<T, TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
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
            Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(fieldType))
    && !typeof(ArrayList).IsAssignableFrom(fieldType)
    && minumum >= 0 /*&& maximum >= 0 && minumum <= maximum*/)
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
            WithFirstLineHeader();
        }

        internal void BuildIndexMap(string fieldName, Type fieldType, int minumum, int maximum,
            string fullyQualifiedMemberName = null, string displayName = null,
            Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
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
                    var fcs1 = CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == fullyQualifiedMemberName).ToArray();
                    int priority = 0;
                    foreach (var fc in fcs1)
                    {
                        priority = fcs1.First().Priority;
                        displayName = fcs1.First().FieldName;
                        CSVRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index < maximum; index++)
                    {
                        int fieldPosition = 0;
                        fieldPosition = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;

                        var nfc = new ChoCSVRecordFieldConfiguration(fieldName, fieldPosition) { ArrayIndex = index, Priority = priority };
                        mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(nfc));

                        if (displayName != null)
                            nfc.FieldName = displayName;

                        string lFieldName = null;
                        if (ArrayIndexSeparator == null)
                            lFieldName = nfc.FieldName + "_" + index;
                        else
                            lFieldName = nfc.FieldName + ArrayIndexSeparator + index;

                        nfc.DeclaringMember = nfc.Name;
                        nfc.Name = lFieldName;
                        nfc.FieldName = lFieldName;
                        nfc.FieldPosition = fieldPosition;
                        nfc.ArrayIndex = index;

                        nfc.FieldType = recordType;
                        CSVRecordFieldConfigurations.Add(nfc);
                    }
                }
                else
                {
                    int priority = 0;

                    //Remove collection config member
                    var fcs1 = CSVRecordFieldConfigurations.Where(o => o.FieldName == fqn).ToArray();
                    foreach (var fc in fcs1)
                    {
                        priority = fc.Priority;
                        CSVRecordFieldConfigurations.Remove(fc);
                    }

                    //Remove any unused config
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                    {
                        var fcs = CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                        && o.ArrayIndex != null && (o.ArrayIndex < minumum || o.ArrayIndex > maximum)).ToArray();

                        foreach (var fc in fcs)
                            CSVRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                        {
                            var fc = CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                            && o.ArrayIndex != null && o.ArrayIndex == index).FirstOrDefault();

                            if (fc != null) continue;

                            Type pt = pd.PropertyType.GetUnderlyingType();
                            if (pt != typeof(object) && !pt.IsSimple())
                            {
                            }
                            else
                            {
                                int fieldPosition = 0;
                                fieldPosition = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                                //fieldPosition++;
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, fullyQualifiedMemberName, pd, index, displayName, ignoreAttrs: false,
                                    mapper: mapper);

                                obj.Priority = priority;
                                //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                CSVRecordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        public ChoCSVRecordConfiguration DictionaryMap(string fieldName, Type fieldType,
            string[] keys, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            DictionaryMapInternal(fieldName, fieldType, fieldName, keys, null, mapper);
            return this;
        }

        public ChoCSVRecordConfiguration DictionaryMap<T, TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            PropertyDescriptor pd = field.GetPropertyDescriptor();

            DictionaryMapInternal(pd.Name, fieldType, fqn, keys, pd, mapper);
            return this;
        }

        internal ChoCSVRecordConfiguration DictionaryMapInternal(string fieldName, Type fieldType, string fqn,
            string[] keys, PropertyDescriptor pd = null, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            List<ChoCSVRecordFieldConfiguration> fcsList = new List<ChoCSVRecordFieldConfiguration>();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                && typeof(string) == fieldType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
                WithFirstLineHeader();

                //Remove collection config member
                var fcs1 = CSVRecordFieldConfigurations.Where(o => o.FieldName == fqn).ToArray();
                foreach (var fc in fcs1)
                    CSVRecordFieldConfigurations.Remove(fc);

                //Remove any unused config
                var fcs = CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == fieldName
                && !o.DictKey.IsNullOrWhiteSpace() && !keys.Contains(o.DictKey)).ToArray();

                foreach (var fc in fcs)
                    CSVRecordFieldConfigurations.Remove(fc);

                foreach (var key in keys)
                {
                    if (!key.IsNullOrWhiteSpace())
                    {
                        var fc = CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == fieldName
                            && !o.DictKey.IsNullOrWhiteSpace() && key == o.DictKey).FirstOrDefault();

                        if (fc != null) continue;

                        //ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);
                        int fieldPosition = 0;
                        fieldPosition = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;
                        ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, null, pd, displayName: fieldName, dictKey: key, mapper: mapper);

                        //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                        //CSVRecordFieldConfigurations.Add(obj);
                        CSVRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            return this;
        }

        #region Fluent API

        public ChoCSVRecordConfiguration Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoCSVRecordConfiguration IgnoreHeader()
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = true;

            return this;
        }

        public ChoCSVRecordConfiguration WithFirstLineHeader(bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public ChoCSVRecordConfiguration WithHeaderLineAt(int pos = 1, bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HeaderLineAt = pos;
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public ChoCSVRecordConfiguration HeaderLineAt(long value)
        {
            FileHeaderConfiguration.HeaderLineAt = value;
            return this;
        }

        public ChoCSVRecordConfiguration IgnoreCase(bool value)
        {
            FileHeaderConfiguration.IgnoreCase = value;
            return this;
        }

        #endregion 
    }

    public class ChoCSVRecordConfiguration<T> : ChoCSVRecordConfiguration
    {
        public ChoCSVRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoCSVRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoCSVRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            base.Map(field, position);
            return this;
        }

        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            base.Map(field, fieldName);
            return this;
        }

        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field,
            Action<ChoCSVRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoCSVRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoCSVRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoCSVRecordFieldConfigurationMap(cf).FieldName(fieldName);
            return this;
        }

        public ChoCSVRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, int position)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoCSVRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoCSVRecordFieldConfigurationMap(cf).Position(position);
            return this;
        }

        public ChoCSVRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, Action<ChoCSVRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);
            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoCSVRecordConfiguration<T> IndexMap<TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            base.IndexMap(field, minumum, maximum, mapper);
            return this;
        }

        public ChoCSVRecordConfiguration<T> DictionaryMap<TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            base.DictionaryMap(field, keys, mapper);
            return this;
        }

        #region Fluent API

        public ChoCSVRecordConfiguration<T> Configure(Action<ChoCSVRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoCSVRecordConfiguration<T> IgnoreHeader()
        {
            base.IgnoreHeader();
            return this;
        }

        public new ChoCSVRecordConfiguration<T> WithFirstLineHeader(bool ignoreHeader = false)
        {
            base.WithFirstLineHeader(ignoreHeader);
            return this;
        }

        public new ChoCSVRecordConfiguration<T> WithHeaderLineAt(int pos = 1, bool ignoreHeader = false)
        {
            base.WithHeaderLineAt(pos, ignoreHeader);
            return this;
        }

        public new ChoCSVRecordConfiguration<T> HeaderLineAt(long value)
        {
            base.HeaderLineAt(value);
            return this;
        }

        public new ChoCSVRecordConfiguration<T> IgnoreCase(bool value)
        {
            base.IgnoreCase(value);
            return this;
        }

        public new ChoCSVRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoCSVRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }

        #endregion
    }
}
