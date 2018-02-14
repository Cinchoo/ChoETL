using System;
using System.Collections;
using System.Collections.Generic;
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
        private IChoNotifyRecordRead _callbackRecord;
        private bool _configCheckDone = false;
        private Lazy<XmlSerializer> _se = null;
        internal ChoReader Reader = null;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlRecordReader(Type recordType, ChoXmlRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);

            //Configuration.Validate();
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            XmlReader sr = source as XmlReader;
            ChoGuard.ArgumentNotNull(sr, "XmlReader");

            if (!RaiseBeginLoad(sr))
                yield break;

            foreach (var item in AsEnumerable(sr.GetXmlElements(Configuration.XPath), TraceSwitch, filterFunc))
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

        bool _nsInitialized = false;
        private IEnumerable<object> AsEnumerable(IEnumerable<XElement> xElements, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, XElement> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool abortRequested = false;
            _se = new Lazy<XmlSerializer>(() => Configuration.XmlSerializer == null ? new XmlSerializer(RecordType) : Configuration.XmlSerializer);
            if (!Configuration.NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace())
            {
                _nsInitialized = true;
                Configuration.NamespaceManager.AddNamespace("x", Configuration.NamespaceManager.DefaultNamespace);
            }

            foreach (XElement el in xElements)
            {
                if (!_nsInitialized)
                {
                    _nsInitialized = true;
                    if (!Configuration.NamespaceManager.DefaultNamespace.IsNullOrWhiteSpace())
                    {
                        Configuration.NamespaceManager.AddNamespace("x", Configuration.NamespaceManager.DefaultNamespace);
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
                            if (skipUntil.Value)
                                skip = skipUntil;
                            else
                                skip = true;
                        }
                    }
                }
                if (skip == null)
                    break;
                if (skip.Value)
                    continue;

                if (!_configCheckDone)
                {
                    Configuration.Validate(pair);
                    var dict = Configuration.XmlRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
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
                AddProperty(parent, node.Name.ToString(), node.Value.Trim());
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
            if (!Configuration.UseXmlSerialization || Configuration.IsDynamicObject)
                rec = Configuration.IsDynamicObject ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : Activator.CreateInstance(RecordType);

            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping...");
                    rec = null;
                    return true;
                }

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }


                if (!Configuration.UseXmlSerialization)
                {
                    if (!FillRecord(rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);
                }
                else
                {
                    if (Configuration.IsDynamicObject)
                        Parse(rec, pair.Item2);
                    else
                    {
                        rec = _se.Value.Deserialize(pair.Item2.CreateReader());
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
                    throw;

                ChoETLFramework.HandleException(ref ex);
                if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                    rec = null;
                }
                else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                {
                    if (!RaiseRecordLoadError(rec, pair, ex))
                        throw;
                    else
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        rec = null;
                    }
                }
                else
                    throw;

                return true;
            }

            return true;
        }

        private void ToDictionary(XElement node)
        {
            Dictionary<string, List<string>> dictionary = xDict; // new Dictionary<string, List<string>>();
            xDict.Clear();
            //if (Configuration.IsComplexXPathUsed)
            //    return dictionary;

            string key = null;
            //get all child elements, skip parent nodes
            foreach (XElement elem in node.Elements().Where(a => !a.HasElements && !a.HasAttributes))
            {
                key = Configuration.GetNameWithNamespace(elem.Name);

                //avoid duplicates
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, new List<string>());

                dictionary[key].Add(elem.Value);
            }
            foreach (XAttribute elem in node.Attributes())
            {
                key = Configuration.GetNameWithNamespace(node.Name, elem.Name);

                //avoid duplicates
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, new List<string>());

                dictionary[key].Add(elem.Value);
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

            lineNo = pair.Item1;
            node = pair.Item2;

            fXElements = null;
            fieldValue = null;
            fieldConfig = null;
            pi = null;
            xpn = node.CreateNavigator(Configuration.NamespaceManager.NameTable);
            ToDictionary(node);

            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                if (fieldConfig.XPath == "text()")
                {
                    if (Configuration.GetNameWithNamespace(node.Name) == fieldConfig.FieldName)
                    {
                        object value = node;
                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref value))
                            continue;
                        if (fieldConfig.ValueConverter != null)
                            value = fieldConfig.ValueConverter(value);

                        fieldValue = value is XElement ? ((XElement)value).ToObjectFromXml(typeof(ChoDynamicObject)) : value;

                        //fieldValue = value is XElement ? node.Value : value;
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
                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref value))
                            continue;

                        if (fieldConfig.ValueConverter != null)
                            fieldValue = fieldConfig.ValueConverter(value);
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
                                            fieldValue = fieldConfig.ItemConverter(fXAttributes[0]);
                                        else
                                            fieldValue = fXAttributes[0].Value;
                                    }
                                    else
                                    {
                                        List<object> arr = new List<object>();
                                        foreach (var ele in fXAttributes)
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
                                    XAttribute fXElement = fXAttributes.FirstOrDefault();
                                    if (fXElement != null)
                                    {
                                        if (fieldConfig.ItemConverter != null)
                                            fieldValue = fieldConfig.ItemConverter(fXElement);
                                        else
                                            fieldValue = fXElement.Value;
                                    }
                                }
                                else if (fieldConfig.FieldType.IsCollection())
                                {
                                    List<object> list = new List<object>();
                                    Type itemType = fieldConfig.FieldType.GetItemType().GetUnderlyingType();

                                    foreach (var ele in fXAttributes)
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
                                    XAttribute fXElement = fXAttributes.FirstOrDefault();
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
                            else
                            {
                                fXElements = xNodes.OfType<XElement>().ToArray();

                                if (!fXElements.IsNullOrEmpty())
                                {
                                    if (fieldConfig.IsArray != null && fieldConfig.IsArray.Value)
                                    {
                                        List<object> list = new List<object>();
                                        Type itemType = fieldConfig.FieldType != null ? fieldConfig.FieldType.GetItemType().GetUnderlyingType() :
                                            typeof(ChoDynamicObject);

                                        foreach (var ele in fXElements)
                                        {
                                            if (fieldConfig.ItemConverter != null)
                                                list.Add(fieldConfig.ItemConverter(ele));
                                            else
                                            {
                                                if (itemType.IsSimple())
                                                    list.Add(ChoConvert.ConvertTo(ele.Value, itemType));
                                                else
                                                {
                                                    if (itemType == typeof(ChoDynamicObject))
                                                        list.Add(ele.ToDynamic());
                                                    else
                                                        list.Add(ele.ToObjectFromXml(itemType, GetXmlOverrides(fieldConfig)));
                                                }
                                            }
                                        }
                                        fieldValue = list.ToArray();
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
                                                    fieldValue = fXElements[0].ToObjectFromXml(typeof(ChoDynamicObject));
                                            }
                                            else
                                            {
                                                List<object> arr = new List<object>();
                                                foreach (var ele in fXElements)
                                                {
                                                    if (fieldConfig.ItemConverter != null)
                                                        arr.Add(fieldConfig.ItemConverter(ele));
                                                    else
                                                        arr.Add(ele.ToObjectFromXml(typeof(ChoDynamicObject)) as ChoDynamicObject);
                                                }

                                                fieldValue = arr.ToArray();
                                            }
                                        }
                                        else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                                        {
                                            XElement fXElement = fXElements.FirstOrDefault();
                                            if (fXElement != null)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    fieldValue = fieldConfig.ItemConverter(fXElement);
                                                else
                                                    fieldValue = fXElement.Value;
                                            }
                                        }
                                        else if (fieldConfig.FieldType.IsCollection())
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
                                                    list.Add(fieldConfig.ItemConverter(ele));
                                                else
                                                {
                                                    if (itemType.IsSimple())
                                                        list.Add(ChoConvert.ConvertTo(ele.Value, itemType));
                                                    else
                                                    {
                                                        list.Add(ele.ToObjectFromXml(itemType, null));
                                                    }
                                                }
                                            }

                                            fieldValue = list.ToArray();
                                        }
                                        else
                                        {
                                            XElement fXElement = fXElements.FirstOrDefault(); //.SelectMany(e => e.Elements()).FirstOrDefault();
                                            if (fXElement != null)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    fieldValue = fieldConfig.ItemConverter(fXElement);
                                                else
                                                {
                                                    fieldValue = fXElement.ToObjectFromXml(fieldConfig.FieldType, null);
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
                                            XText fXElement = xTexts.FirstOrDefault();
                                            if (fXElement != null)
                                            {
                                                if (fieldConfig.ItemConverter != null)
                                                    fieldValue = fieldConfig.ItemConverter(fXElement);
                                                else
                                                    fieldValue = fXElement.Value;
                                            }
                                        }
                                        else if (fieldConfig.FieldType.IsCollection())
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
                                    else if (Configuration.ColumnCountStrict)
                                        throw new ChoParserException("Missing '{0}' xml node.".FormatString(fieldConfig.FieldName));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (xDict[fieldConfig.FieldName].Count == 1)
                            fieldValue = xDict[fieldConfig.FieldName][0];
                        else
                            fieldValue = xDict[fieldConfig.FieldName];

                        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                            continue;

                        if (fieldConfig.ValueConverter != null)
                            fieldValue = fieldConfig.ValueConverter(fieldValue);
                    }
                }

                if (Configuration.IsDynamicObject)
                {
                    if (fieldValue != null && kvp.Value.FieldType == null && lineNo == 1)
                        kvp.Value.FieldType = fieldValue is ICollection ? fieldValue.GetType() : fieldValue.GetType().IsSimple() ? DiscoverFieldType(fieldValue as string, Configuration) : null;
                }
                else
                {
                    if (pi != null)
                    {
                        kvp.Value.FieldType = pi.PropertyType;
                    }
                    else
                        kvp.Value.FieldType = typeof(string);
                }

                if (fieldValue is string)
                    fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string, kvp.Value.EncodeValue);

                try
                {
                    bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        if (!fieldConfig.IsArray.CastTo<bool>())
                            dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else
                            dict[kvp.Key] = fieldValue;

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
                        if (pi != null)
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else
                            throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(kvp.Key, ChoType.GetTypeName(rec)));

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }

                    if (!RaiseAfterRecordFieldLoad(rec, pair.Item1, kvp.Key, fieldValue))
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
                        if (Configuration.IsDynamicObject)
                        {
                            var dict = rec as IDictionary<string, Object>;

                            if (dict.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (dict.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                    }
                    catch (Exception innerEx)
                    {
                        if (ex == innerEx.InnerException)
                        {
                            if (fieldConfig.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                            {
                                continue;
                            }
                            else
                            {
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                            }
                        }
                        else
                        {
                            throw new ChoReaderException("Failed to assign '{0}' fallback value to '{1}' field.".FormatString(fieldValue, fieldConfig.FieldName), innerEx);
                        }
                    }
                }
            }

            return true;
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

            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim;

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
                        fieldValue = fieldValue.Substring(0, config.Size.Value);
                }
            }

            if (encodeValue != null && !encodeValue.Value && fieldValue != null)
                return System.Net.WebUtility.HtmlEncode(fieldValue);

            return System.Net.WebUtility.HtmlDecode(fieldValue);
        }

        #region Event Raisers

        private bool RaiseBeginLoad(object state)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(state), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeginLoad(state), true);
            }
            return true;
        }

        private void RaiseEndLoad(object state)
        {
            if (_callbackRecord != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndLoad(state));
            }
            else if (Reader != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Reader.RaiseEndLoad(state));
            }
        }

        private bool? RaiseSkipUntil(Tuple<long, XElement> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackRecord.SkipUntil(index, state));

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseSkipUntil(index, state));

                return retValue;
            }
            return null;
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, XElement> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, XElement>(index, state as XElement);

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

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
            if (_callbackRecord != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            else if (Reader != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            skip = sp;
            return ret;
        }

        private bool RaiseRecordLoadError(object target, Tuple<long, XElement> pair, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (_callbackRecord != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (Reader != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, value, ex), false);
            }
            return true;
        }

        #endregion Event Raisers
    }
}
