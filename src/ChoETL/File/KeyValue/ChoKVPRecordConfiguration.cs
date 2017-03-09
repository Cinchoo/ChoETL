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
        public string Seperator
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

        internal KeyValuePair<string, ChoKVPRecordFieldConfiguration>[] FCArray;

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

            if (recordType != null)
            {
                Init(recordType);
            }

            if (Seperator.IsNullOrEmpty())
            {
                if (Seperator.IsNullOrWhiteSpace())
                    Seperator = ":";
            }

            FileHeaderConfiguration = new ChoKVPFileHeaderConfiguration(recordType, Culture);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoKVPRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoKVPRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                Seperator = recObjAttr.Delimiter;
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
            if (recordType != typeof(ExpandoObject))
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

                if (Seperator.IsNullOrWhiteSpace())
                    throw new ChoRecordConfigurationException("Delimiter can't be null or whitespace.");
                if (Seperator == EOLDelimiter)
                    throw new ChoRecordConfigurationException("Delimiter [{0}] can't be same as EODDelimiter [{1}]".FormatString(Seperator, EOLDelimiter));
                if (Seperator.Contains(QuoteChar))
                    throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(QuoteChar, Seperator));
                if (Comments != null && Comments.Contains(Seperator))
                    throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");
                if (RecordStart.IsNullOrWhiteSpace() && RecordEnd.IsNullOrWhiteSpace())
                {

                }
                else
                {
                    if (RecordStart.IsNullOrWhiteSpace())
                        throw new ChoRecordConfigurationException("RecordStart is missing.");
                    else if (RecordEnd.IsNullOrWhiteSpace())
                        throw new ChoRecordConfigurationException("RecordEnd is missing.");
                }

                //Validate Header
                if (FileHeaderConfiguration != null)
                {
                    if (FileHeaderConfiguration.FillChar != null)
                    {
                        if (FileHeaderConfiguration.FillChar.Value == ChoCharEx.NUL)
                            throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FileHeaderConfiguration.FillChar));
                        if (Seperator.Contains(FileHeaderConfiguration.FillChar.Value))
                            throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar, Seperator));
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

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));

                RecordFieldConfigurationsDict = KVPRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);
                FCArray = RecordFieldConfigurationsDict.ToArray();

                LoadNCacheMembers(KVPRecordFieldConfigurations);
            }
        }
    }
}
