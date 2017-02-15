using GotDotNet.XPath;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChoETL
{
    public class ChoXmlRecordConfiguration : ChoFileRecordConfiguration
    {
        public List<ChoXmlRecordFieldConfiguration> XmlRecordFieldConfigurations
        {
            get;
            private set;
        }
        public string XPath
        {
            get;
            set;
        }
        internal Dictionary<string, ChoXmlRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }

        public ChoXmlRecordFieldConfiguration this[string name]
        {
            get
            {
                return XmlRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoXmlRecordConfiguration(Type recordType = null) : base(recordType)
        {
            XmlRecordFieldConfigurations = new List<ChoXmlRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            if (XPath.IsNullOrEmpty())
            {
                XPath = "//*";
            }
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoXmlRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoXmlRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
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
                XmlRecordFieldConfigurations.Clear();

                if (TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoXmlRecordFieldAttribute>().First());
                        obj.FieldType = pd.PropertyType;
                        XmlRecordFieldConfigurations.Add(obj);
                    }
                }
                else
                {
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>())
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, $"//{pd.Name}");
                        obj.FieldType = pd.PropertyType;
                        XmlRecordFieldConfigurations.Add(obj);
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            if (XPath.IsNull())
                throw new ChoRecordConfigurationException("XPath can't be null or whitespace.");

            XPathReader xpr = state is Tuple<int, XPathReader> ? ((Tuple<int, XPathReader>)state).Item2 : null;
            if (AutoDiscoverColumns
                && XmlRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && RecordType != typeof(ExpandoObject)
                    && TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlRecordFieldAttribute>().Any()).Any())
                {
                    int startIndex = 0;
                    int size = 0;
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, $"//{pd.Name}");
                        obj.FieldType = pd.PropertyType;
                        XmlRecordFieldConfigurations.Add(obj);

                        startIndex += size;
                    }

                    //RecordLength = startIndex;
                }
                else if (xpr != null)
                {
                    var x = XDocument.Parse(xpr.ReadOuterXml());
                    IDictionary dict = x.Root
                          .Elements()
                          .ToDictionary(
                            d => d.Name.LocalName, // avoids getting an IDictionary<XName,string>
                            l => l.Name.LocalName);

                    foreach (string name in dict.Keys)
                    {
                        var obj = new ChoXmlRecordFieldConfiguration(name, $"//{name}");
                        XmlRecordFieldConfigurations.Add(obj);
                    }
                }
            }

            if (XmlRecordFieldConfigurations.Count <= 0)
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            //foreach (var fieldConfig in XmlRecordFieldConfigurations)
            //    fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = XmlRecordFieldConfigurations.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] specified found.".FormatString(String.Join(",", dupFields)));

            RecordFieldConfigurationsDict = XmlRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);
        }
    }
}
