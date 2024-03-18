using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
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
        internal Dictionary<Type, Dictionary<string, ChoXmlRecordFieldConfiguration>> XmlRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoXmlRecordFieldConfiguration>>();
        internal Dictionary<Type, Func<object, object>> NodeConvertersForType = new Dictionary<Type, Func<object, object>>();
        internal ChoResetLazy<ChoXmlNamespaceManager> XmlNamespaceManager;
        public Func<string, object, bool?> XmlArrayQualifier
        {
            get;
            set;
        }

        public bool? UseProxy
        {
            get;
            set;
        }

        public string AttributeFieldPrefixes
        {
            get;
            set;
        }
        public string CDATAFieldPostfixes
        {
            get;
            set;
        }
        public string CDATAFieldPrefixes
        {
            get;
            set;
        }

        public bool FlattenNode
        {
            get;
            set;
        }

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
        public bool AllowComplexXPath
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
        //private string _xmlSchemaInstanceNamespace = null;
        //[DataMember]
        //public string XmlSchemaInstanceNamespace
        //{
        //    get { return _xmlSchemaInstanceNamespace.IsNullOrWhiteSpace() ? ChoXmlSettings.XmlSchemaInstanceNamespace : _xmlSchemaInstanceNamespace; }
        //    set { _xmlSchemaInstanceNamespace = value; }
        //}
        private string _xmlSchemaNamespace = null;
        [DataMember]
        public string XmlSchemaNamespace
        {
            get { return _xmlSchemaNamespace.IsNullOrWhiteSpace() ? ChoXmlSettings.XmlSchemaNamespace : _xmlSchemaNamespace; }
            set { _xmlSchemaNamespace = value; }
        }
        private string _JSONSchemaNamespace = null;
        [DataMember]
        public string JSONSchemaNamespace
        {
            get { return _JSONSchemaNamespace.IsNullOrWhiteSpace() ? ChoXmlSettings.JSONSchemaNamespace : _JSONSchemaNamespace; }
            set { _JSONSchemaNamespace = value; }
        }

        [DataMember]
        public ChoEmptyXmlNodeValueHandling EmptyXmlNodeValueHandling { get; set; }

        private Func<XElement, XElement> _customNodeSelecter = null;
        public Func<XElement, XElement> CustomNodeSelector
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
        public bool? UseXmlArray { get; set; }
        public bool UseJsonNamespaceForObjectType { get; set; }
        public XmlNodeEventHandler UnknownNode { get; set; }
        public bool IgnoreNSPrefix { get; set; }
        public bool IncludeAllSchemaNS { get; set; }
        public bool KeepOriginalNodeName { get; set; }
        public bool AutoDiscoverXmlNamespaces { get; set; } = true;

        public ChoXmlRecordFieldConfiguration this[string name]
        {
            get
            {
                return XmlRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }
        public static new int MaxLineSize
        {
            get { throw new NotSupportedException(); }
        }
        //public static new string EOLDelimiter
        //{
        //    get { throw new NotSupportedException(); }
        //}
        public static new string MayContainEOLInData
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool IgnoreEmptyLine
        {
            get { throw new NotSupportedException(); }
        }
        //public static new bool ColumnCountStrict
        //{
        //    get { throw new NotSupportedException(); }
        //}
        //public static new bool ColumnOrderStrict
        //{
        //    get { throw new NotSupportedException(); }
        //}
        public static new bool EscapeQuoteAndDelimiter
        {
            get { throw new NotSupportedException(); }
        }
        public static new string Comment
        {
            get { throw new NotSupportedException(); }
        }
        public static new string[] Comments
        {
            get { throw new NotSupportedException(); }
        }
        public static new bool LiteParsing
        {
            get;
            set;
        }
        public static new bool? QuoteAllFields
        {
            get;
            set;
        }
        public static new bool? QuoteChar
        {
            get;
            set;
        }
        public static new bool? QuoteEscapeChar
        {
            get;
            set;
        }
        public static new bool QuoteLeadingAndTrailingSpaces
        {
            get;
            set;
        }
        public static new bool? MayHaveQuotedFields
        {
            get { return QuoteAllFields; }
            set { QuoteAllFields = value; }
        }

        public ChoXmlRecordConfiguration() : this(null)
        {

        }

        internal ChoXmlRecordConfiguration(Type recordType) : base(recordType)
        {
            XmlNamespaceManager = new ChoResetLazy<ChoXmlNamespaceManager>(() => NamespaceManager == null ? null : new ChoXmlNamespaceManager(NamespaceManager));

            XmlRecordFieldConfigurations = new List<ChoXmlRecordFieldConfiguration>();

            AutoDiscoverColumns = false;
            Formatting = Formatting.Indented;
            XmlVersion = "1.0";
            OmitXmlDeclaration = true;
            OmitXsiNamespace = true;
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

        internal void AddFieldForType(Type rt, ChoXmlRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!XmlRecordFieldConfigurationsForType.ContainsKey(rt))
                XmlRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoXmlRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (XmlRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                XmlRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                XmlRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        internal bool ShouldUseProxy(ChoXmlRecordFieldConfiguration fc)
        {
            if (fc != null)
            {
                if (fc.UseProxy != null)
                    return fc.UseProxy.Value;

            }

            return UseProxy != null ? UseProxy.Value : false;
        }

        protected override bool ContainsRecordConfigForType(Type rt)
        {
            return XmlRecordFieldConfigurationsForType.ContainsKey(rt);
        }

        protected override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return XmlRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        protected override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForTypeInternal(rt))
                return XmlRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }

        protected override void Clone(ChoRecordConfiguration config)
        {
            base.Clone(config);
            if (!(config is ChoXmlRecordConfiguration))
                return;

            ChoXmlRecordConfiguration xconfig = config as ChoXmlRecordConfiguration;

            xconfig.XmlRecordFieldConfigurationsForType = XmlRecordFieldConfigurationsForType;
            xconfig.NodeConvertersForType = NodeConvertersForType;
            xconfig.XmlNamespaceManager = XmlNamespaceManager;
            xconfig.Indent = Indent;
            xconfig.IndentChar = IndentChar;
            xconfig.NamespaceManager = NamespaceManager;
            xconfig.XmlSerializer = XmlSerializer;
            xconfig.NullValueHandling = NullValueHandling;
            xconfig.IgnoreCase = IgnoreCase;
            xconfig.XPath = "//";
            xconfig.RecordType = RecordType;

            xconfig.UseProxy = UseProxy;
            xconfig.AttributeFieldPrefixes = AttributeFieldPrefixes;
            xconfig.CDATAFieldPostfixes = CDATAFieldPostfixes;
            xconfig.CDATAFieldPrefixes = CDATAFieldPrefixes;
            xconfig.FlattenNode = FlattenNode;
            xconfig.TurnOffAutoCorrectXNames = TurnOffAutoCorrectXNames;
            xconfig.DoNotEmitXmlNamespace = DoNotEmitXmlNamespace;
            xconfig.TurnOffXmlFormatting = TurnOffXmlFormatting;
            xconfig.TurnOffPluralization = TurnOffPluralization;
            xconfig.Indent = Indent;
            xconfig.IndentChar = IndentChar;
            xconfig.NamespaceManager = NamespaceManager;
            xconfig.EmitDataType = EmitDataType;
            xconfig.Formatting = Formatting;
            xconfig.UseXmlSerialization = UseXmlSerialization;
            xconfig.AreAllFieldTypesNull = AreAllFieldTypesNull;
            xconfig.XmlEncoding = XmlEncoding;
            xconfig.XmlVersion = XmlVersion;
            xconfig.OmitXmlDeclaration = OmitXmlDeclaration;
            xconfig.OmitXsiNamespace = OmitXsiNamespace;
            xconfig.XmlSchemaNamespace = XmlSchemaNamespace;
            xconfig.JSONSchemaNamespace = JSONSchemaNamespace;
            xconfig.EmptyXmlNodeValueHandling = EmptyXmlNodeValueHandling;
            xconfig.CustomNodeSelector = CustomNodeSelector;
            xconfig.IgnoreCase = IgnoreCase;

            xconfig.RetainAsXmlAwareObjects = RetainAsXmlAwareObjects;
            xconfig.IncludeSchemaInstanceNodes = IncludeSchemaInstanceNodes;
            xconfig.DefaultNamespacePrefix = DefaultNamespacePrefix;

            xconfig.UseXmlArray = UseXmlArray;
            xconfig.UseJsonNamespaceForObjectType = UseJsonNamespaceForObjectType;
            xconfig.DefaultNamespacePrefix = DefaultNamespacePrefix;
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

            var pd = ChoTypeDescriptor.GetTypeAttribute<ChoXPathAttribute>(recordType);
            if (pd != null)
            {
                XPath = pd.XPath;
                AllowComplexXPath = pd.AllowComplexXPath;
            }
            var up = ChoTypeDescriptor.GetTypeAttribute<ChoUseXmlProxyAttribute>(recordType);
            if (up != null)
            {
                UseProxy = up.Flag;
            }

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
        public void MapRecordFields()
        {
            RecordType = DiscoverRecordFields(RecordType, false, null, true);
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

        private Type DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoXmlRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = XmlRecordFieldConfigurations;

            if (clear)
                XmlRecordFieldConfigurations.Clear();
            return DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().Any()).Any(), recordFieldConfigurations, isTop);
        }

        private Type DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false,
            List<ChoXmlRecordFieldConfiguration> recordFieldConfigurations = null, bool isTop = false)
        {
            if (recordType == null)
                return recordType;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) //ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        var fa = pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().FirstOrDefault();
                        bool optIn1 = fa == null || fa.UseXmlSerialization ? optIn : ChoTypeDescriptor.GetProperties(pt).Where(pd1 => pd1.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().Any()).Any();
                        //if (false) //optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                        if (optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn1, recordFieldConfigurations, false);
                        }
                        else if (pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoXmlRecordFieldConfiguration(pd.Name, pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                    recordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
                else
                {
                    if (isTop)
                    {
                        if (typeof(IList).IsAssignableFrom(recordType) || (recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>)))
                        {
                            throw new ChoParserException("Record type not supported.");
                        }
                        else if (typeof(IDictionary<string, object>).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                        else if (typeof(IDictionary).IsAssignableFrom(recordType))
                        {
                            recordType = typeof(ExpandoObject);
                            return recordType;
                        }
                    }

                    if (recordType.IsSimple())
                    {
                        var obj = new ChoXmlRecordFieldConfiguration("Value", "//Value");
                        obj.FieldType = recordType;

                        recordFieldConfigurations.Add(obj);
                        return recordType;
                    }

                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        XmlIgnoreAttribute xiAttr = pd.Attributes.OfType<XmlIgnoreAttribute>().FirstOrDefault();
                        if (xiAttr != null)
                            continue;

                        pt = pd.PropertyType.GetUnderlyingType();
                        //if (false) //pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                        if ((pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                            /*|| (pt != typeof(object) && !pt.IsSimple() && !ChoTypeDescriptor.HasTypeConverters(pd.GetPropertyInfo()))*/)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, recordFieldConfigurations, false);
                        }
                        else
                        {
                            //var obj = new ChoXmlRecordFieldConfiguration(pd.Name, null/*$"/{pd.Name}|/@{pd.Name}"*/);
                            var obj = new ChoXmlRecordFieldConfiguration(pd.Name, ChoTypeDescriptor.GetPropetyAttribute<ChoXmlNodeRecordFieldAttribute>(pd),
                                pd.Attributes.OfType<Attribute>().ToArray());

                            obj.FieldType = pt;
                            obj.PropertyDescriptorInternal = pd;
                            obj.DeclaringMemberInternal = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;
                            ChoUseXmlSerializationAttribute sAttr = pd.Attributes.OfType<ChoUseXmlSerializationAttribute>().FirstOrDefault();
                            if (sAttr != null)
                                obj.UseXmlSerialization = sAttr.Flag;
                            ChoXPathAttribute xpAttr = pd.Attributes.OfType<ChoXPathAttribute>().FirstOrDefault();
                            if (xpAttr != null && !xpAttr.XPath.IsNullOrWhiteSpace())
                                obj.XPath = xpAttr.XPath;
                            ChoUseXmlProxyAttribute upAttr = pd.Attributes.OfType<ChoUseXmlProxyAttribute>().FirstOrDefault();
                            if (upAttr != null)
                                obj.UseProxy = upAttr.Flag;

                            XmlElementAttribute xAttr = pd.Attributes.OfType<XmlElementAttribute>().FirstOrDefault();
                            if (xAttr != null && !xAttr.ElementName.IsNullOrWhiteSpace())
                            {
                                obj.FieldName = xAttr.ElementName;
                            }
                            else
                            {
                                XmlAttributeAttribute xaAttr = pd.Attributes.OfType<XmlAttributeAttribute>().FirstOrDefault();
                                if (xaAttr != null && !xaAttr.AttributeName.IsNullOrWhiteSpace())
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

                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                    recordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
            return recordType;
        }

        private string GetDefaultNSPrefix()
        {
            return XmlNamespaceManager.Value.GetNamespacePrefixOrDefault(NamespaceManager.DefaultNamespace, DefaultNamespacePrefix);

            //string nsPrefix = Configuration.XmlNamespaceManager.Value.GetNamespacePrefix(Configuration.NamespaceManager.DefaultNamespace);
            //if (nsPrefix.IsNullOrWhiteSpace())
            //{
            //    nsPrefix = Configuration.DefaultNamespacePrefix;
            //    if (nsPrefix.IsNullOrWhiteSpace())
            //    {
            //        nsPrefix = ChoXmlNamespaceManager.DefaultNSToken;
            //    }
            //}

            //return nsPrefix;
        }

        protected override void Validate(object state)
        {
            base.Validate(state);

            //if (XPath.IsNull())
            //    throw new ChoRecordConfigurationException("XPath can't be null or whitespace.");

            string defaultNS = null;
            if (XPath.IsNullOrWhiteSpace())
            {
                if (!IsDynamicObject && (RecordType.IsGenericType && RecordType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)))
                {
                    NodeName = NodeName.IsNullOrWhiteSpace() ? "KeyValuePair" : NodeName;
                    RootName = RootName.IsNullOrWhiteSpace() ? "KeyValuePairs" : RootName;
                }
                else if (!IsDynamicObject && !typeof(IChoScalarObject).IsAssignableFrom(RecordType))
                {
                    XmlRootAttribute rootAttr = ChoType.GetAttribute<XmlRootAttribute>(RecordType);
                    if (rootAttr != null && NodeName.IsNullOrWhiteSpace())
                    {
                        NodeName = rootAttr.ElementName;
                        var ns = rootAttr.Namespace;
                        if (!ns.IsNullOrWhiteSpace())
                        {
                            defaultNS = ns;
                        }
                    }

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
                {
                    nodeName = ra.ElementName;
                    defaultNS = ra.Namespace;
                }
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

            if (fieldNames != null && XmlRecordFieldConfigurations.Count > 0 && FlattenNode)
                XmlRecordFieldConfigurations.Clear();

            if (AutoDiscoverColumns
                || XmlRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject
                    /*&& ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().Any()).Any()*/)
                {
                    DiscoverRecordFields(RecordType);
                }
                else if (xpr != null)
                {
                    XmlRecordFieldConfigurations.AddRange(DiscoverRecordFieldsFromXElement(xpr));
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    string nsPrefix = !NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace() ? GetDefaultNSPrefix() : null;
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        if (AttributeFieldPrefixes != null && AttributeFieldPrefixes.Select(c => fn.StartsWith(c.ToString())).Any())
                        {
                            string fn1 = fn.Substring(1);
                            if (!DefaultNamespacePrefix.IsNullOrWhiteSpace())
                                fn1 = $"{DefaultNamespacePrefix}:{fn1}";

                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: GetXPath(nsPrefix, fn1) /*$"./{fn1}"*/);
                            obj.FieldName = fn1;
                            obj.IsXmlAttribute = true;
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                        else if (CDATAFieldPrefixes != null && CDATAFieldPrefixes.Select(c => fn.StartsWith(c.ToString())).Any())
                        {
                            string fn1 = fn.Substring(0, fn.Length - 1);
                            if (!DefaultNamespacePrefix.IsNullOrWhiteSpace())
                                fn1 = $"{DefaultNamespacePrefix}:{fn1}";
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: GetXPath(nsPrefix, fn1) /*$"./{fn1}"*/);
                            obj.FieldName = fn1;
                            obj.IsXmlCDATA = true;
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                        else if (CDATAFieldPostfixes != null && CDATAFieldPostfixes.Select(c => fn.EndsWith(c.ToString())).Any())
                        {
                            string fn1 = fn.Substring(0, fn.Length - 1);
                            if (!DefaultNamespacePrefix.IsNullOrWhiteSpace())
                                fn1 = $"{DefaultNamespacePrefix}:{fn1}";
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: GetXPath(nsPrefix, fn1) /*$"./{fn1}"*/);
                            obj.FieldName = fn1;
                            obj.IsXmlCDATA = true;
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                        else
                        {
                            string fn1 = fn;
                            if (!DefaultNamespacePrefix.IsNullOrWhiteSpace())
                                fn1 = $"{DefaultNamespacePrefix}:{fn1}";
                            var obj = new ChoXmlRecordFieldConfiguration(fn, xPath: GetXPath(nsPrefix, fn1) /*$"./{fn1}"*/);
                            XmlRecordFieldConfigurations.Add(obj);
                        }
                    }
                }
            }
            else
            {
                foreach (var fc in XmlRecordFieldConfigurations)
                {
                    if (fc.IsArray == null)
                    {
                        fc.IsArray = typeof(ICollection).IsAssignableFrom(fc.FieldType);
                    }

                    if (fc.FieldName.IsNullOrWhiteSpace())
                        fc.FieldName = fc.Name;
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

            PIDictInternal = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDictInternal = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in XmlRecordFieldConfigurations)
            {
                var pd1 = fc.DeclaringMemberInternal.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
                    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMemberInternal);
                if (pd1 != null)
                {
                    fc.PropertyDescriptorInternal = pd1;

                    XmlArrayItemAttribute xmlArrayItemAttr = pd1.Attributes.OfType<XmlArrayItemAttribute>().FirstOrDefault();
                    if (xmlArrayItemAttr != null)
                        fc.FieldName = xmlArrayItemAttr.ElementName;

                    ChoXmlArrayAttribute slAttr = pd1.Attributes.OfType<ChoXmlArrayAttribute>().FirstOrDefault();
                    if (slAttr != null)
                    {
                        fc.IsArray = slAttr.Flag;
                        fc.ArrayNodeName = slAttr.ArrayNodeName;
                    }
                    else
                    {
                        XmlArrayAttribute slAttr1 = pd1.Attributes.OfType<XmlArrayAttribute>().FirstOrDefault();
                        if (slAttr1 != null)
                        {
                            fc.IsArray = true;
                            fc.ArrayNodeName = slAttr1.ElementName;
                            fc.ArrayNodeNamespace = slAttr1.Namespace;
                        }
                        else
                            fc.IsArray = false;
                    }

                    if (fc.IsArray.CastTo<bool>() && fc.ArrayNodeName.IsNullOrWhiteSpace())
                    {
                        if (pd1 != null)
                        {
                            var itemType = pd1.PropertyType.GetItemType();

                            XmlRootAttribute rootAttr = itemType.GetCustomAttributes(false).OfType<XmlRootAttribute>().FirstOrDefault();
                            if (rootAttr != null)
                            {
                                fc.ArrayNodeName = rootAttr.ElementName;
                                fc.ArrayNodeNamespace = rootAttr.Namespace;
                            }
                        }
                    }
                }

                if (fc.PropertyDescriptorInternal == null)
                    fc.PropertyDescriptorInternal = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptorInternal == null)
                    continue;

                PIDictInternal.Add(fc.Name, fc.PropertyDescriptorInternal.ComponentType.GetProperty(fc.PropertyDescriptorInternal.Name));
                PDDictInternal.Add(fc.Name, fc.PropertyDescriptorInternal);
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

            if (DefaultNamespacePrefix.IsNullOrWhiteSpace() && !defaultNS.IsNullOrWhiteSpace())
            {
                if (XmlNamespaceManager != null)
                {
                    var nsPrefix = XmlNamespaceManager.Value.GetNamespacePrefix(defaultNS);
                    if (!nsPrefix.IsNullOrWhiteSpace())
                        DefaultNamespacePrefix = nsPrefix;
                }

            }

            LoadNCacheMembers(XmlRecordFieldConfigurations);
        }
        internal string GetXPath(string nsPrefix, string fieldName, bool isAttribute = false)
        {
            string xpath = null;

            if (nsPrefix.IsNullOrWhiteSpace())
            {
                if (isAttribute)
                    xpath = $"@{fieldName}";
                else
                    xpath = $"{fieldName}|/{fieldName}|@{fieldName}";
            }
            else
            {
                if (fieldName.Contains(":"))
                {
                    if (isAttribute)
                        xpath = $"@{fieldName}";
                    else
                        xpath = $"/{fieldName}|{fieldName}|@{fieldName}";
                }
                else
                {
                    if (isAttribute)
                        xpath = $"@{fieldName}|{nsPrefix}:{fieldName}";
                    else
                        xpath = $"/{fieldName}|{fieldName}|@{fieldName}|/{nsPrefix}:{fieldName}|{nsPrefix}:{fieldName}";
                }
            }
            return xpath;
        }
        internal ChoXmlRecordFieldConfiguration[] DiscoverRecordFieldsFromXElement(XElement xpr)
        {
            ChoXmlNamespaceManager nsMgr = XmlNamespaceManager.Value;
            string nsPrefix = !NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace() ? GetDefaultNSPrefix() : null;

            Dictionary<string, ChoXmlRecordFieldConfiguration> dict = new Dictionary<string, ChoXmlRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
            string name = null;
            foreach (var attr in xpr.Attributes())
            {
                if (!attr.IsValidAttribute(XmlSchemaNamespace, XmlSchemaNamespace, nsMgr, IncludeSchemaInstanceNodes))
                    continue;

                //if (!IsInNamespace(xpr.Name, attr.Name))
                //    continue;

                //if (!attr.Name.NamespaceName.IsNullOrWhiteSpace()) continue;

                name = GetNameWithNamespace(xpr.Name, attr.Name);

                if (name.IsValidXNode(DefaultNamespacePrefix))
                {
                    if (!dict.ContainsKey(name))
                        dict.Add(name, new ChoXmlRecordFieldConfiguration(attr.Name.LocalName, GetXPath(nsPrefix, name, true)) { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//@{name}" : $"//@{DefaultNamespace}" + ":" + $"{name}") { IsXmlAttribute = true });
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

                    var NSName = GetNameWithNamespace(ele.Name);
                    name = IgnoreNSPrefix ? ele.Name.LocalName : GetNameWithNamespace(ele.Name);

                    if (name.IsValidXNode(DefaultNamespacePrefix))
                    {
                        hasElements = true;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(ele.Name.LocalName, GetXPath(nsPrefix, NSName)) { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
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

                    var NSName = GetNameWithNamespace(ele.Name);
                    name = IgnoreNSPrefix ? ele.Name.LocalName : GetNameWithNamespace(ele.Name);

                    if (name.IsValidXNode(DefaultNamespacePrefix))
                    {
                        hasElements = true;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoXmlRecordFieldConfiguration(ele.Name.LocalName, GetXPath(nsPrefix, NSName)) { FieldName = name }); // DefaultNamespace.IsNullOrWhiteSpace() ? $"//{name}" : $"//{DefaultNamespace}" + ":" + $"{name}"));
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
                    //name = GetNameWithNamespace(xpr.Name);
                    name = IgnoreNSPrefix ? xpr.Name.LocalName : GetNameWithNamespace(xpr.Name);
                    dict.Add(name, new ChoXmlRecordFieldConfiguration(name, "text()") { FieldName = name });
                }
            }

            return dict.Values.ToArray();
        }

        public ChoXmlRecordConfiguration RegisterNodConverterForType<ModelType>(Func<object, object> selector)
        {
            return RegisterNodeConverterForType(typeof(ModelType), selector);
        }

        public ChoXmlRecordConfiguration RegisterNodeConverterForType(Type type, Func<object, object> selector)
        {
            if (type == null || selector == null)
                return this;

            if (NodeConvertersForType.ContainsKey(type))
                NodeConvertersForType[type] = selector;
            else
                NodeConvertersForType.Add(type, selector);

            return this;
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

        public new ChoXmlRecordConfiguration ClearFields()
        {
            //XmlRecordFieldConfigurationsForType.Clear();
            XmlRecordFieldConfigurations.Clear();
            base.ClearFields();
            return this;
        }

        public ChoXmlRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (XmlRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = XmlRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                XmlRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoXmlRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = XmlRecordFieldConfigurations.Where(f => f.DeclaringMemberInternal == fieldName || f.FieldName == fieldName).FirstOrDefault();
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
            if (String.Compare(prefix, "xmlns") == 0)
                NamespaceManager.AddNamespace("", uri);
            else
                NamespaceManager.AddNamespace(prefix, uri);

            return this;
        }

        public ChoXmlRecordConfiguration WithXmlNamespace(string uri)
        {
            return WithXmlNamespace("", uri);
        }

        public ChoXmlRecordConfiguration WithXmlNamespaces(IDictionary<string, string> ns)
        {
            if (ns != null)
            {
                foreach (var kvp in ns)
                    WithXmlNamespace(kvp.Key, kvp.Value);
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

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfBaseType<ChoXmlNodeRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray());
            mapper?.Invoke(new ChoXmlRecordFieldConfigurationMap(cf));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForTypeInternal(rt))
                XmlRecordFieldConfigurationsForType.Remove(rt);
        }

        public void ClearRecordFieldsForType<T>()
        {
            ClearRecordFieldsForType(typeof(T));
        }

        public ChoXmlRecordConfiguration ConfigureTypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            CreateTypeConverterSpecsIfNull();
            spec?.Invoke(TypeConverterFormatSpec);
            return this;
        }

        public ChoXmlRecordConfiguration<T> MapRecordFieldsForType<T>()
        {
            return MapRecordFieldsForType(typeof(T)).OfType<T>();
        }

        public ChoXmlRecordConfiguration MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return null;

            if (ContainsRecordConfigForTypeInternal(rt))
                return CreateRecordConfigurationForType(rt);

            List<ChoXmlRecordFieldConfiguration> recordFieldConfigurations = new List<ChoXmlRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            XmlRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));

            return CreateRecordConfigurationForType(rt);
        }

        private ChoXmlRecordConfiguration CreateRecordConfigurationForType(Type recordType)
        {
            ChoXmlRecordConfiguration cf = this;

            var cf1 = new ChoXmlRecordConfiguration();
            Clone(cf1);
            cf1.XPath = "//";
            cf1.RecordType = recordType;

            cf1.XmlRecordFieldConfigurations.Clear();
            ChoXmlRecordFieldConfiguration[] fcf = cf.GetRecordConfigForType(recordType).OfType<ChoXmlRecordFieldConfiguration>().ToArray();
            if (!fcf.IsNullOrEmpty())
            {
                cf1.XmlRecordFieldConfigurations.AddRange(fcf);
            }

            return cf1;
        }

        internal void WithField(string name, string xPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            bool isXmlAttribute = false, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool? isArray = null, string nullValue = null, bool encodeValue = false, Type recordType = null,
            Type subRecordType = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null,
            IChoValueConverter propertyConverter = null
            )
        {
            ChoGuard.ArgumentNotNull(recordType, nameof(recordType));

            if (!name.IsNullOrEmpty())
            {
                if (subRecordType != null)
                    MapRecordFieldsForType(subRecordType);

                string fnTrim = name.NTrim();
                ChoXmlRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    XmlRecordFieldConfigurations.Remove(fc);
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }
                else if (subRecordType != null)
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, pd != null ? ChoTypeDescriptor.GetPropetyAttribute<ChoXmlNodeRecordFieldAttribute>(pd) : null,
                                pd != null ? pd.Attributes.OfType<Attribute>().ToArray() : null)
                {
                };
                nfc.XPath = !xPath.IsNullOrWhiteSpace() ? xPath : nfc.XPath;
                nfc.FieldType = fieldType != null ? fieldType : nfc.FieldType;
                nfc.FieldValueTrimOption = fieldValueTrimOption;
                nfc.FieldName = !fieldName.IsNullOrWhiteSpace() ? fieldName : (!nfc.FieldName.IsNullOrWhiteSpace() ? nfc.FieldName : name);
                nfc.ValueConverter = valueConverter != null ? valueConverter : nfc.ValueConverter;
                nfc.CustomSerializer = customSerializer != null ? customSerializer : nfc.CustomSerializer;
                nfc.DefaultValue = defaultValue != null ? defaultValue : nfc.DefaultValue;
                nfc.FallbackValue = fallbackValue != null ? fallbackValue : nfc.FallbackValue;
                nfc.FormatText = !formatText.IsNullOrWhiteSpace() ? formatText : nfc.FormatText;
                nfc.ItemConverter = itemConverter != null ? itemConverter : nfc.ItemConverter;
                nfc.IsArray = isArray != null ? isArray : nfc.IsArray;
                nfc.EncodeValue = encodeValue;
                nfc.NullValue = !nullValue.IsNullOrWhiteSpace() ? nullValue : nfc.NullValue;
                nfc.FieldTypeSelector = fieldTypeSelector != null ? fieldTypeSelector : nfc.FieldTypeSelector;
                nfc.ItemRecordTypeSelector = itemTypeSelector != null ? itemTypeSelector : nfc.ItemRecordTypeSelector;
                nfc.FieldTypeDiscriminator = fieldTypeDiscriminator != null ? fieldTypeDiscriminator : nfc.FieldTypeDiscriminator;
                nfc.ItemTypeDiscriminator = itemTypeDiscriminator != null ? itemTypeDiscriminator : nfc.ItemTypeDiscriminator;
                if (propertyConverter != null)
                    nfc.AddConverter(propertyConverter);

                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : fullyQualifiedMemberName;
                }
                else
                {
                    if (subRecordType == null)
                        pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName);
                    else
                        pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName);

                    nfc.PropertyDescriptorInternal = pd;
                    nfc.DeclaringMemberInternal = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                if (subRecordType == null)
                    XmlRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoXmlRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoXmlNodeRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoXmlRecordFieldConfiguration(propertyName, attr, otherAttrs);
                fc.PropertyDescriptorInternal = pd;
                fc.DeclaringMemberInternal = fqm;
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                if (!XmlRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                    XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(propertyName, attr, otherAttrs));

                var nfc = XmlRecordFieldConfigurations.First(fc => fc.Name == propertyName);
                nfc.PropertyDescriptorInternal = pd;
                nfc.DeclaringMemberInternal = fqm;

                return nfc;
            }
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

        public string GetFirstDefaultNamespace()
        {
            return XmlNamespaceManager.Value.GetFirstDefaultNamespace(this.NamespaceManager.DefaultNamespace);
        }

        public ChoXmlRecordConfiguration<T> OfType<T>()
        {
            var cf = new ChoXmlRecordConfiguration<T>(false);
            Clone(cf);

            return cf;
        }
    }

    public class ChoXmlRecordConfiguration<T> : ChoXmlRecordConfiguration
    {
        internal ChoXmlRecordConfiguration(bool nomap)
        {

        }

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
        public const string DefaultNSToken = "_x_";

        public readonly IDictionary<string, string> NSDict;
        public readonly XmlNamespaceManager NSMgr;
        public readonly XmlSerializerNamespaces XmlSerializerNamespaces;
        public readonly List<string> ReservedXmlNamespacePrefixes = new List<string>();

        public ChoXmlNamespaceManager() : this(new XmlNamespaceManager(new NameTable()))
        {
        }

        public ChoXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            NSMgr = nsMgr;
            NSDict = nsMgr.GetNamespacesInScope(XmlNamespaceScope.All);

            XmlSerializerNamespaces = new XmlSerializerNamespaces();
            foreach (var kvp in NSDict)
            {
                try
                {
                    XmlSerializerNamespaces.Add(kvp.Key, kvp.Value);
                }
                catch { }
            }

            ReservedXmlNamespacePrefixes.Add("xml");
            ReservedXmlNamespacePrefixes.Add("xsi");
            ReservedXmlNamespacePrefixes.Add("xsd");
        }

        public string GetNamespacePrefix(string ns)
        {
            if (ns.IsNullOrWhiteSpace())
                return null;

            return NSDict.Where(Xml => Xml.Value == ns /*&& !Xml.Key.IsNullOrWhiteSpace()*/).Select(Xml => Xml.Key).FirstOrDefault();
        }

        public string GetNamespacePrefixOrDefault(string ns, string defaultValue = null)
        {
            var nsPrefix = NSDict.Where(Xml => Xml.Value == ns && !Xml.Key.IsNullOrWhiteSpace()).Select(Xml => Xml.Key).FirstOrDefault();
            if (nsPrefix.IsNullOrWhiteSpace())
            {
                nsPrefix = defaultValue; //Configuration.DefaultNamespacePrefix;
                if (nsPrefix.IsNullOrWhiteSpace())
                {
                    nsPrefix = ChoXmlNamespaceManager.DefaultNSToken;
                }
            }
            return nsPrefix;
        }

        public XNamespace GetNamespaceForPrefix(string prefix)
        {
            if (prefix != null && NSDict.ContainsKey(prefix))
                return NSDict[prefix];
            else
                return null;
        }

        public string GetFirstDefaultNamespace(string defaultNamespace = null)
        {
            if (!defaultNamespace.IsNullOrWhiteSpace())
                return defaultNamespace;

            var ns = NSDict.Where(kvp => kvp.Key.IsNullOrWhiteSpace()).Select(kvp => kvp.Value).FirstOrDefault();
            if (!ns.IsNullOrWhiteSpace())
                return ns;
            ns = NSDict.Where(kvp => kvp.Key == ChoXmlNamespaceManager.DefaultNSToken).Select(kvp => kvp.Value).FirstOrDefault();
            if (!ns.IsNullOrWhiteSpace())
                return ns;

            return NSDict.Where(kvp => !ReservedXmlNamespacePrefixes.Contains(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault();
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

        public void AddNamespace(string prefix, string ns)
        {
            if (!NSDict.ContainsKey(prefix))
            {
                NSDict.Add(prefix, ns);
                XmlSerializerNamespaces?.Add(prefix, ns);
            }
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
                    if (kvp.Key == "xml" && config.DoNotEmitXmlNamespace)
                        continue;
                    if (kvp.Key == "xsi" && config.OmitXsiNamespace)
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
