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
    [DataContract]
    public class ChoCSVRecordConfiguration : ChoFileRecordConfiguration
    {
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

        public ChoCSVRecordConfiguration Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
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

        public override void MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
        }

        public override void MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return;

            int pos = 0;
            DiscoverRecordFields(recordTypes.FirstOrDefault(), ref pos, true);
            foreach (var rt in recordTypes.Skip(1))
                DiscoverRecordFields(rt, ref pos, false);
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
            DiscoverRecordFields(recordType, ref pos, true);
        }

        private void DiscoverRecordFields(Type recordType, ref int pos, bool clear = true)
        {
            if (recordType == null)
                return;

            if (clear)
            {
                //SupportsMultiRecordTypes = false;
                CSVRecordFieldConfigurations.Clear();
            }
            //else
            //SupportsMultiRecordTypes = true;

            DiscoverRecordFields(recordType, ref pos, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any());
        }

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null,
            bool optIn = false, PropertyDescriptor propDesc = null)
        {
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                        else if (pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoCSVRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? null : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (!CSVRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                CSVRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
                else
                {
                    if (typeof(IList).IsAssignableFrom(recordType)
                        && !typeof(ArrayList).IsAssignableFrom(recordType)
                        && !recordType.IsInterface)
                    {
                        if (propDesc != null)
                        {
                            RangeAttribute dnAttr = propDesc.Attributes.OfType<RangeAttribute>().FirstOrDefault();

                            if (dnAttr == null)
                            {
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                CSVRecordFieldConfigurations.Add(obj);
                            }
                            else if (dnAttr != null && dnAttr.Minimum.CastTo<int>() >= 0 && dnAttr.Maximum.CastTo<int>() > 0
                                && dnAttr.Minimum.CastTo<int>() <= dnAttr.Maximum.CastTo<int>())
                            {
                                recordType = recordType.GetItemType().GetUnderlyingType();

                                if (recordType.IsSimple())
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range <= dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, range);
                                        //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        CSVRecordFieldConfigurations.Add(obj);
                                    }
                                }
                                else
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range <= dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                                        {
                                            pt = pd.PropertyType.GetUnderlyingType();
                                            if (pt != typeof(object) && !pt.IsSimple() /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                            {
                                                //DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd);
                                            }
                                            else
                                            {
                                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, range, propDesc.GetDisplayName());

                                                //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                                CSVRecordFieldConfigurations.Add(obj);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                        /*&& typeof(string) == recordType.GetGenericArguments()[0]*/)
                    {
                        if (propDesc != null)
                        {
                            ChoDictionaryKeyAttribute[] dnAttrs = propDesc.Attributes.OfType<ChoDictionaryKeyAttribute>().ToArray();
                            if (dnAttrs.IsNullOrEmpty())
                            {
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                CSVRecordFieldConfigurations.Add(obj);
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
                                        CSVRecordFieldConfigurations.Add(obj);
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
                            if (!CSVRecordFieldConfigurations.Any(c => c.Name == propDesc.Name))
                                CSVRecordFieldConfigurations.Add(obj);
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
                                    if (propDesc == null)
                                        DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd);
                                    else
                                        DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd);
                                }
                                else
                                {
                                    ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                    if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        CSVRecordFieldConfigurations.Add(obj);
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
                obj = new ChoCSVRecordFieldConfiguration(declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), ++position);
            else if (pd != null)
                obj = new ChoCSVRecordFieldConfiguration("{0}.{1}".FormatString(displayName, pd.Name), ++position);
            else
                obj = new ChoCSVRecordFieldConfiguration(displayName, ++position);

            obj.FieldName = pd != null ? pd.Name : displayName;

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

            if (arrayIndex != null)
            {
                if (ArrayIndexSeparator == null)
                    obj.Name = obj.FieldName = obj.FieldName + "_" + arrayIndex;
                else
                    obj.Name = obj.FieldName = obj.FieldName + ArrayIndexSeparator + arrayIndex;
            }
            else if (!dictKey.IsNullOrWhiteSpace())
            {
                obj.FieldName = dictKey;
            }

            return obj;
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

                if (dupFields.Length > 0 && !IgnoreDuplicateFields)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>();
            PDDict = new Dictionary<string, PropertyDescriptor>();
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

                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.FieldName, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.FieldName, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);
            //RecordFieldConfigurationsDictGroup = RecordFieldConfigurationsDict.GroupBy(kvp => kvp.Key.Contains(".") ? kvp.Key.SplitNTrim(".").First() : kvp.Key).ToDictionary(i => i.Key, i => i.ToArray());
            RecordFieldConfigurationsDict2 = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);
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

        public ChoCSVRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            Map(field, m => m.Position(position));
            return this;
        }

        public ChoCSVRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string fieldName)
        {
            Map(field, m => m.FieldName(fieldName));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, int position)
        {
            Map(propertyName, m => m.Position(position));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, string fieldName)
        {
            Map(propertyName, m => m.FieldName(fieldName));
            return this;
        }

        public ChoCSVRecordConfiguration Map(string propertyName, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoCSVRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoCSVRecordFieldConfigurationMap> mapper = null)
        {
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(), pd, fqm);
            mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(cf));
            return this;
        }

        internal ChoCSVRecordFieldConfiguration GetFieldConfiguration(string fn, ChoCSVRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null)
        {
            if (fqm == null)
                fqm = fn;

            fn = fn.SplitNTrim(".").LastOrDefault();
            if (!CSVRecordFieldConfigurations.Any(fc => fc.DeclaringMember == fqm))
            {
                var c = new ChoCSVRecordFieldConfiguration(fn, attr, otherAttrs);
                if (pd != null)
                {
                    c.PropertyDescriptor = pd;
                    c.FieldType = pd.PropertyType.GetUnderlyingType();
                }

                c.DeclaringMember = fqm;

                CSVRecordFieldConfigurations.Add(c);
            }

            return CSVRecordFieldConfigurations.First(fc => fc.DeclaringMember == fqm);
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

            if (typeof(IList).IsAssignableFrom(fieldType) && !typeof(ArrayList).IsAssignableFrom(fieldType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum)
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

            if (typeof(IList).IsAssignableFrom(recordType) && !typeof(ArrayList).IsAssignableFrom(recordType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum
                && !fieldType.IsInterface)
            {
                var itemType = recordType.GetItemType().GetUnderlyingType();
                if (itemType.IsSimple())
                {
                    for (int index = minumum; index <= maximum; index++)
                    {
                        int fieldPosition = 0;
                        fieldPosition = CSVRecordFieldConfigurations.Count > 0 ? CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;

                        var nfc = new ChoCSVRecordFieldConfiguration(fieldName, fieldPosition) { ArrayIndex = index };
                        mapper?.Invoke(new ChoCSVRecordFieldConfigurationMap(nfc));

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
                    //Remove collection config member
                    var fcs1 = CSVRecordFieldConfigurations.Where(o => o.FieldName == fqn).ToArray();
                    foreach (var fc in fcs1)
                        CSVRecordFieldConfigurations.Remove(fc);

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
                                fieldPosition++;
                                ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, fullyQualifiedMemberName, pd, index, displayName, ignoreAttrs: true,
                                    mapper: mapper);

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
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                && typeof(string) == fieldType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
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
        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, int position)
        {
            base.Map(field, position);
            return this;
        }

        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string fieldName)
        {
            base.Map(field, fieldName);
            return this;
        }

        public ChoCSVRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, 
            Action<ChoCSVRecordFieldConfigurationMap> setup = null)
        {
            base.Map(field, setup);
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
    }
}
