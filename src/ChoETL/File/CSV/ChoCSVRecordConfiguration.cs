using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoCSVRecordConfiguration : ChoFileRecordConfiguration
    {
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

        public ChoCSVRecordConfiguration Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        internal KeyValuePair<string, ChoCSVRecordFieldConfiguration>[] FCArray;

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

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null, bool optIn = false)
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
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                        else
                        {
                            var obj = new ChoCSVRecordFieldConfiguration(pd.Name, ++position);
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? null : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;
                            DisplayAttribute dpAttr = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                            if (dpAttr != null)
                            {
                                if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                                    obj.FieldName = dpAttr.ShortName;
                                else if (!dpAttr.Name.IsNullOrWhiteSpace())
                                    obj.FieldName = dpAttr.Name;
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
                            if (!CSVRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                CSVRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
            }
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
                if (headers != null)
                {
                    int index = 0;
                    CSVRecordFieldConfigurations = (from header in headers
                                                    select new ChoCSVRecordFieldConfiguration(header, ++index)).ToList();
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

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>();
            PDDict = new Dictionary<string, PropertyDescriptor>();
            foreach (var fc in CSVRecordFieldConfigurations)
            {
                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.PropertyDescriptor.Name, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.PropertyDescriptor.Name, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name, FileHeaderConfiguration.StringComparer);
            RecordFieldConfigurationsDict2 = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);
            AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name, FileHeaderConfiguration.StringComparer);
            FCArray = RecordFieldConfigurationsDict.ToArray();

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
    }
}
