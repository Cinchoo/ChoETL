using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
        public string DefaultNamespace
        {
            get;
            set;
        }
        public XmlNamespaceManager NamespaceManager
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

        public ChoXmlRecordConfiguration() : this(null)
        {

        }

        internal ChoXmlRecordConfiguration(Type recordType) : base(recordType)
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
            NamespaceManager = new XmlNamespaceManager(new NameTable());
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

                if (TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(recordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().First());
                        if (obj.XPath.IsNullOrWhiteSpace())
                            obj.XPath = $"//{obj.FieldName}|//@{obj.FieldName}";

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

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, $"//{pd.Name}|//@{pd.Name}");
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

            XElement xpr = state is Tuple<int, XElement> ? ((Tuple<int, XElement>)state).Item2 : null;
            if (AutoDiscoverColumns
                && XmlRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && RecordType != typeof(ExpandoObject)
                    && TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any())
                {
                    int startIndex = 0;
                    int size = 0;
                    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        var obj = new ChoXmlRecordFieldConfiguration(pd.Name, $"//{pd.Name}|//@{pd.Name}");
                        obj.FieldType = pd.PropertyType;
                        XmlRecordFieldConfigurations.Add(obj);

                        startIndex += size;
                    }

                    //RecordLength = startIndex;
                }
                else if (xpr != null)
                {
                    ChoXmlNamespaceManager nsMgr = new ChoXmlNamespaceManager(NamespaceManager);

                    Dictionary<string, ChoXmlRecordFieldConfiguration> dict = new Dictionary<string, ChoXmlRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    foreach (var attr in xpr.Attributes())
                    {
                        //if (!attr.Name.NamespaceName.IsNullOrWhiteSpace()) continue;

                        name = GetNameWithNamespace(attr.Name);

                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(name, DefaultNamespace.IsNullOrWhiteSpace() ? $"//@{name}" : $"//@{DefaultNamespace}" + ":" + $"{name}") { IsXmlAttribute = true });
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    bool hasElements = false;
                    foreach (var ele in xpr.Elements())
                    {
                        name = GetNameWithNamespace(ele.Name);

                        hasElements = true;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(name, DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
                        else
                        {
                            if (dict[name].IsXmlAttribute)
                                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));

                            dict[name].IsCollection = true;
                        }
                    }

                    if (!hasElements)
                    {
                        name = xpr.Name.LocalName;
                        dict.Add(name, new ChoXmlRecordFieldConfiguration(name, "text()"));
                    }

                    foreach (ChoXmlRecordFieldConfiguration obj in dict.Values)
                        XmlRecordFieldConfigurations.Add(obj);
                }
            }
            else
            {
                foreach (var fc in XmlRecordFieldConfigurations)
                {
                    if (fc.XPath.IsNullOrWhiteSpace())
                        fc.XPath = $"//{fc.FieldName}|//@{fc.FieldName}";
                }
            }

            if (XmlRecordFieldConfigurations.Count <= 0)
                throw new ChoRecordConfigurationException("No record fields specified.");

            LoadNCacheMembers(XmlRecordFieldConfigurations);

            //Validate each record field
            //foreach (var fieldConfig in XmlRecordFieldConfigurations)
            //    fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = XmlRecordFieldConfigurations.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(String.Join(",", dupFields)));

            RecordFieldConfigurationsDict = XmlRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);
        }

        internal string GetNameWithNamespace(XName name)
        {
            ChoXmlNamespaceManager nsMgr = new ChoXmlNamespaceManager(NamespaceManager);

            if (!name.NamespaceName.IsNullOrWhiteSpace())
            {
                string prefix = nsMgr.GetPrefixOfNamespace(name.NamespaceName);
                if (prefix.IsNullOrWhiteSpace()) return name.LocalName;

                return prefix + ":" + name.LocalName;
            }
            else
                return name.LocalName;
        }
    }

    public class ChoXmlNamespaceManager
    {
        public readonly IDictionary<string, string> NSDict;

        public ChoXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            NSDict = nsMgr.GetNamespacesInScope(XmlNamespaceScope.All);
        }

        public string GetPrefixOfNamespace(string ns)
        {
            return NSDict.Where(kvp => kvp.Value == ns).Select(kvp => kvp.Key).FirstOrDefault();
        }

        public string GetNamespaceForPrefix(string prefix)
        {
            if (NSDict.ContainsKey(prefix))
                return NSDict[prefix];
            else
                return null;
        }
    }
}
