using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoKVPRecordConfiguration : ChoFileRecordConfiguration
    {
        [DataMember]
        public ChoKVPFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public string RecordStart
        {
            get;
            set;
        }
        [DataMember]
        public string RecordEnd
        {
            get;
            set;
        }
        [DataMember]
        public string Separator
        {
            get;
            set;
        }
        public char[] LineContinuationChars
        {
            get;
            set;
        }

        [DataMember]
        public List<ChoKVPRecordFieldConfiguration> KVPRecordFieldConfigurations
        {
            get;
            private set;
        }

        internal Dictionary<string, ChoKVPRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoKVPRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }

        internal KeyValuePair<string, ChoKVPRecordFieldConfiguration>[] FCArray;
        internal bool AutoDiscoveredColumns = false;
        private bool _isWildcardComparisionOnRecordStart = false;
        private bool _isWildcardComparisionOnRecordEnd = false;
        private ChoWildcard _recordStartWildCard = null;
        private ChoWildcard _recordEndWildCard = null;

        public ChoKVPRecordFieldConfiguration this[string name]
        {
            get
            {
                return KVPRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoKVPRecordConfiguration() : this(null)
        {

        }

        internal ChoKVPRecordConfiguration(Type recordType) : base(recordType)
        {
            KVPRecordFieldConfigurations = new List<ChoKVPRecordFieldConfiguration>();
            LineContinuationChars = new char[] { ' ', '\t' };

            if (recordType != null)
            {
                Init(recordType);
            }

            if (Separator.IsNullOrEmpty())
            {
                if (Separator.IsNullOrWhiteSpace())
                    Separator = ":";
            }

            FileHeaderConfiguration = new ChoKVPFileHeaderConfiguration(recordType, Culture);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoKVPRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoKVPRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                Separator = recObjAttr.Separator;
                RecordStart = recObjAttr.RecordStart;
                RecordEnd = recObjAttr.RecordEnd;
                LineContinuationChars = recObjAttr.LineContinuationChars;
            }

            DiscoverRecordFields(recordType);
        }

        public override void MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
        }

        public override void MapRecordFields(Type recordType)
        {
            DiscoverRecordFields(recordType);
        }

        private void DiscoverRecordFields(Type recordType)
        {
            if (!IsDynamicObject)
            {
                KVPRecordFieldConfigurations.Clear();

                if (ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoKVPRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoKVPRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoKVPRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoKVPRecordFieldAttribute>().First());
                        obj.FieldType = pd.PropertyType;
                        KVPRecordFieldConfigurations.Add(obj);
                    }
                }
                else
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoKVPRecordFieldConfiguration(pd.Name);
                        obj.FieldType = pd.PropertyType;
                        KVPRecordFieldConfigurations.Add(obj);
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            if (state == null)
            {
                base.Validate(state);

                if (Separator.IsNullOrWhiteSpace())
                    throw new ChoRecordConfigurationException("Separator can't be null or whitespace.");
                if (Separator == EOLDelimiter)
                    throw new ChoRecordConfigurationException("Separator [{0}] can't be same as EODDelimiter [{1}]".FormatString(Separator, EOLDelimiter));
                if (Separator.Contains(QuoteChar))
                    throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(QuoteChar, Separator));
                if (Comments != null && Comments.Contains(Separator))
                    throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");
                if (RecordStart.IsNullOrWhiteSpace() && RecordEnd.IsNullOrWhiteSpace())
                {

                }
                else
                {
                    if (RecordStart.IsNullOrWhiteSpace())
                        throw new ChoRecordConfigurationException("RecordStart is missing.");
                    //else if (RecordEnd.IsNullOrWhiteSpace())
                    //    RecordEnd = RecordStart;
                    //throw new ChoRecordConfigurationException("RecordEnd is missing.");

                    if (RecordStart.Contains("*") || RecordStart.Contains("?"))
                    {
                        _isWildcardComparisionOnRecordStart = true;
                        _recordStartWildCard = new ChoWildcard(RecordStart);
                    }
                    if (!RecordEnd.IsNullOrWhiteSpace() && (RecordEnd.EndsWith("*") || RecordStart.Contains("?")))
                    {
                        _isWildcardComparisionOnRecordEnd = true;
                        _recordEndWildCard = new ChoWildcard(RecordEnd);
                    }
                }

                //Validate Header
                if (FileHeaderConfiguration != null)
                {
                    if (FileHeaderConfiguration.FillChar != null)
                    {
                        if (FileHeaderConfiguration.FillChar.Value == ChoCharEx.NUL)
                            throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FileHeaderConfiguration.FillChar));
                        if (Separator.Contains(FileHeaderConfiguration.FillChar.Value))
                            throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar, Separator));
                        if (EOLDelimiter.Contains(FileHeaderConfiguration.FillChar.Value))
                            throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar.Value, EOLDelimiter));
                        if ((from comm in Comments
                             where comm.Contains(FileHeaderConfiguration.FillChar.Value.ToString())
                             select comm).Any())
                            throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                    }
                }
            }
            else
            {
                string[] headers = state as string[];
                if (AutoDiscoverColumns
                    && KVPRecordFieldConfigurations.Count == 0)
                {
                    AutoDiscoveredColumns = true;
                    if (headers != null)
                    {
                        KVPRecordFieldConfigurations = (from header in headers
                                                        select new ChoKVPRecordFieldConfiguration(header)).ToList();
                    }
                    else
                    {
                        MapRecordFields(RecordType);
                    }
                }

                if (KVPRecordFieldConfigurations.Count <= 0)
                    throw new ChoRecordConfigurationException("No record fields specified.");

                //Validate each record field
                foreach (var fieldConfig in KVPRecordFieldConfigurations)
                    fieldConfig.Validate(this);

                //Check if any field has empty names 
                if (KVPRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = KVPRecordFieldConfigurations.GroupBy(i => i.FieldName, FileHeaderConfiguration.StringComparer)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0 /* && !IgnoreDuplicateFields */)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));

                RecordFieldConfigurationsDict = KVPRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).GroupBy(i => i.Name).Select(g => g.First()).ToDictionary(i => i.Name, FileHeaderConfiguration.StringComparer);
                RecordFieldConfigurationsDict2 = KVPRecordFieldConfigurations.Where(i => !i.FieldName.IsNullOrWhiteSpace()).GroupBy(i => i.Name).Select(g => g.First()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);
                FCArray = RecordFieldConfigurationsDict.ToArray();

                LoadNCacheMembers(KVPRecordFieldConfigurations);
            }
        }

        internal bool IsRecordStartMatch(string line)
        {
            if (_isWildcardComparisionOnRecordStart)
            {
                return _recordStartWildCard.IsMatch(line);
            }
            else
            {
                return FileHeaderConfiguration.IsEqual(line, RecordStart);
            }
        }

        internal bool IsRecordEndMatch(string line)
        {
            if (RecordEnd.IsNullOrWhiteSpace())
            {
                return IsRecordStartMatch(line);
            }
            else
            {
                if (_isWildcardComparisionOnRecordEnd)
                {
                    return _recordEndWildCard.IsMatch(line);
                }
                else
                {
                    return FileHeaderConfiguration.IsEqual(line, RecordEnd);
                }
            }
        }
    }
}
