using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace ChoETL
{
    internal class ChoXmlRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        private Lazy<XmlSerializer> _se = null;
        internal ChoReader Reader = null;
        private Lazy<List<XElement>> _recBuffer = null;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoXmlRecordReader(Type recordType, ChoXmlRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            //Configuration.Validate();
            _recBuffer = new Lazy<List<XElement>>(() =>
            {
                if (Reader != null)
                {
                    var b = Reader.Context.ContainsKey("RecBuffer") ? Reader.Context.RecBuffer : null;
                    if (b == null)
                        Reader.Context.RecBuffer = new List<XElement>();

                    return Reader.Context.RecBuffer;
                }
                else
                    return new List<XElement>();
            });
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            if (source == null)
                yield break;

            XmlReader sr = source as XmlReader;
            ChoGuard.ArgumentNotNull(sr, "XmlReader");

            InitializeRecordConfiguration(Configuration);

            if (!RaiseBeginLoad(sr))
                yield break;

            var d = sr.GetXmlttributesFromDeclaration();
            Configuration.XmlEncoding = d.XmlEncoding;
            Configuration.XmlVersion = d.XmlVersion;

            foreach (var item in AsEnumerable(sr.GetXmlElements(Configuration.XPath, Configuration.NamespaceManager,
                Configuration.AllowComplexXmlPath, (nsTable) => Configuration.XmlNamespaceTable = Configuration.XmlNamespaceTable == null ? nsTable : Configuration.XmlNamespaceTable), TraceSwitch, filterFunc))
            {
                yield return item;
            }

            RaiseEndLoad(sr);
        }

        public IEnumerable<object> AsEnumerable(IEnumerable<XElement> xElements, Func<object, bool?> filterFunc = null)
        {
            foreach (var item in AsEnumerable(xElements, TraceSwitch, filterFunc))
            {
                yield return item;
            }
        }

        private void CalcFieldMaxCountIfApplicable(IEnumerator<XElement> nodes)
        {
            if (Configuration.MaxScanRows <= 0)
                return;

            if (Configuration.AutoDiscoverColumns
                && Configuration.XmlRecordFieldConfigurations.Count == 0)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    long recCount = 0;
                    _configCheckDone = true;
                    while (nodes.MoveNext())
                    {
                        _recBuffer.Value.Add(nodes.Current);
                        recCount++;

                        XElement ele = nodes.Current;
                        if (ele != null)
                        {
                            var fcs = Configuration.DiscoverRecordFieldsFromXElement(ele);
                            var diff = fcs.Where(fc => !Configuration.XmlRecordFieldConfigurations.Any(fc1 => fc1.FieldName == fc.FieldName)).ToArray();
                            Configuration.XmlRecordFieldConfigurations.AddRange(diff);
                        }

                        if (Configuration.MaxScanRows == recCount)
                            break;
                    }

                    Configuration.Validate(null);
                    var dict = Configuration.XmlRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);
                    Configuration.UpdateFieldTypesIfAny(dict);

                }
            }
        }

        private IEnumerable<XElement> ReadNodes(IEnumerable<XElement> nodes)
        {
            var nodesEnum = nodes.GetEnumerator();
            CalcFieldMaxCountIfApplicable(nodesEnum);

            //object x = Reader.Context.RecBuffer;
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return rec;

            while (nodesEnum.MoveNext())
                yield return nodesEnum.Current;
        }

        private Lazy<Dictionary<string, Type>> _xmlTypeCache = new Lazy<Dictionary<string, Type>>(() =>
        {
            Dictionary<string, Type> dict = new Dictionary<string, Type>();
            var types = ChoType.GetTypes(typeof(XmlRootAttribute));
            if (types != null)
            {
                foreach (var t in types.Where(t1 => !t1.Name.IsNullOrWhiteSpace()))
                {
                    var xr = t.GetCustomAttribute(typeof(XmlRootAttribute)) as XmlRootAttribute;
                    if (xr == null || xr.ElementName.IsNullOrWhiteSpace())
                        continue;

                    dict.AddOrUpdate(xr.ElementName, t);
                }
            }

            return dict;
        });

        private object Deserialize(Tuple<long, XElement> pair)
        {
            if (_se.Value != null)
                return _se.Value.Deserialize(pair.Item2.CreateReader());
            else
            {
                string name = pair.Item2.Name.ToString();
                var recType = _xmlTypeCache.Value.ContainsKey(name) && _xmlTypeCache.Value[name] != null ? _xmlTypeCache.Value[name] : RecordType;
                return pair.Item2.ToObjectFromXml(recType, nsMgr: Configuration.XmlNamespaceManager.Value);
            }
        }

        bool _nsInitialized = false;
        private IEnumerable<object> AsEnumerable(IEnumerable<XElement> xElements, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, XElement> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool? doWhile = true;
            bool abortRequested = false;
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;
            _se = new Lazy<XmlSerializer>(() => Configuration.XmlSerializer == null ? null : Configuration.XmlSerializer);
            if (!Configuration.NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace())
            {
                _nsInitialized = true;
                Configuration.NamespaceManager.AddNamespace("x", Configuration.NamespaceManager.DefaultNamespace);
            }

            foreach (XElement el in ReadNodes(xElements))
            {
                if (!_nsInitialized)
                {
                    _nsInitialized = true;
                    if (!Configuration.NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace())
                    {
                        Configuration.NamespaceManager.AddNamespace("x", Configuration.NamespaceManager.DefaultNamespace);
                        ChoXmlSettings.XmlNamespace = Configuration.NamespaceManager.DefaultNamespace;
                    }
                }

                skip = false;
                pair = new Tuple<long, XElement>(++counter, el);

                if (skipUntil != null)
                {
                    if (skipUntil.Value)
                    {
                        skipUntil = RaiseSkipUntil(pair);
                        if (skipUntil == null)
                        {

                        }
                        else
                        {
                            skip = skipUntil.Value;
                        }
                    }
                }
                if (skip == null)
                    break;
                if (skip.Value)
                    continue;

                if (!_configCheckDone)
                {
                    if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null && !Configuration.RecordTypeMapped)
                    {
                    }
                    else
                        Configuration.Validate(pair);
                    var dict = Configuration.XmlRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                    if (Configuration.MaxScanRows == 0)
                        RaiseMembersDiscovered(dict);
                    Configuration.UpdateFieldTypesIfAny(dict);
                    _configCheckDone = true;
                }


                object rec = null;
                if (TraceSwitch.TraceVerbose)
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading node [{0}]...".FormatString(pair.Item1));

                if (!LoadNode(pair, ref rec))
                    yield break;

                if (rec == null)
                    continue;

                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.AreAllFieldTypesNull && Configuration.AutoDiscoverFieldTypes && Configuration.MaxScanRows > 0 && counter <= Configuration.MaxScanRows)
                    {
                        buffer.Add(rec);
                        if (recFieldTypes == null)
                        {
                            string[] dupFields = Configuration.XmlRecordFieldConfigurations.GroupBy(i => i.FieldName)
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key).ToArray();
                            if (dupFields.Length > 0)
                            {
                                throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
                            }

                            recFieldTypes = Configuration.XmlRecordFieldConfigurations.GroupBy(i => i.FieldName).Select(g => new { FieldName = g.Key, FieldType = g.First().FieldType })
                                .ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                        }
                        RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, counter == Configuration.MaxScanRows);
                        if (counter == Configuration.MaxScanRows)
                        {
                            Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                            var dict = recFieldTypes = Configuration.XmlRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                            RaiseMembersDiscovered(dict);

                            foreach (object rec1 in buffer)
                                yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes));

                            buffer.Clear();
                        }
                    }
                    else
                    {
                        yield return rec;
                    }
                }
                else
                    yield return rec;

                if (Configuration.NotifyAfter > 0 && pair.Item1 % Configuration.NotifyAfter == 0)
                {
                    if (RaisedRowsLoaded(pair.Item1))
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                        abortRequested = true;
                        yield break;
                    }
                }

                if (doWhile != null)
                {
                    doWhile = RaiseDoWhile(pair);
                    if (doWhile != null && doWhile.Value)
                        break;
                }
            }

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {
                if (buffer.Count > 0)
                {
                    Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                    var dict = recFieldTypes = Configuration.XmlRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);

                    foreach (object rec1 in buffer)
                        yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes));
                }
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1);
        }

        private static void Parse(dynamic parent, XElement node)
        {
            if (node.HasElements)
            {
                if (node.Elements(node.Elements().First().Name.LocalName).Count() > 1)
                {
                    //list
                    var item = new ExpandoObject();
                    var list = new List<dynamic>();
                    foreach (var element in node.Elements())
                    {
                        Parse(list, element);
                    }

                    AddProperty(item, node.Elements().First().Name.LocalName, list);
                    AddProperty(parent, node.Name.ToString(), item);
                }
                else
                {
                    var item = new ExpandoObject();

                    foreach (var attribute in node.Attributes())
                    {
                        AddProperty(item, attribute.Name.ToString(), attribute.Value.Trim());
                    }

                    //element
                    foreach (var element in node.Elements())
                    {
                        Parse(item, element);
                    }

                    AddProperty(parent, node.Name.ToString(), item);
                }
            }
            else
            {
                AddProperty(parent, node.Name.ToString(), node.NilAwareValue().NTrim());
            }
        }

        private static void AddProperty(dynamic parent, string name, object value)
        {
            if (parent is List<dynamic>)
            {
                (parent as List<dynamic>).Add(value);
            }
            else
            {
                (parent as IDictionary<String, object>)[name] = value;
            }
        }

        private bool LoadNode(Tuple<long, XElement> pair, ref object rec)
        {
            bool ignoreFieldValue = pair.Item2.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
            if (ignoreFieldValue)
                return false;
            else if (pair.Item2 == null && !Configuration.IsDynamicObject)
            {
                rec = RecordType.CreateInstanceAndDefaultToMembers(Configuration.RecordFieldConfigurationsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ChoRecordFieldConfiguration));
                return true;
            }

            if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null)
            {
                Type recType = Configuration.RecordSelector(pair);
                if (recType == null)
                {
                    if (Configuration.IgnoreIfNoRecordTypeFound)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, $"No record type found for [{pair.Item1}] line to parse.");
                        return true;
                    }
                    else
                        throw new ChoParserException($"No record type found for [{pair.Item1}] line to parse.");
                }

                if (!Configuration.RecordTypeMapped)
                {
                    Configuration.MapRecordFields(recType);
                    Configuration.Validate(null);
                }

                rec = recType.IsDynamicType() ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : ChoActivator.CreateInstance(recType);
                RecordType = recType;
            }
            else if (!Configuration.UseXmlSerialization || Configuration.IsDynamicObject)
                rec = Configuration.IsDynamicObject ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : ChoActivator.CreateInstance(RecordType);

            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping...");
                    rec = null;
                    return true;
                }

                if (Configuration.CustomNodeSelecter != null)
                {
                    pair = new Tuple<long, XElement>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                }

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }


                if (!Configuration.UseXmlSerialization
                    && !typeof(ICollection).IsAssignableFrom(Configuration.RecordType)
                    && !(Configuration.RecordType.IsGenericType && Configuration.RecordType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    )
                {
                    if (!FillRecord(rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);
                }
                else
                {
                    //if (Configuration.IsDynamicObject)
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                        Parse(rec, pair.Item2);
                    else
                    {
                        rec = Deserialize(pair); // _se.Value.Deserialize(pair.Item2.CreateReader());
                    }

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                        rec.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);
                }


                bool skip = false;
                if (!RaiseAfterRecordLoad(rec, pair, ref skip))
                    return false;
                else if (skip)
                {
                    rec = null;
                    return true;
                }
            }
            //catch (ChoParserException)
            //{
            //    throw;
            //}
            //catch (ChoMissingRecordFieldException)
            //{
            //    throw;
            //}
            catch (Exception ex)
            {
                Reader.IsValid = false;
                if (ex is ChoMissingRecordFieldException && Configuration.ThrowAndStopOnMissingField)
                {
                    if (!RaiseRecordLoadError(rec, pair, ex))
                        throw;
                    else
                    {
                        //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        //rec = null;
                    }
                }
                else
                {
                    ChoETLFramework.HandleException(ref ex);
                    if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        rec = null;
                    }
                    else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                    {
                        if (!RaiseRecordLoadError(rec, pair, ex))
                            throw;
                        else
                        {
                            //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            //rec = null;
                        }
                    }
                    else
                        throw;
                }

                return true;
            }

            return true;
        }

        private void ToDictionary(XElement node)
        {
            Dictionary<string, List<string>> dictionary = xDict; // new Dictionary<string, List<string>>();
            xDict.Clear();

            string key = null;
            foreach (XAttribute elem in node.Attributes())
            {
                key = Configuration.GetNameWithNamespace(node.Name, elem.Name);

                if (key.IsValidXNode(Configuration.DefaultNamespacePrefix))
                {
                    //avoid duplicates
                    if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, new List<string>());

                    dictionary[key].Add(elem.Value);
                }
            }
        }

        List<object> xNodes = new List<object>();
        XElement[] fXElements = null;
        object fieldValue = null;
        ChoXmlRecordFieldConfiguration fieldConfig = null;
        PropertyInfo pi = null;
        XPathNavigator xpn = null;
        private readonly Dictionary<string, List<string>> xDict = new Dictionary<string, List<string>>();

        private bool FillRecord(object rec, Tuple<long, XElement> pair)
        {
            long lineNo;
            XElement node;
            string key = null;
            bool isXmlAttribute = false;

            lineNo = pair.Item1;
            node = pair.Item2;

            fXElements = null;
            fieldValue = null;
            fieldConfig = null;
            pi = null;
            xpn = node.CreateNavigator(Configuration.NamespaceManager.NameTable);
            ToDictionary(node);

            object rootRec = rec;

            if (rec is ChoDynamicObject)
                ((ChoDynamicObject)rec).DynamicObjectName = node.Name.LocalName;
            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                key = kvp.Key;
                isXmlAttribute = false;
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(key, out pi);

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                if (fieldConfig.XPath == "text()")
                {
                    if (Configuration.GetNameWithNamespace(node.Name) == fieldConfig.FieldName)
                    {
                        object value = node;
                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, key, ref value))
                            continue;
                        if (fieldConfig.CustomSerializer != null)
                            value = Normalize(fieldConfig.CustomSerializer(node));
                        else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref value))
                            value = Normalize(value);
                        else if (fieldConfig.PropCustomSerializer != null)
                            value = Normalize(ChoCustomSerializer.Deserialize(value, fieldConfig.FieldType, fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name));

                        if (value is XElement)
                        {
                            dynamic dobj = ((XElement)value).ToObjectFromXml(typeof(ChoDynamicObject), GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value);
                            if (dobj == null || !dobj.HasText())
                                continue;

                            fieldValue = dobj.GetText();
                        }
                        else
                            fieldValue = value;
                    }
                    else if (Configuration.ColumnCountStrict)
                        throw new ChoParserException("Missing '{0}' xml node.".FormatString(fieldConfig.FieldName));
                }
                else
                {
                    if (/*!fieldConfig.UseCache && */!xDict.ContainsKey(fieldConfig.FieldName))
                    {
                        xNodes.Clear();
                        foreach (XPathNavigator z in xpn.Select(fieldConfig.XPath, Configuration.NamespaceManager))
                        {
                            xNodes.Add(z.UnderlyingObject);
                        }

                        object value = xNodes;
                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, key, ref value))
                            continue;

                        if (fieldConfig.CustomSerializer != null)
                            fieldValue = Normalize(fieldConfig.CustomSerializer(xNodes));
                        else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref value))
                            fieldValue = Normalize(value);
                        else if (fieldConfig.PropCustomSerializer != null)
                            fieldValue = Normalize(ChoCustomSerializer.Deserialize(value, fieldConfig.FieldType, fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name));
                        else
                        {
                            //object[] xNodes = ((IEnumerable)node.XPathEvaluate(fieldConfig.XPath, Configuration.NamespaceManager)).OfType<object>().ToArray();
                            //continue;
                            XAttribute[] fXAttributes = xNodes.OfType<XAttribute>().ToArray();
                            if (!fXAttributes.IsNullOrEmpty()) //fXAttribute != null)
                            {
                                //fieldValue = fXAttribute.Value;
                                if (fieldConfig.FieldType == null)
                                {
                                    if (fXAttributes.Length == 1)
                                    {
                                        if (fieldConfig.ItemConverter != null)
                                            fieldValue = Normalize(fieldConfig.ItemConverter(fXAttributes[0]));
                                        else
                                            fieldValue = fXAttributes[0].Value;
                                    }
                                    else
                                    {
                                        List<object> arr = new List<object>();
                                        foreach (var ele in fXAttributes)
                                        {
                                            if (fieldConfig.ItemConverter != null)
                                                arr.Add(Normalize(fieldConfig.ItemConverter(ele)));
                                            else
                                                arr.Add(ele.Value);
                                        }

                                        fieldValue = arr.ToArray();
                                    }
                                }
                                else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                                {
                                    if (!fieldConfig.HasConverters())
                                    {
                                        XAttribute fXElement = fXAttributes.FirstOrDefault();
                                        if (fXElement != null)
                                        {
                                            if (fieldConfig.ItemConverter != null)
                                                fieldValue = Normalize(fieldConfig.ItemConverter(fXElement));
                                            else
                                                fieldValue = fXElement.Value;
                                        }
                                    }
                                }
                                else if (fieldConfig.FieldType.IsCollection() || fieldConfig.FieldType.IsGenericList()
                                    || fieldConfig.FieldType.IsGenericEnumerable())
                                {
                                    List<object> list = new List<object>();
                                    Type itemType = fieldConfig.FieldType.GetItemType().GetUnderlyingType();

                                    foreach (var ele in fXAttributes)
                                    {
                                        if (fieldConfig.ItemConverter != null)
                                            list.Add(Normalize(fieldConfig.ItemConverter(ele)));
                                        else
                                        {
                                            if (itemType.IsSimple())
                                                list.Add(Normalize(ChoConvert.ConvertTo(ele.Value, itemType)));
                                            else
                                            {
                                                list.Add(ele.Value);
                                            }
                                        }
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    if (!fieldConfig.HasConverters())
                                    {
                                        XAttribute fXElement = fXAttributes.FirstOrDefault();
                                        if (fXElement != null)
                                        {
                                            if (fieldConfig.ItemConverter != null)
                                                fieldValue = Normalize(fieldConfig.ItemConverter(fXElement));
                                            else
                                            {
                                                fieldValue = Normalize(fXElement.Value);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                fXElements = xNodes.OfType<XElement>().ToArray();

                                if (!fXElements.IsNullOrEmpty())
                                {
                                    if ((fieldConfig.IsArray != null && fieldConfig.IsArray.Value)
                                        || IsArray(xpn.Name, fXElements))
                                    {
                                        List<object> list = new List<object>();
                                        Type itemType = fieldConfig.FieldType != null ? fieldConfig.FieldType.GetItemType().GetUnderlyingType() :
                                            typeof(ChoDynamicObject);

                                        foreach (var ele in fXElements)
                                        {
                                            if (fieldConfig.ItemConverter != null)
                                                list.Add(Normalize(fieldConfig.ItemConverter(ele)));
                                            else
                                            {
                                                if (itemType.IsSimple())
                                                    list.Add(Normalize(ChoConvert.ConvertTo(ele.NilAwareValue(), itemType)));
                                                else
                                                {
                                                    if (itemType == typeof(ChoDynamicObject))
                                                        list.Add(Normalize(ele.ToDynamic(Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                                            defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value)));
                                                    else
                                                        list.Add(Normalize(ele.ToObjectFromXml(itemType, GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                                            defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value)));
                                                }
                                            }
                                        }
                                        fieldValue = list.ToArray();

                                        if ((fieldConfig.IsArray != null && fieldConfig.IsArray.Value))
                                        {

                                        }
                                        else
                                        {
                                            if (key.IsSingular())
                                                key = key.ToPlural();
                                        }
                                    }
                                    else
                                    {
                                        if (fieldConfig.FieldType == null
                                            || fieldConfig.FieldType == typeof(object)
                                            || fieldConfig.FieldType.GetItemType() == typeof(object))
                                        {
                                            if (fXElements.Length == 1)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    fieldValue = fieldConfig.ItemConverter(fXElements[0]);
                                                else
                                                    fieldValue = fXElements[0].ToObjectFromXml(typeof(ChoDynamicObject),
                                                        GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace,
                                                        Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative, defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value);

                                                fieldValue = Normalize(fieldValue);
                                            }
                                            else
                                            {
                                                List<object> arr = new List<object>();
                                                foreach (var ele in fXElements)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        arr.Add(Normalize(fieldConfig.ItemConverter(ele)));
                                                    else
                                                        arr.Add(Normalize(ele.ToObjectFromXml(typeof(ChoDynamicObject), GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                                            defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value) as ChoDynamicObject));
                                                }

                                                fieldValue = arr.ToArray();
                                            }
                                        }
                                        else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                                        {
                                            if (!fieldConfig.HasConverters())
                                            {
                                                XElement fXElement = fXElements.FirstOrDefault();
                                                if (fXElement != null)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        fieldValue = Normalize(fieldConfig.ItemConverter(fXElement));
                                                    else
                                                        fieldValue = Normalize(fXElement.NilAwareValue());
                                                }
                                            }
                                        }
                                        else if (fieldConfig.FieldType.IsCollection() || fieldConfig.FieldType.IsGenericList()
                                            || fieldConfig.FieldType.IsGenericEnumerable())
                                        {
                                            List<object> list = new List<object>();
                                            Type itemType = fieldConfig.FieldType.GetItemType().GetUnderlyingType();

                                            //if (!itemType.IsSimple())
                                            //{
                                            //    fXElements = fXElements.SelectMany(e => e.Elements()).ToArray();
                                            //}

                                            foreach (var ele in fXElements)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    list.Add(Normalize(fieldConfig.ItemConverter(ele)));
                                                else
                                                {
                                                    if (itemType.IsSimple())
                                                        list.Add(Normalize(ChoConvert.ConvertTo(ele.NilAwareValue(), itemType)));
                                                    else
                                                    {
                                                        list.Add(Normalize(ele.ToObjectFromXml(itemType, GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                                            defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value)));
                                                    }
                                                }
                                            }

                                            fieldValue = list.ToArray();
                                        }
                                        else
                                        {
                                            if (!fieldConfig.HasConverters())
                                            {
                                                XElement fXElement = fXElements.FirstOrDefault(); //.SelectMany(e => e.Elements()).FirstOrDefault();
                                                if (fXElement != null)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        fieldValue = Normalize(fieldConfig.ItemConverter(fXElement));
                                                    else
                                                    {
                                                        fieldValue = Normalize(fXElement.ToObjectFromXml(fieldConfig.FieldType, GetXmlOverrides(fieldConfig),
                                                            Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace,
                                                            Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative, ChoNullValueHandling.Ignore, Configuration.XmlNamespaceManager.Value.GetFirstDefaultNamespace(),
                                                            defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    XText[] xTexts = xNodes.OfType<XText>().ToArray();
                                    if (!xTexts.IsNullOrEmpty())
                                    {
                                        if (fieldConfig.FieldType == null)
                                        {
                                            if (xTexts.Length == 1)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    fieldValue = fieldConfig.ItemConverter(xTexts[0]);
                                                else
                                                    fieldValue = xTexts[0].Value;
                                            }
                                            else
                                            {
                                                List<object> arr = new List<object>();
                                                foreach (var ele in xTexts)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        arr.Add(fieldConfig.ItemConverter(ele));
                                                    else
                                                        arr.Add(ele.Value);
                                                }

                                                fieldValue = arr.ToArray();
                                            }
                                        }
                                        else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                                        {
                                            if (!fieldConfig.HasConverters())
                                            {
                                                XText fXElement = xTexts.FirstOrDefault();
                                                if (fXElement != null)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        fieldValue = fieldConfig.ItemConverter(fXElement);
                                                    else
                                                        fieldValue = fXElement.Value;
                                                }
                                            }
                                        }
                                        else if (fieldConfig.FieldType.IsCollection() || fieldConfig.FieldType.IsGenericList()
                                            || fieldConfig.FieldType.IsGenericEnumerable())
                                        {
                                            List<object> list = new List<object>();
                                            Type itemType = fieldConfig.FieldType.GetItemType().GetUnderlyingType();

                                            foreach (var ele in xTexts)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    list.Add(fieldConfig.ItemConverter(ele));
                                                else
                                                {
                                                    if (itemType.IsSimple())
                                                        list.Add(ChoConvert.ConvertTo(ele.Value, itemType));
                                                    else
                                                    {
                                                        list.Add(ele.Value);
                                                    }
                                                }
                                            }
                                            fieldValue = list.ToArray();
                                        }
                                        else
                                        {
                                            if (!fieldConfig.HasConverters())
                                            {
                                                XText fXElement = xTexts.FirstOrDefault();
                                                if (fXElement != null)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        fieldValue = fieldConfig.ItemConverter(fXElement);
                                                    else
                                                    {
                                                        fieldValue = fXElement.Value;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if (Configuration.ColumnCountStrict)
                                        throw new ChoParserException("Missing '{0}' xml node.".FormatString(fieldConfig.FieldName));
                                }
                            }
                        }
                    }
                    else
                    {
                        isXmlAttribute = true;

                        if (xDict[fieldConfig.FieldName].Count == 1)
                            fieldValue = xDict[fieldConfig.FieldName][0];
                        else
                            fieldValue = xDict[fieldConfig.FieldName];

                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, key, ref fieldValue))
                            continue;

                        //if (fieldConfig.ValueConverter != null)
                        //    fieldValue = fieldConfig.ValueConverter(fieldValue);
                    }
                }

                //if (Configuration.IsDynamicObject)

                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (fieldValue != null && kvp.Value.FieldType == null && lineNo == 1)
                    {
                        if (Configuration.MaxScanRows == 0)
                        {
                            kvp.Value.FieldType = null;
                        }
                        else
                        {
                            kvp.Value.FieldType = fieldValue is ICollection ? fieldValue.GetType() : null; // fieldValue.GetType().IsSimple() ? DiscoverFieldType(fieldValue as string, Configuration) : null;
                        }
                    }
                }
                else
                {
                    if (pi != null)
                    {
                        if (kvp.Value.FieldTypeSelector != null)
                        {
                            Type rt = kvp.Value.FieldTypeSelector(pair.Item2);
                            kvp.Value.FieldType = rt == null ? pi.PropertyType : rt;
                        }
                        else
                            kvp.Value.FieldType = pi.PropertyType;
                    }
                    else
                        kvp.Value.FieldType = typeof(string);
                }

                if (fieldValue is string)
                    fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string, kvp.Value.EncodeValue);

                try
                {
                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        if (!fieldConfig.IsArray.CastTo<bool>())
                        {
                            bool isArray = fieldValue is IDictionary<string, Object> ? ((IDictionary<string, Object>)fieldValue).Count == 1 && key == ((IDictionary<string, Object>)fieldValue).Keys.First() : false;
                            if (isArray)
                            {
                                fieldValue = ((IDictionary<string, Object>)fieldValue).Values.First();
                                dict.ConvertNSetMemberValue(key, kvp.Value, ref fieldValue, Configuration.Culture);
                            }
                            else
                            {
                                isArray = fieldValue is IList;
                                if (isArray)
                                {
                                    var objs = ((IList)fieldValue).OfType<ChoDynamicObject>();
                                    string firstName = objs.FirstOrDefault() != null ? objs.First().DynamicObjectName : null;

                                    if (objs.Count() == ((IList)fieldValue).Count && firstName != null
                                        && objs.All(o => o.DynamicObjectName == firstName))
                                    {
                                        string key1 = firstName;

                                        if (!Configuration.IsTurnOffPluralization(fieldConfig))
                                        {
                                            key1 = key1.ToPlural();
                                            if (key1 == firstName)
                                                key1 = "{0}s".FormatString(firstName);
                                        }
                                        dict.ConvertNSetMemberValue(key1, kvp.Value, ref fieldValue, Configuration.Culture);
                                    }
                                    else
                                    {
                                        dict.ConvertNSetMemberValue(key, kvp.Value, ref fieldValue, Configuration.Culture);
                                    }
                                }
                                else
                                    dict.ConvertNSetMemberValue(key, kvp.Value, ref fieldValue, Configuration.Culture);
                            }
                        }
                        else
                            dict[key] = fieldValue;

                        if (isXmlAttribute)
                        {
                            if (dict is ChoDynamicObject)
                                ((ChoDynamicObject)dict).SetAttribute(key, fieldValue);
                        }

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
                        if (Configuration.SupportsMultiRecordTypes)
                        {
                            ChoType.TryGetProperty(rec.GetType(), kvp.Key, out pi);
                            fieldConfig.PI = pi;
                            fieldConfig.PropConverters = ChoTypeDescriptor.GetTypeConverters(fieldConfig.PI);
                            fieldConfig.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fieldConfig.PI);

                            //Load Custom Serializer
                            fieldConfig.PropCustomSerializer = ChoTypeDescriptor.GetCustomSerializer(fieldConfig.PI);
                            fieldConfig.PropCustomSerializerParams = ChoTypeDescriptor.GetCustomSerializerParams(fieldConfig.PI);
                        }

                        if (pi != null)
                            rec.ConvertNSetMemberValue(key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else if (!Configuration.SupportsMultiRecordTypes)
                            throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(key, ChoType.GetTypeName(rec)));

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            rec.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                    }

                    if (!RaiseAfterRecordFieldLoad(rec, pair.Item1, key, fieldValue))
                        return false;
                }
                catch (ChoParserException)
                {
                    Reader.IsValid = false;
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    Reader.IsValid = false;
                    if (Configuration.ThrowAndStopOnMissingField)
                        throw;
                }
                catch (Exception ex)
                {
                    Reader.IsValid = false;
                    ChoETLFramework.HandleException(ref ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw;

                    try
                    {
                        if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                        {
                            var dict = rec as IDictionary<string, Object>;

                            if (dict.SetFallbackValue(key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (dict.SetDefaultValue(key, kvp.Value, Configuration.Culture))
                                dict.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                    }
                    catch (Exception innerEx)
                    {
                        if (ex == innerEx.InnerException || ex is ValidationException)
                        {
                            if (fieldConfig.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                            {
                                continue;
                            }
                            else
                            {
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, key, ref fieldValue, ex))
                                {
                                    if (ex is ValidationException)
                                        throw;

                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                                }
                                else
                                {
                                    try
                                    {
                                        if (Configuration.IsDynamicObject)
                                        {
                                            var dict = rec as IDictionary<string, Object>;

                                            dict.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);
                                        }
                                        else
                                        {
                                            if (pi != null)
                                                rec.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);
                                            else
                                                throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(key, ChoType.GetTypeName(rec)));
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            throw new ChoReaderException("Failed to assign '{0}' fallback value to '{1}' field.".FormatString(fieldValue, fieldConfig.FieldName), innerEx);
                        }
                    }
                }
            }

            //Find any object members and serialize them
            //if (!Configuration.IsDynamicObject) //rec is ExpandoObject)
            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {

            }
            else
            {
                rec = SerializeObjectMembers(rec);
            }

            return true;
        }

        private object SerializeObjectMembers(object target, bool isTop = true)
        {
            if (target == null)
                return target;

            Type recordType = target.GetType();
            if (typeof(XObject).IsAssignableFrom(recordType))
                return target;
            if (recordType.IsSimple())
                return target;
            if (typeof(IList).IsAssignableFrom(recordType))
            {
#if NETSTANDARD2_0
                return target;
#else
                return ((IList)target).Cast((t) =>
                {
                    return SerializeObjectMembers(t, false);
                });
#endif
            }
            if (typeof(IDictionary).IsAssignableFrom(recordType))
            {
#if NETSTANDARD2_0
                return target;
#else
                return ((IDictionary)target).Cast((t) =>
                {
                    var key = t.Key;
                    var value = t.Value;

                    key = SerializeObjectMembers(key, false);
                    value = SerializeObjectMembers(value, false);
                    return new KeyValuePair<object, object>(key, value);
                });
#endif
            }

            if (typeof(IEnumerable).IsAssignableFrom(recordType))
                return target;

            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
            {
                if (pd.PropertyType == typeof(object))
                {
                    var pi = ChoType.GetProperty(recordType, pd.Name);
                    var propConverters = ChoTypeDescriptor.GetTypeConverters(pi);
                    var propConverterParams = ChoTypeDescriptor.GetTypeConverterParams(pi);

                    var itemValue = ChoType.GetPropertyValue(target, pd.Name);

                    if (propConverters.IsNullOrEmpty())
                    {
                        if (itemValue != null)
                        {
                            Type itemType = itemValue.GetType();
                            if (itemType == typeof(XAttribute))
                            {
                                ChoType.SetPropertyValue(target, pd.Name, System.Net.WebUtility.HtmlDecode(((XAttribute)itemValue).Value));
                            }
                            else if (itemType == typeof(XElement))
                            {
                                fieldValue = Normalize(((XElement)itemValue).ToObjectFromXml(typeof(ChoDynamicObject), GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                    defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value));
                                ChoType.SetPropertyValue(target, pd.Name, fieldValue);
                            }
                            else if (typeof(IList<XAttribute>).IsAssignableFrom(itemType))
                            {
                                fieldValue = ((IList)itemValue).Cast(t => ((XAttribute)t).Value);
                                ChoType.SetPropertyValue(target, pd.Name, fieldValue);
                            }
                            else if (typeof(IList<XElement>).IsAssignableFrom(itemType))
                            {
                                fieldValue = ((IList)itemValue).Cast(t => ((XElement)itemValue).ToObjectFromXml(typeof(ChoDynamicObject), GetXmlOverrides(fieldConfig), Configuration.XmlSchemaNamespace, Configuration.JSONSchemaNamespace, Configuration.EmptyXmlNodeValueHandling, Configuration.RetainXmlAttributesAsNative,
                                    defaultNSPrefix: Configuration.DefaultNamespacePrefix, nsMgr: Configuration.XmlNamespaceManager.Value));
                                ChoType.SetPropertyValue(target, pd.Name, fieldValue);
                            }
                        }
                    }
                    else
                    {
                        var fv = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, propConverters, propConverterParams, Configuration.Culture);
                        ChoType.SetPropertyValue(target, pd.Name, fv);
                    }
                }
                else
                {
                    ChoType.SetPropertyValue(target, pd.Name, SerializeObjectMembers(ChoType.GetPropertyValue(target, pd.Name), false));
                }
            }
            return target;
        }
        private object Normalize(object fieldValue)
        {
            if (Configuration.RetainAsXmlAwareObjects)
                return fieldValue;

            if (fieldValue is ChoDynamicObject)
            {
                var dict = fieldValue as ChoDynamicObject;
                if (dict.Keys.Count == 1 && dict.HasText())
                {
                    fieldValue = dict.GetText();
                }
            }

            return fieldValue;
        }

        private bool IsArray(string fieldName, XElement[] fXElements)
        {
            if (fXElements == null || fieldName == null)
                return false;

            string parentNodeName = fieldName.ToSingular();
            return fXElements.All(x => Configuration.StringComparer.Compare(x.Name.LocalName, parentNodeName) == 0);
        }

        private XmlAttributeOverrides GetXmlOverrides(ChoXmlRecordFieldConfiguration fieldConfig, Type fieldType = null)
        {
            if (fieldType == null) return null;

            XmlAttributeOverrides overrides = null;
            var xattribs = new XmlAttributes();
            var xroot = new XmlRootAttribute(fieldConfig.FieldName);
            xattribs.XmlRoot = xroot;
            overrides = new XmlAttributeOverrides();
            fieldType = fieldType == null ? fieldConfig.FieldType : fieldType;
            overrides.Add(fieldType, xattribs);
            return overrides;
        }

        private string CleanFieldValue(ChoXmlRecordFieldConfiguration config, Type fieldType, string fieldValue, bool? encodeValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForRead(fieldType, Configuration.FieldValueTrimOption);

            if (config.FieldValueTrimOption == null)
            {
                //if (fieldType == typeof(string))
                //    fieldValueTrimOption = ChoFieldValueTrimOption.None;
            }
            else
                fieldValueTrimOption = config.FieldValueTrimOption.Value;

            switch (fieldValueTrimOption)
            {
                case ChoFieldValueTrimOption.Trim:
                    fieldValue = fieldValue.Trim();
                    break;
                case ChoFieldValueTrimOption.TrimStart:
                    fieldValue = fieldValue.TrimStart();
                    break;
                case ChoFieldValueTrimOption.TrimEnd:
                    fieldValue = fieldValue.TrimEnd();
                    break;
            }

            if (config.Size != null)
            {
                if (fieldValue.Length > config.Size.Value)
                {
                    if (!config.Truncate)
                        throw new ChoParserException("Incorrect field value length found for '{0}' member [Expected: {1}, Actual: {2}].".FormatString(config.FieldName, config.Size.Value, fieldValue.Length));
                    else
                    {
                        if (fieldValueTrimOption == ChoFieldValueTrimOption.TrimStart)
                            fieldValue = fieldValue.Right(config.Size.Value);
                        else
                            fieldValue = fieldValue.Substring(0, config.Size.Value);
                    }
                }
            }

            if (encodeValue != null && !encodeValue.Value && fieldValue != null)
                return System.Net.WebUtility.HtmlEncode(fieldValue);

            return System.Net.WebUtility.HtmlDecode(fieldValue);
        }

#region Event Raisers

        private bool RaiseBeginLoad(object state)
        {
            if (Reader != null && Reader.HasBeginLoadSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeginLoad(state), true);
            }
            else if (_callbackFileRead != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFileRead.BeginLoad(state), true);
            }
            return true;
        }

        private void RaiseEndLoad(object state)
        {
            if (Reader != null && Reader.HasEndLoadSubscribed)
            {
                ChoActionEx.RunWithIgnoreError(() => Reader.RaiseEndLoad(state));
            }
            else if (_callbackFileRead != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackFileRead.EndLoad(state));
            }
        }

        private bool? RaiseSkipUntil(Tuple<long, XElement> pair)
        {
            if (Reader != null && Reader.HasSkipUntilSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseSkipUntil(index, state));

                return retValue;
            }
            else if (_callbackFileRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackFileRead.SkipUntil(index, state));

                return retValue;
            }
            return null;
        }

        private bool? RaiseDoWhile(Tuple<long, XElement> pair)
        {
            if (Reader != null && Reader.HasDoWhileSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseDoWhile(index, state));

                return retValue;
            }
            else if (_callbackFileRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackFileRead.DoWhile(index, state));

                return retValue;
            }
            return null;
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, XElement> pair)
        {
            if (Reader != null && Reader.HasBeforeRecordLoadSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, XElement>(index, state as XElement);

                return retValue;
            }
            else if (_callbackRecordRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, XElement>(index, state as XElement);

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, XElement> pair, ref bool skip)
        {
            bool ret = true;
            bool sp = false;

            if (Reader != null && Reader.HasAfterRecordLoadSubscribed)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            else if (_callbackRecordRead != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.AfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            skip = sp;
            return ret;
        }

        private bool RaiseRecordLoadError(object target, Tuple<long, XElement> pair, Exception ex)
        {
            if (Reader != null && Reader.HasRecordLoadErrorSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            else if (_callbackRecordRead != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (Reader != null && Reader.HasBeforeRecordFieldLoadSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).BeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (_callbackRecordFieldRead != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldRead.BeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            if (Reader != null && Reader.HasAfterRecordFieldLoadSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (_callbackRecordFieldRead != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldRead.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = false;
            object state = value;
            if (Reader != null && Reader.HasRecordFieldLoadErrorSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, ref state, ex), false);
                if (retValue)
                    value = state;
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).RecordFieldLoadError(target, index, propName, ref state, ex), false);
                if (retValue)
                    value = state;
            }
            else if (_callbackRecordFieldRead != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldRead.RecordFieldLoadError(target, index, propName, ref state, ex), false);
                if (retValue)
                    value = state;
            }
            return retValue;
        }

#endregion Event Raisers

        private bool RaiseRecordFieldDeserialize(object target, long index, string propName, ref object value)
        {
            if (Reader is IChoSerializableReader && ((IChoSerializableReader)Reader).HasRecordFieldDeserializeSubcribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoSerializableReader)Reader).RaiseRecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (target is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = target as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (_callbackRecordSeriablizable is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            return false;
        }
    }
}
