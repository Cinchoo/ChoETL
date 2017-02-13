using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoXmlRecordConfiguration : ChoFileRecordConfiguration
    {
        public ChoCSVFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }

        public List<ChoCSVRecordFieldConfiguration> CSVRecordFieldConfigurations
        {
            get;
            private set;
        }
        public string XPath
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

        public ChoCSVRecordFieldConfiguration this[string name]
        {
            get
            {
                return CSVRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoXmlRecordConfiguration(Type recordType = null) : base(recordType)
        {
            CSVRecordFieldConfigurations = new List<ChoCSVRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            if (XPath.IsNullOrEmpty())
            {
                XPath = "./*";
            }

            FileHeaderConfiguration = new ChoCSVFileHeaderConfiguration(recordType, Culture);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoCSVRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoCSVRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                XPath = recObjAttr.Delimiter;
                HasExcelSeparator = recObjAttr.HasExcelSeparatorInternal;
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
                CSVRecordFieldConfigurations.Clear();

                if (TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoCSVRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoCSVRecordFieldAttribute>().First());
                        obj.FieldType = pd.PropertyType;
                        CSVRecordFieldConfigurations.Add(obj);
                    }
                }
                else
                {
                    int position = 0;
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>())
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoCSVRecordFieldConfiguration(pd.Name, ++position);
                        obj.FieldType = pd.PropertyType;
                        CSVRecordFieldConfigurations.Add(obj);
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            if (XPath.IsNull())
                throw new ChoRecordConfigurationException("Delimiter can't be null or whitespace.");
            if (XPath == EOLDelimiter)
                throw new ChoRecordConfigurationException("Delimiter [{0}] can't be same as EODDelimiter [{1}]".FormatString(XPath, EOLDelimiter));
            if (XPath.Contains(QuoteChar))
                throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(QuoteChar, XPath));
            if (Comments.Contains(XPath))
                throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");

            //Validate Header
            if (FileHeaderConfiguration != null)
            {
                if (FileHeaderConfiguration.FillChar != null)
                {
                    if (FileHeaderConfiguration.FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FileHeaderConfiguration.FillChar));
                    if (XPath.Contains(FileHeaderConfiguration.FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar, XPath));
                    if (EOLDelimiter.Contains(FileHeaderConfiguration.FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar.Value, EOLDelimiter));
                    if ((from comm in Comments
                         where comm.Contains(FileHeaderConfiguration.FillChar.Value.ToString())
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
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

            if (CSVRecordFieldConfigurations.Count > 0)
                MaxFieldPosition = CSVRecordFieldConfigurations.Max(r => r.FieldPosition);
            else
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            //foreach (var fieldConfig in CSVRecordFieldConfigurations)
            //    fieldConfig.Validate(this);

            if (!FileHeaderConfiguration.HasHeaderRecord)
            {
                //Check if any field has 0 
                if (CSVRecordFieldConfigurations.Where(i => i.FieldPosition <= 0).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields contain invalid field position. All field positions must be > 0.");

                //Check field position for duplicate
                int[] dupPositions = CSVRecordFieldConfigurations.GroupBy(i => i.FieldPosition)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupPositions.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field position(s) [Index: {0}] specified to record fields.".FormatString(String.Join(",", dupPositions)));
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
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] specified to record fields.".FormatString(String.Join(",", dupFields)));
            }

            RecordFieldConfigurationsDict = CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);
        }
    }
}
