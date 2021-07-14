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
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoFixedLengthRecordConfiguration : ChoFileRecordConfiguration
    {
        [DataMember]
        public ChoFixedLengthFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public ChoFixedLengthRecordTypeConfiguration RecordTypeConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public List<ChoFixedLengthRecordFieldConfiguration> FixedLengthRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public int RecordLength
        {
            get;
            set;
        }
        public ChoFixedLengthFieldDefaultSizeConfiguation FixedLengthFieldDefaultSizeConfiguation
        {
            get;
            set;
        }

        internal Dictionary<string, ChoFixedLengthRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoFixedLengthRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }

        private Func<string, string> _customTextSelecter = null;
        public Func<string, string> CustomTextSelecter
        {
            get { return _customTextSelecter; }
            set { if (value == null) return; _customTextSelecter = value; }
        }

        internal Dictionary<string, string> AlternativeKeys
        {
            get;
            set;
        }
        public readonly dynamic Context = new ChoDynamicObject();

        public ChoFixedLengthRecordFieldConfiguration this[string name]
        {
            get
            {
                return FixedLengthRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoFixedLengthRecordConfiguration() : this(null)
        {
        }

        internal ChoFixedLengthRecordConfiguration(Type recordType) : base(recordType)
        {
            FixedLengthRecordFieldConfigurations = new List<ChoFixedLengthRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            FileHeaderConfiguration = new ChoFixedLengthFileHeaderConfiguration(recordType, Culture);
            RecordTypeConfiguration = new ChoFixedLengthRecordTypeConfiguration();
            RecordTypeConfiguration.DefaultRecordType = recordType;

            RecordSelector = new Func<object, Type>((value) =>
            {
                Tuple<long, string> kvp = value as Tuple<long, string>;
                string line = kvp.Item2;
                if (line.IsNullOrEmpty()) return RecordTypeConfiguration.DefaultRecordType;

                if (RecordTypeCodeExtractor != null)
                {
                    string rt = RecordTypeCodeExtractor(line);
                    if (RecordTypeConfiguration.Contains(rt))
                        return RecordTypeConfiguration[rt];
                }
                else
                {
                    if (RecordTypeConfiguration.StartIndex >= 0 && RecordTypeConfiguration.Size == 0)
                        return RecordTypeConfiguration.DefaultRecordType;
                    if (RecordTypeConfiguration.StartIndex + RecordTypeConfiguration.Size > line.Length)
                        return RecordTypeConfiguration.DefaultRecordType;

                    string rtc = line.Substring(RecordTypeConfiguration.StartIndex, RecordTypeConfiguration.Size);
                    if (RecordTypeConfiguration.Contains(rtc))
                        return RecordTypeConfiguration[rtc];
                }

                return RecordTypeConfiguration.DefaultRecordType;
            });
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoFixedLengthRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFixedLengthRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                RecordLength = recObjAttr.RecordLength;
            }
            if (IgnoreFieldValueMode == null)
                IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Empty | ChoIgnoreFieldValueMode.WhiteSpace;

            if (FixedLengthRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType, true);
        }
        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in FixedLengthRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public bool IgnoreRootNodeName { get; set; }

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

        public ChoFixedLengthRecordConfiguration ClearFields()
        {
            //FixedLengthRecordFieldConfigurationsForType.Clear();
            FixedLengthRecordFieldConfigurations.Clear();
            return this;
        }

        public ChoFixedLengthRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (FixedLengthRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = FixedLengthRecordFieldConfigurations.Where(f => f.DeclaringMember == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                FixedLengthRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoFixedLengthRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = FixedLengthRecordFieldConfigurations.Where(f => f.DeclaringMember == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                FixedLengthRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoFixedLengthRecordConfiguration Map(string propertyName, int startIndex, int size, string fieldName, Type fieldType = null)
        {
            Map(propertyName, m => m.StartIndex(startIndex).Size(size).FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoFixedLengthRecordConfiguration Map(string propertyName, Action<ChoFixedLengthRecordFieldConfigurationMap> mapper)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoFixedLengthRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoFixedLengthRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, int startIndex, int size)
        {
            Map(field, m => m.StartIndex(startIndex).Size(size));
            return this;
        }

        public ChoFixedLengthRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoFixedLengthRecordFieldConfigurationMap> mapper)
        {
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray());
            mapper?.Invoke(new ChoFixedLengthRecordFieldConfigurationMap(cf));
            return this;
        }

        internal ChoFixedLengthRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoFixedLengthRecordFieldAttribute attr = null, Attribute[] otherAttrs = null)
        {
            if (!FixedLengthRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration(propertyName, attr, otherAttrs));

            return FixedLengthRecordFieldConfigurations.First(fc => fc.Name == propertyName);
        }

        public ChoFixedLengthRecordConfiguration MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T), true);
            return this;
        }

        public ChoFixedLengthRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault(), true);
            foreach (var rt in recordTypes.Skip(1).Where(rt => rt != null))
                DiscoverRecordFields(rt, false);

            return this;
        }

        private void DiscoverRecordFields(Type recordType, bool clear)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (clear)
            {
                //SupportsMultiRecordTypes = false;
                FixedLengthRecordFieldConfigurations.Clear();
            }
            //else
            //    SupportsMultiRecordTypes = true;

            DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()).Any());
        }

        private void DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false)
        {
            if (recordType == null)
                return;

            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                int startIndex = 0;
                int size = 0;

                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                        else if (pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoFixedLengthRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (!FixedLengthRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                FixedLengthRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
                else
                {
                    if (typeof(IList).IsAssignableFrom(recordType))
                    {

                    }
                    else if (recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                        && typeof(string) == recordType.GetGenericArguments()[0])
                    {

                    }
                    else
                    {
                        if (recordType == typeof(object))
                        {

                        }
                        else if (recordType.IsSimple())
                        {
                        }
                        else
                        {
                            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                            {
                                pt = pd.PropertyType.GetUnderlyingType();
                                if (pt != typeof(object) && !pt.IsSimple() /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                    DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                                else
                                {
                                    if (FixedLengthFieldDefaultSizeConfiguation == null)
                                        size = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(pd.PropertyType);
                                    else
                                        size = FixedLengthFieldDefaultSizeConfiguation.GetSize(pd.PropertyType);

                                    var obj = new ChoFixedLengthRecordFieldConfiguration(pd.Name, startIndex, size);
                                    obj.FieldType = pt;
                                    obj.PropertyDescriptor = pd;
                                    obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
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
                                    DisplayFormatAttribute dfAttr = pd.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
                                    if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                                    {
                                        obj.FormatText = dfAttr.DataFormatString;
                                    }
                                    if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                                    {
                                        obj.NullValue = dfAttr.NullDisplayText;
                                    }
                                    if (!FixedLengthRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                        FixedLengthRecordFieldConfigurations.Add(obj);

                                    startIndex += size;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            string line = null;
            string[] fieldNames = null;
            if (state is Tuple<long, string>)
                line = ((Tuple<long, string>)state).Item2;
            else
                fieldNames = state as string[];

            if (RecordLength <= 0 && line != null)
                RecordLength = line.Length;

            //Validate Header
            if (FileHeaderConfiguration != null)
            {
                if (FileHeaderConfiguration.FillChar != null)
                {
                    if (FileHeaderConfiguration.FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FileHeaderConfiguration.FillChar));
                    if (EOLDelimiter.Contains(FileHeaderConfiguration.FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar.Value, EOLDelimiter));
                    if (Comments != null)
                    {
                        if ((from comm in Comments
                             where comm.Contains(FileHeaderConfiguration.FillChar.Value.ToString())
                             select comm).Any())
                            throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                    }
                }
            }

            //string[] headers = state as string[];
            if (AutoDiscoverColumns
                && FixedLengthRecordFieldConfigurations.Count == 0 /*&& headers != null*/)
            {
                if (RecordType != null && !IsDynamicObject
                    && ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()).Any())
                {
                    MapRecordFields(RecordType);
                }
                else if (!line.IsNullOrEmpty())
                {
                    int index = 0;
                    if (IsDynamicObject)
                    {
                        foreach (var item in DiscoverColumns(line))
                        {
                            var obj = new ChoFixedLengthRecordFieldConfiguration(FileHeaderConfiguration.HasHeaderRecord ? item.Item1 : "Column{0}".FormatString(++index), item.Item2, item.Item3);
                            FixedLengthRecordFieldConfigurations.Add(obj);
                        }
                    }
                    else
                    {
                        Tuple<string, int, int>[] tuples = DiscoverColumns(line);
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(RecordType))
                        {
                            if (index < tuples.Length)
                            {
                                var obj = new ChoFixedLengthRecordFieldConfiguration(FileHeaderConfiguration.HasHeaderRecord ? tuples[index].Item1 : pd.Name, tuples[index].Item2, tuples[index].Item3);
                                FixedLengthRecordFieldConfigurations.Add(obj);
                                index++;
                            }
                            else
                                break;
                        }
                    }
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    int startIndex = 0;
                    int fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(typeof(string));
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoFixedLengthRecordFieldConfiguration(fn, startIndex, fieldLength);
                        FixedLengthRecordFieldConfigurations.Add(obj);
                        startIndex += fieldLength;
                    }
                }
            }

            if (FixedLengthRecordFieldConfigurations.Count == 0)
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Derive record length from fields
            if (RecordLength <= 0)
            {
                int maxStartIndex = FixedLengthRecordFieldConfigurations.Max(f => f.StartIndex);
                int maxSize = FixedLengthRecordFieldConfigurations.Where(f => f.StartIndex == maxStartIndex).Max(f1 => f1.Size.Value);
                var fc = FixedLengthRecordFieldConfigurations.Where(f => f.StartIndex == maxStartIndex && f.Size.Value == maxSize).FirstOrDefault();
                if (fc != null)
                {
                    RecordLength = fc.StartIndex + fc.Size.Value;
                }
            }

            if (RecordLength <= 0)
                throw new ChoRecordConfigurationException("RecordLength must be > 0");

            //Check if any field has empty names
            if (FixedLengthRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

            //Check field names for duplicate
            string[] dupFields = FixedLengthRecordFieldConfigurations.GroupBy(i => i.FieldName, FileHeaderConfiguration.StringComparer)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field names [Name: {0}] specified to record fields.".FormatString(String.Join(",", dupFields)));

            //Find duplicate fields with start index
            ChoFixedLengthRecordFieldConfiguration dupRecConfig = FixedLengthRecordFieldConfigurations.Where(c => c.Size > 0).GroupBy(i => i.StartIndex).Where(g => g.Count() > 1).Select(g => g.FirstOrDefault()).FirstOrDefault();
            if (dupRecConfig != null)
                throw new ChoRecordConfigurationException("Found duplicate '{0}' record field with same start index.".FormatString(dupRecConfig.FieldName));

            //Check any overlapping fields specified
            foreach (var f in FixedLengthRecordFieldConfigurations)
            {
                if (f.StartIndex + f.Size.Value > RecordLength)
                    throw new ChoRecordConfigurationException("Found '{0}' record field out of bounds of record length.".FormatString(f.FieldName));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(FileHeaderConfiguration.StringComparer);
            PDDict = new Dictionary<string, PropertyDescriptor>(FileHeaderConfiguration.StringComparer);
            foreach (var fc in FixedLengthRecordFieldConfigurations)
            {
                var pd1 = fc.DeclaringMember.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMember);
                if (pd1 != null)
                    fc.PropertyDescriptor = pd1;

                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.Name, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.Name, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = FixedLengthRecordFieldConfigurations.OrderBy(i => i.StartIndex).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name, FileHeaderConfiguration.StringComparer);
            RecordFieldConfigurationsDict2 = FixedLengthRecordFieldConfigurations.OrderBy(i => i.StartIndex).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);
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

            //Validate each record field
            foreach (var fieldConfig in FixedLengthRecordFieldConfigurations)
                fieldConfig.Validate(this);

            if (!FileHeaderConfiguration.HasHeaderRecord)
            {
            }
            else
            {
            }

            LoadNCacheMembers(FixedLengthRecordFieldConfigurations);
        }

        private Tuple<string, int, int>[] DiscoverColumns(string line)
        {
            List<Tuple<string, int, int>> words = new List<Tuple<string, int, int>>();
            if (!line.IsNullOrEmpty())
            {
                //const string text = "   Test42  a       yxx ";
                var result = new StringBuilder(line.Length);
                int i = 0;
                while (i < line.Length - 1)
                {
                    result.Append(line[i]);
                    if ((line[i] != '-'
                        && line[i] != '.'
                        && line[i] != '/'
                        && line[i] != '\\') && (Char.IsWhiteSpace(line[i]) && !Char.IsWhiteSpace(line[i + 1])
                        //|| char.IsUpper(line[i + 1])
                        //|| !char.IsDigit(line[i]) && char.IsDigit(line[i + 1])
                        ))
                    {
                        if (Char.IsWhiteSpace(line[i]) && Char.IsWhiteSpace(line[i + 1]))
                        {

                        }
                        else
                        {
                            if (!result.ToString().IsNullOrWhiteSpace())
                            {
                                words.Add(new Tuple<string, int, int>(result.ToString(), i - (result.Length - 1), result.Length));
                                result.Clear();
                            }
                        }
                    }

                    i++;
                }
                result.Append(line[line.Length - 1]);
                string word = result.ToString();
                if (!word.IsNullOrWhiteSpace() || words.Count == 0)
                    words.Add(new Tuple<string, int, int>(result.ToString(), i - (result.Length - 1), result.Length));
                else
                {
                    Tuple<string, int, int> tuple = words[words.Count - 1];
                    words.RemoveAt(words.Count - 1);
                    words.Add(new Tuple<string, int, int>(tuple.Item1 + result.ToString(), tuple.Item2, tuple.Item3 + result.Length));
                }
            }
            //foreach (var item in words)
            //    Console.WriteLine(item.Item1 + " " + item.Item2 + " " + item.Item3);
            return words.ToArray();
        }

        internal ChoFixedLengthRecordFieldConfiguration GetFieldConfiguration(string fn)
        {
            if (!FixedLengthRecordFieldConfigurations.Any(fc => fc.Name == fn))
                FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration(fn));

            return FixedLengthRecordFieldConfigurations.First(fc => fc.Name == fn);
        }

        #region Fluent API

        public ChoFixedLengthRecordConfiguration Configure(Action<ChoFixedLengthRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoFixedLengthRecordConfiguration IgnoreHeader()
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = true;

            return this;
        }

        public ChoFixedLengthRecordConfiguration WithFirstLineHeader(bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public ChoFixedLengthRecordConfiguration WithHeaderLineAt(int pos = 1, bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HeaderLineAt = pos;
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public ChoFixedLengthRecordConfiguration HeaderLineAt(long value)
        {
            FileHeaderConfiguration.HeaderLineAt = value;
            return this;
        }

        public ChoFixedLengthRecordConfiguration IgnoreCase(bool value)
        {
            FileHeaderConfiguration.IgnoreCase = value;
            return this;
        }

        #endregion
    }

    public class ChoFixedLengthRecordConfiguration<T> : ChoFixedLengthRecordConfiguration
    {
        public ChoFixedLengthRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoFixedLengthRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoFixedLengthRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoFixedLengthRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, int startIndex, int size)
        {
            base.Map(field, startIndex, size);
            return this;
        }

        public ChoFixedLengthRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, Action<ChoFixedLengthRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        #region Fluent API

        public ChoFixedLengthRecordConfiguration<T> Configure(Action<ChoFixedLengthRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> IgnoreHeader()
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = true;

            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> WithFirstLineHeader(bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> WithHeaderLineAt(int pos = 1, bool ignoreHeader = false)
        {
            FileHeaderConfiguration.HeaderLineAt = pos;
            FileHeaderConfiguration.HasHeaderRecord = true;
            FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> HeaderLineAt(long value)
        {
            FileHeaderConfiguration.HeaderLineAt = value;
            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> IgnoreCase(bool value)
        {
            FileHeaderConfiguration.IgnoreCase = value;
            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoFixedLengthRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }

        #endregion
    }
}
