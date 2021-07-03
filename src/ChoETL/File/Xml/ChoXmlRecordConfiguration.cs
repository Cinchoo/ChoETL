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
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChoETL
{
    [DataContract]
    public class ChoXmlRecordConfiguration : ChoFileRecordConfiguration
    {
        internal readonly Lazy<ChoXmlNamespaceManager> XmlNamespaceManager;

        public bool TurnOffAutoCorrectXNames
        {
            get;
            set;
        }

        public bool DoNotEmitXmlNamespace
        {
            get;
            set;
        }

        public bool TurnOffXmlFormatting
        {
            get;
            set;
        }
        public bool? TurnOffPluralization
        {
            get;
            set;
        }

        [DataMember]
        public List<ChoXmlRecordFieldConfiguration> XmlRecordFieldConfigurations
        {
            get;
            private set;
        }
        public bool AllowComplexXmlPath
        {
            get;
            set;
        }
        [DataMember]
        public string XPath
        {
            get;
            set;
        }
        [DataMember]
        public int Indent
        {
            get;
            set;
        }
        [DataMember]
        public char IndentChar
        {
            get;
            set;
        }
        public XmlNamespaceManager NamespaceManager
        {
            get;
            set;
        }
        public bool EmitDataType
        {
            get;
            set;
        }
        [DataMember]
        public System.Xml.Formatting Formatting
        {
            get;
            set;
        }
        public XmlSerializer XmlSerializer
        {
            get;
            set;
        }
        [DataMember]
        public bool UseXmlSerialization
        {
            get;
            set;
        }
        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }
        public string XmlEncoding { get; set; }
        public string XmlVersion { get; set; }
        [DataMember]
        public bool OmitXmlDeclaration { get; set; }
        public bool OmitXsiNamespace { get; set; }
        internal Dictionary<string, ChoXmlRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        [DataMember]
        public string XmlSchemaNamespace { get; set; }
        [DataMember]
        public string JSONSchemaNamespace { get; set; }
        [DataMember]
        public ChoEmptyXmlNodeValueHandling EmptyXmlNodeValueHandling { get; set; }

        private Func<XElement, XElement> _customNodeSelecter = null;
        public Func<XElement, XElement> CustomNodeSelecter
        {
            get { return _customNodeSelecter; }
            set { if (value == null) return; _customNodeSelecter = value; }
        }

        private bool _ignoreCase = true;
        [DataMember]
        public bool IgnoreCase
        {
            get { return _ignoreCase; }
            set
            {
                _ignoreCase = value;
                StringComparer = StringComparer.Create(Culture == null ? CultureInfo.CurrentCulture : Culture, IgnoreCase);
            }
        }
        internal string[] DocumentElements
        {
            get;
            set;
        }
        [DataMember]
        public string RootName
        {
            get;
            set;
        }
        [DataMember]
        public string NodeName
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreNodeName
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreRootName
        {
            get;
            set;
        }
        [DataMember]
        public bool RetainAsXmlAwareObjects { get; set; }
        public bool IncludeSchemaInstanceNodes { get; set; }
        internal StringComparer StringComparer
        {
            get;
            private set;
        }
        [DataMember]
        internal bool RetainXmlAttributesAsNative { get; set; }

        private string _defaultNamespacePrefix;
        [DataMember]
        public string DefaultNamespacePrefix
        {
            get { return _defaultNamespacePrefix; }
            set
            {
                _defaultNamespacePrefix = value;
            }
        }
        internal XNamespace NS
        {
            get
            {
                var nsm = XmlNamespaceManager.Value;
                return nsm == null ? null : nsm.GetNamespaceForPrefix(DefaultNamespacePrefix);
            }
        }
        internal bool FlatToNestedObjectSupport
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in XmlRecordFieldConfigurations)
                    yield return fc;
            }
        }

        internal ChoXmlNamespaceTable XmlNamespaceTable { get; set; }
        public bool UseXmlArray { get; set; }

        public readonly dynamic Context = new ChoDynamicObject();

        internal bool IsComplexXPathUsed = true;
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
            XmlNamespaceManager = new Lazy<ChoXmlNamespaceManager>(() => NamespaceManager == null ? null : new ChoXmlNamespaceManager(NamespaceManager));

            XmlRecordFieldConfigurations = new List<ChoXmlRecordFieldConfiguration>();

            Formatting = Formatting.Indented;
            XmlVersion = "1.0";
            OmitXmlDeclaration = true;
            Indent = 2;
            IndentChar = ' ';
            IgnoreCase = true;
            NullValueHandling = ChoNullValueHandling.Empty;
            NamespaceManager = new XmlNamespaceManager(new NameTable());
            if (recordType != null)
            {
                Init(recordType);
            }

            if (XPath.IsNullOrEmpty())
            {
                //XPath = "//*";
            }
        }

        protected override void Clone(ChoRecordConfiguration config)
        {
            base.Clone(config);
            if (!(config is ChoXmlRecordConfiguration))
                return;

            ChoXmlRecordConfiguration xconfig = config as ChoXmlRecordConfiguration;

            xconfig.Indent = Indent;
            xconfig.IndentChar = IndentChar;
            xconfig.NamespaceManager = NamespaceManager;
            xconfig.XmlSerializer = XmlSerializer;
            xconfig.NullValueHandling = NullValueHandling;
            xconfig.IgnoreCase = IgnoreCase;
        }

        public IDictionary<string, string> GetXmlNamespacesInScope()
        {
            return XmlNamespaceTable != null ? XmlNamespaceTable.NamespaceTable : null;
        }

        public ChoXmlRecordConfiguration Clone(Type recordType = null)
        {
            ChoXmlRecordConfiguration config = new ChoXmlRecordConfiguration(recordType);
            Clone(config);

            return config;
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoXmlRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoXmlRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                XPath = recObjAttr.XPath;
            }

            if (XmlRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType);
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

        public ChoXmlRecordConfiguration MapRecordFields<T>()
        {
            MapRecordFields(typeof(T));
            return this;
        }

        public ChoXmlRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault());
            foreach (var rt in recordTypes.Where(rt => rt != null).Skip(1))
                DiscoverRecordFields(rt, false);
            return this;
        }

        private void DiscoverRecordFields(Type recordType, bool clear = true)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (clear)
                XmlRecordFieldConfigurations.Clear();
            DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any());
        }

        private void DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false)
        {
            if (recordType == null)
                return;
            if (!recordType.IsDynamicType())
            {
                IsComplexXPathUsed = false;
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        var fa = pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().FirstOrDefault();
                        bool optIn1 = fa == null || fa.UseXmlSerialization ? optIn : ChoTypeDescriptor.GetProperties(pt).Where(pd1 => pd1.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any();
                        if (false) //optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn1);
                        }
                        else if (pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any())
                        {
                            bool useCache = true;
                            string xPath = null;
                            ChoXmlNodeRecordFieldAttribute attr = ChoTypeDescriptor.GetPropetyAttribute<ChoXmlNodeRecordFieldAttribute>(pd);
                            if (attr.XPath.IsNullOrEmpty())
                            {
                                if (!attr.FieldName.IsNullOrWhiteSpace())
                                {
                                    attr.XPath = $"/{attr.FieldName}|/@{attr.FieldName}";
                                }
                                else
                                    attr.XPath = xPath = $"/{pd.Name}|/@{pd.Name}";
                                IsComplexXPathUsed = true;
                            }
                            else
                                useCache = false;

                            var obj = new ChoXmlRecordFieldConfiguration(pd.Name, attr, pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            obj.UseCache = useCache;
                            if (obj.XPath.IsNullOrWhiteSpace())
                            {
                                if (!obj.FieldName.IsNullOrWhiteSpace())
                                    obj.XPath = $"/{obj.FieldName}|/@{obj.FieldName}";
                                else
                                    obj.XPath = $"/{obj.Name}|/@{obj.Name}";
                            }

                            obj.FieldType = pd.PropertyType.GetUnderlyingType();
                            if (!XmlRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                XmlRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
                else
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        XmlIgnoreAttribute xiAttr = pd.Attributes.OfType<XmlIgnoreAttribute>().FirstOrDefault();
                        if (xiAttr != null)
                            continue;

                        pt = pd.PropertyType.GetUnderlyingType();
                        if (false) //pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                        }
                        else
                        {
                            var obj = new ChoXmlRecordFieldConfiguration(pd.Name, $"/{pd.Name}|/@{pd.Name}");
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;

                            XmlElementAttribute xAttr = pd.Attributes.OfType<XmlElementAttribute>().FirstOrDefault();
                            if (xAttr != null && !xAttr.ElementName.IsNullOrWhiteSpace())
                            {
                                obj.FieldName = xAttr.ElementName;
                            }
                            else
                            {
                                XmlAttributeAttribute xaAttr = pd.Attributes.OfType<XmlAttributeAttribute>().FirstOrDefault();
                                if (xAttr != null && !xaAttr.AttributeName.IsNullOrWhiteSpace())
                                {
                                    obj.FieldName = xaAttr.AttributeName;
                                }
                                else
                                {
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
                            if (!XmlRecordFieldConfigurations.Any(c => c.Name == pd.Name))
                                XmlRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            //if (XPath.IsNull())
            //    throw new ChoRecordConfigurationException("XPath can't be null or whitespace.");

            if (XPath.IsNullOrWhiteSpace())
            {
                if (!IsDynamicObject && (RecordType.IsGenericType && RecordType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)))
                {
                    NodeName = NodeName.IsNullOrWhiteSpace() ? "KeyValuePair" : NodeName;
                    RootName = RootName.IsNullOrWhiteSpace() ? "KeyValuePairs" : RootName;
                }
                else if (!IsDynamicObject && !typeof(IChoScalarObject).IsAssignableFrom(RecordType))
                {
                    NodeName = NodeName.IsNullOrWhiteSpace() ? RecordType.Name : NodeName;
                    RootName = RootName.IsNullOrWhiteSpace() ? NodeName.ToPlural() : RootName;
                }
            }
            else
            {
                if (ChoXmlNodeEx.IsSimpleXmlPath(XPath))
                {
                    var t1 = XPath.SplitNTrim("/").Where(t => !t.IsNullOrWhiteSpace() && t.NTrim() != "." && t.NTrim() != ".." && t.NTrim() != "*").ToArray();
                    if (RootName.IsNullOrWhiteSpace())
                    {
                        if (t1.Length >= 2)
                            RootName = t1.Skip(t1.Length - 2).FirstOrDefault();
                    }
                    NodeName = NodeName.IsNullOrWhiteSpace() ? t1.LastOrDefault() : NodeName;
                    if (t1.Length > 2)
                    {
                        DocumentElements = t1.Reverse().Skip(2).Reverse().ToArray();
                    }
                }
            }

            string rootName = null;
            string nodeName = null;
            ChoXmlDocumentRootAttribute da = TypeDescriptor.GetAttributes(RecordType).OfType<ChoXmlDocumentRootAttribute>().FirstOrDefault();
            if (da != null)
                rootName = da.Name;
            else
            {
                XmlRootAttribute ra = TypeDescriptor.GetAttributes(RecordType).OfType<XmlRootAttribute>().FirstOrDefault();
                if (ra != null)
                    nodeName = ra.ElementName;
            }

            RootName = RootName.IsNullOrWhiteSpace() && !rootName.IsNullOrWhiteSpace() ? rootName : RootName;
            NodeName = NodeName.IsNullOrWhiteSpace() && !nodeName.IsNullOrWhiteSpace() ? nodeName : NodeName;

            RootName = RootName.IsNullOrWhiteSpace() && !NodeName.IsNullOrWhiteSpace() ? NodeName.ToPlural() : RootName;
            if (!RootName.IsNullOrWhiteSpace() && RootName.ToSingular() != RootName)
                NodeName = NodeName.IsNullOrWhiteSpace() && !RootName.IsNullOrWhiteSpace() ? RootName.ToSingular() : NodeName;

            if (RootName.IsNullOrWhiteSpace())
                RootName = "Root";
            if (NodeName.IsNullOrWhiteSpace())
                NodeName = "XElement";

            //Encode Root and node names
            RootName = System.Net.WebUtility.HtmlEncode(RootName);
            NodeName = System.Net.WebUtility.HtmlEncode(NodeName);

            string[] fieldNames = null;
            XElement xpr = null;
            if (state is Tuple<long, XElement>)
                xpr = ((Tuple<long, XElement>)state).Item2;
            else
                fieldNames = state as string[];

            if (AutoDiscoverColumns
                && XmlRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject
                    && ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().Any()).Any())
                {
                    DiscoverRecordFields(RecordType);
                }
                else if (xpr != null)
                {
                    XmlRecordFieldConfigurations.AddRange(DiscoverRecordFieldsFromXElement(xpr));
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        if (fn.StartsWith("_"))
                        {
                            string fn1 = fn.Substring(1);
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: $"./{fn1}");
                            obj.FieldName = fn1;
                            obj.IsXmlAttribute = true;
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                        else if (fn.EndsWith("_"))
                        {
                            string fn1 = fn.Substring(0, fn.Length - 1);
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: $"./{fn1}");
                            obj.FieldName = fn1;
                            obj.IsXmlCDATA = true;
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                        else
                        {
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: $"./{fn}");
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
            }
            else
            {
                IsComplexXPathUsed = false;

                foreach (var fc in XmlRecordFieldConfigurations)
                {
                    if (fc.IsArray == null)
                        fc.IsArray = typeof(ICollection).IsAssignableFrom(fc.FieldType);

                    if (fc.FieldName.IsNullOrWhiteSpace())
                        fc.FieldName = fc.Name;

                    if (fc.XPath.IsNullOrWhiteSpace())
                        fc.XPath = $"/{fc.FieldName}|/@{fc.FieldName}";
                    else
                    {
                        if (fc.XPath == fc.FieldName
                            || fc.XPath == $"/{fc.FieldName}" || fc.XPath == $"/{fc.FieldName}" || fc.XPath == $"/{fc.FieldName}"
                            || fc.XPath == $"/@{fc.FieldName}" || fc.XPath == $"/@{fc.FieldName}" || fc.XPath == $"/@{fc.FieldName}"
                            )
                        {

                        }
                        else
                        {
                            IsComplexXPathUsed = true;
                            fc.UseCache = false;
                        }
                    }
                }
            }

            //if (XmlRecordFieldConfigurations.Count <= 0)
            //    throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            foreach (var fieldConfig in XmlRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = XmlRecordFieldConfigurations.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(String.Join(",", dupFields)));

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in XmlRecordFieldConfigurations)
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

            RecordFieldConfigurationsDict = XmlRecordFieldConfigurations.OrderBy(c => c.IsXmlAttribute).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            if (XmlRecordFieldConfigurations.Where(e => e.IsNullable.CastTo<bool>()).Any()
                || NullValueHandling == ChoNullValueHandling.Default)
            {
                if (NamespaceManager != null)
                {
                    if (XmlNamespaceManager != null)
                    {
                        foreach (var kvp in XmlNamespaceManager.Value.NSDict)
                        {
                            NamespaceManager.AddNamespace(kvp.Key, kvp.Value);
                        }
                    }
                    if (!NamespaceManager.HasNamespace("xsi"))
                        NamespaceManager.AddNamespace("xsi", ChoXmlSettings.XmlSchemaInstanceNamespace);
                    if (!NamespaceManager.HasNamespace("xsd"))
                        NamespaceManager.AddNamespace("xsd", ChoXmlSettings.XmlSchemaNamespace);
                }
            }

            LoadNCacheMembers(XmlRecordFieldConfigurations);
        }

        internal ChoXmlRecordFieldConfiguration[] DiscoverRecordFieldsFromXElement(XElement xpr)
        {
            IsComplexXPathUsed = false;
            ChoXmlNamespaceManager nsMgr = XmlNamespaceManager.Value;

            Dictionary<string, ChoXmlRecordFieldConfiguration> dict = new Dictionary<string, ChoXmlRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
            string name = null;
            foreach (var attr in xpr.Attributes())
            {
                if (!attr.IsValidAttribute(XmlSchemaNamespace, JSONSchemaNamespace, nsMgr, IncludeSchemaInstanceNodes))
                    continue;

                //if (!IsInNamespace(xpr.Name, attr.Name))
                //    continue;

                //if (!attr.Name.NamespaceName.IsNullOrWhiteSpace()) continue;

                name = GetNameWithNamespace(xpr.Name, attr.Name);

                if (name.IsValidXNode(DefaultNamespacePrefix))
                {
                    if (!dict.ContainsKey(name))
                        dict.Add(name, new ChoXmlRecordFieldConfiguration(attr.Name.LocalName, $"/@{name}") { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//@{name}" : $"//@{DefaultNamespace}" + ":" + $"{name}") { IsXmlAttribute = true });
                    else
                    {
                        throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                    }
                }
            }

            bool hasElements = false;
            //var z = xpr.Elements().ToArray();
            XElement ele = null;
            foreach (var kvp in xpr.Elements().GroupBy(e => e.Name.LocalName).Select(g => new { Name = g.Key, Value = g.ToArray() }))
            {
                if (kvp.Value.Length == 1)
                {
                    ele = kvp.Value.First();
                    if (!IsInNamespace(ele.Name))
                        continue;

                    name = GetNameWithNamespace(ele.Name);

                    if (name.IsValidXNode(DefaultNamespacePrefix))
                    {
                        hasElements = true;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(ele.Name.LocalName, $"/{name}") { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
                        else
                        {
                            if (dict[name].IsXmlAttribute)
                                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));

                            dict[name].IsArray = true;
                        }
                    }
                }
                else if (kvp.Value.Length > 1)
                {
                    ele = kvp.Value.First();
                    if (!IsInNamespace(ele.Name))
                        continue;

                    name = GetNameWithNamespace(ele.Name);

                    if (name.IsValidXNode(DefaultNamespacePrefix))
                    {
                        hasElements = true;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(xpr.Name.LocalName, $"/{name}") { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
                        else
                        {
                            if (dict[name].IsXmlAttribute)
                                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));

                            dict[name].IsArray = true;
                        }
                    }
                }
            }

            //foreach (var ele in xpr.Elements())
            //{
            //    if (!IsInNamespace(ele.Name))
            //        continue;

            //    name = GetNameWithNamespace(ele.Name);

            //    hasElements = true;
            //    if (!dict.ContainsKey(name))
            //        dict.Add(name, new ChoXmlRecordFieldConfiguration(ele.Name.LocalName, $"/{name}") { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
            //    else
            //    {
            //        if (dict[name].IsXmlAttribute)
            //            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));

            //        dict[name].IsArray = true;
            //    }
            //}

            if (!hasElements)
            {
                if (IsInNamespace(xpr.Name))
                {
                    //name = xpr.Name.LocalName;
                    name = GetNameWithNamespace(xpr.Name);
                    dict.Add(name, new ChoXmlRecordFieldConfiguration(name, "text()") { FieldName = name });
                }
            }

            return dict.Values.ToArray();
        }

        internal string GetNameWithNamespace(XName name)
        {
            return XmlNamespaceManager.Value.GetNameWithNamespace(name);
        }

        internal string GetNameWithNamespace(XName name, XName propName)
        {
            return XmlNamespaceManager.Value.GetNameWithNamespace(name, propName);
        }

        internal bool IsInNamespace(XName name)
        {
            return XmlNamespaceManager.Value.IsInNamespace(name);
        }

        internal bool IsInNamespace(XName name, XName propName)
        {
            return XmlNamespaceManager.Value.IsInNamespace(name, propName);
        }

        public ChoXmlRecordConfiguration Configure(Action<ChoXmlRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoXmlRecordConfiguration ClearFields()
        {
            //XmlRecordFieldConfigurationsForType.Clear();
            XmlRecordFieldConfigurations.Clear();
            return this;
        }

        public ChoXmlRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (XmlRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = XmlRecordFieldConfigurations.Where(f => f.DeclaringMember == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                XmlRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoXmlRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = XmlRecordFieldConfigurations.Where(f => f.DeclaringMember == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                XmlRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoXmlRecordConfiguration WithXPath(string xPath)
        {
            XPath = xPath;
            return this;
        }

        public ChoXmlRecordConfiguration WithXmlNamespace(string prefix, string uri)
        {
            NamespaceManager.AddNamespace(prefix, uri);

            return this;
        }

        public ChoXmlRecordConfiguration WithXmlNamespaces(IDictionary<string, string> ns)
        {
            if (ns != null)
            {
                foreach (var kvp in ns)
                    NamespaceManager.AddNamespace(kvp.Key, kvp.Value);
            }

            return this;
        }

        public ChoXmlRecordConfiguration WithDefaultXmlNamespace(string prefix, string uri)
        {
            WithXmlNamespace(prefix, uri);
            WithDefaultNamespacePrefix(prefix);
            return this;
        }

        public ChoXmlRecordConfiguration WithDefaultNamespacePrefix(string prefix)
        {
            DefaultNamespacePrefix = prefix;

            return this;
        }

        public ChoXmlRecordConfiguration Map(string propertyName, string xPath = null, string fieldName = null, Type fieldType = null)
        {
            Map(propertyName, m => m.XPath(xPath).FieldName(fieldName).FieldType(fieldType));
            return this;
        }

        public ChoXmlRecordConfiguration Map(string propertyName, Action<ChoXmlRecordFieldConfigurationMap> mapper)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoXmlRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoXmlRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string xPath = null, string fieldName = null)
        {
            Map(field, m => m.XPath(xPath).FieldName(fieldName));
            return this;
        }

        public ChoXmlRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoXmlRecordFieldConfigurationMap> mapper)
        {
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoXmlNodeRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray());
            mapper?.Invoke(new ChoXmlRecordFieldConfigurationMap(cf));
            return this;
        }

        internal ChoXmlRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoXmlNodeRecordFieldAttribute attr = null, Attribute[] otherAttrs = null)
        {
            if (!XmlRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(propertyName, attr, otherAttrs));

            return XmlRecordFieldConfigurations.First(fc => fc.Name == propertyName);
        }

        internal ChoXmlRecordFieldConfiguration GetFieldConfiguration(string propertyName)
        {
            propertyName = propertyName.NTrim();
            if (!XmlRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(propertyName, $"/{propertyName}"));

            return XmlRecordFieldConfigurations.First(fc => fc.Name == propertyName);
        }

        internal bool IsTurnOffPluralization(ChoXmlRecordFieldConfiguration fc)
        {
            if (fc.TurnOffPluralization == null)
            {
                if (TurnOffPluralization != null)
                    return TurnOffPluralization.Value;
            }
            else
                return fc.TurnOffPluralization.Value;

            return false;
        }
    }

    public class ChoXmlRecordConfiguration<T> : ChoXmlRecordConfiguration
    {
        public ChoXmlRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoXmlRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoXmlRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoXmlRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string xPath = null, string fieldName = null)
        {
            base.Map(field, xPath, fieldName);
            return this;
        }

        public ChoXmlRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, Action<ChoXmlRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoXmlRecordConfiguration<T> Configure(Action<ChoXmlRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoXmlRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoXmlRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }
    }

    public class ChoXmlNamespaceManager
    {
        public readonly IDictionary<string, string> NSDict;
        public readonly XmlNamespaceManager NSMgr;
        public readonly XmlSerializerNamespaces XmlSerializerNamespaces;

        public ChoXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            NSMgr = nsMgr;
            NSDict = nsMgr.GetNamespacesInScope(XmlNamespaceScope.All);

            XmlSerializerNamespaces = new XmlSerializerNamespaces();
            foreach (var kvp in NSDict)
                XmlSerializerNamespaces.Add(kvp.Key, kvp.Value);
        }

        public string GetPrefixOfNamespace(string ns)
        {
            return NSDict.Where(Xml => Xml.Value == ns && !Xml.Key.IsNullOrWhiteSpace()).Select(Xml => Xml.Key).FirstOrDefault();
        }

        public XNamespace GetNamespaceForPrefix(string prefix)
        {
            if (prefix != null && NSDict.ContainsKey(prefix))
                return NSDict[prefix];
            else
                return null;
        }

        public string GetFirstDefaultNamespace()
        {
            return NSDict.Where(kvp => kvp.Key != "xml").Select(kvp => kvp.Value).FirstOrDefault();
        }

        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();

            if (NSDict != null)
            {
                foreach (var kvp in NSDict)
                {
                    msg.AppendFormat(@" xmlns:{0}=""{1}""", kvp.Key, kvp.Value);
                }
            }

            return msg.ToString();
        }

        public string ToString(ChoXmlRecordConfiguration config)
        {
            if (config == null)
                return ToString();

            StringBuilder msg = new StringBuilder();

            if (NSDict != null)
            {
                foreach (var kvp in NSDict.Where(kvp1 => kvp1.Key.IsNullOrWhiteSpace()))
                {
                    msg.AppendFormat(@" xmlns=""{0}""", kvp.Value);
                    break;
                }

                foreach (var kvp in NSDict.Where(kvp1 => !kvp1.Key.IsNullOrWhiteSpace()))
                {
                    if (!config.DoNotEmitXmlNamespace && kvp.Key == "xml")
                        continue;
                    if (!config.OmitXsiNamespace && kvp.Key == "xsi")
                        continue;

                    if (kvp.Key.Contains(":"))
                    {
                        msg.AppendFormat(@" {0}=""{1}""", kvp.Key, kvp.Value);
                    }
                    else if (String.Compare(kvp.Key, "xmlns", true) == 0)
                        msg.AppendFormat(@" xmlns=""{1}""", kvp.Key, kvp.Value);
                    else
                        msg.AppendFormat(@" xmlns:{0}=""{1}""", kvp.Key, kvp.Value);
                }
            }

            if (config.UseXmlSerialization)
            {
                if (!msg.ToString().Contains("xmlns:xsi"))
                {
                    if (!config.OmitXsiNamespace)
                        msg.AppendFormat(@" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""");
                }
                else
                {
                }
            }

            return msg.ToString();
        }

    }
}
