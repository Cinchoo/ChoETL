using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVRecordConfiguration : ChoFileRecordConfiguration
    {
        public static readonly ChoCSVRecordConfiguration Default = new ChoCSVRecordConfiguration();

        public ChoCSVFileHeaderConfiguration CSVFileHeaderConfiguration
        {
            get;
            set;
        }

        public List<ChoCSVRecordFieldConfiguration> RecordFieldConfigurations
        {
            get;
            private set;
        }
        public string Delimiter
        {
            get;
            set;
        }
        public bool? HasExcelSeparator
        {
            get;
            set;
        }
        internal int MaxFieldPosition
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoCSVRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }

        public ChoCSVRecordConfiguration(Type recordType = null) : base(recordType)
        {
            RecordFieldConfigurations = new List<ChoCSVRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }
            CSVFileHeaderConfiguration = new ChoCSVFileHeaderConfiguration(recordType, Culture);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoCSVRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoCSVRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                Delimiter = recObjAttr.Delimiter;
                HasExcelSeparator = recObjAttr._hasExcelSeparator;
            }

            DiscoverRecordFields(recordType);
        }

        private void DiscoverRecordFields(Type recordType)
        {
            RecordFieldConfigurations.Clear();
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()))
            {
                var obj = new ChoCSVRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().First());
                obj.FieldType = pd.PropertyType;
                RecordFieldConfigurations.Add(obj);
            }
        }

        public override void Validate()
        {
            if (RecordFieldConfigurations.Count > 0)
                MaxFieldPosition = RecordFieldConfigurations.Max(r => r.FieldPosition);
            else
                throw new ChoRecordConfigurationException("No record fields specified.");
            RecordFieldConfigurationsDict = RecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            if (Delimiter.IsNullOrWhiteSpace())
            {
                Delimiter = Culture.TextInfo.ListSeparator;
                if (Delimiter.IsNullOrWhiteSpace())
                    Delimiter = ",";
            }

            base.Validate();

            if (Delimiter.IsNullOrWhiteSpace())
                throw new ChoRecordConfigurationException("Delimiter can't be null or whitespace.");
            if (Delimiter == EOLDelimiter)
                throw new ChoRecordConfigurationException("Delimiter [{0}] can't be same as EODDelimiter [{1}]".FormatString(Delimiter, EOLDelimiter));
            if (Delimiter.Contains(QuoteChar))
                throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(QuoteChar, Delimiter));
            if (Comments.Contains(Delimiter))
                throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");

            //Validate Header
            if (CSVFileHeaderConfiguration != null)
                CSVFileHeaderConfiguration.Validate(this);

            //Validate each record field
            foreach (var fieldConfig in RecordFieldConfigurations)
                fieldConfig.Validate(this);

            if (!CSVFileHeaderConfiguration.HasHeaderRecord)
            {
                //Check if any field has 0 
                if (RecordFieldConfigurations.Where(i => i.FieldPosition <= 0).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields contain invalid field position. All field positions must be > 0.");

                //Check field position for duplicate
                int[] dupPositions = RecordFieldConfigurations.GroupBy(i => i.FieldPosition)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupPositions.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field positions [Index: {0}] specified to record fields.".FormatString(String.Join(",", dupPositions)));
            }
            else
            {
                //Check if any field has empty names 
                if (RecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = RecordFieldConfigurations.GroupBy(i => i.FieldName, CSVFileHeaderConfiguration.StringComparer)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field names [Name: {0}] specified to record fields.".FormatString(String.Join(",", dupFields)));
            }
        }
    }
}
