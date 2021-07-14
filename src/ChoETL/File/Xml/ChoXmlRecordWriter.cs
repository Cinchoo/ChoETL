using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChoETL
{
    internal class ChoXmlRecordWriter : ChoRecordWriter
    {
        private IChoNotifyFileWrite _callbackFileWrite;
        private IChoNotifyRecordWrite _callbackRecordWrite;
        private IChoNotifyRecordFieldWrite _callbackRecordFieldWrite;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private IChoCustomNodeNameOverrideable _callbackCustomNodeNameOverrideable;
        private bool _configCheckDone = false;
        private long _index = 0;
        private Lazy<XmlSerializer> _se = null;
        private readonly Regex _beginNSTagRegex = new Regex(@"^(<\w+)\:(\w+)(.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _endNSTagRegex = new Regex(@"(.*)(</\w+)\:(\w+)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _beginTagRegex = new Regex(@"^(<\w+)(.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _endTagRegex = new Regex("(.*)(</.*>)$", RegexOptions.Compiled | RegexOptions.Multiline);
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        private Lazy<List<object>> _recBuffer = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;
        private int _indent = 0;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlRecordWriter(Type recordType, ChoXmlRecordConfiguration configuration) : base(recordType, true)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            _callbackCustomNodeNameOverrideable = ChoMetadataObjectCache.CreateMetadataObject<IChoCustomNodeNameOverrideable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            _recBuffer = new Lazy<List<object>>(() =>
            {
                if (Writer != null)
                {
                    var b = Writer.Context.ContainsKey("RecBuffer") ? Writer.Context.RecBuffer : null;
                    if (b == null)
                        Writer.Context.RecBuffer = new List<object>();

                    return Writer.Context.RecBuffer;
                }
                else
                    return new List<object>();
            }, true);
            //Configuration.Validate();

            BeginWrite = new Lazy<bool>(() =>
            {
                TextWriter sw = _sw as TextWriter;
                if (sw != null)
                    return RaiseBeginWrite(sw);

                return false;
            });
        }

        internal void EndWrite(object writer)
        {
            TextWriter sw = writer as TextWriter;

            try
            {
                if (_configCheckDone)
                {
                    if (!Configuration.RootName.IsNullOrWhiteSpace() && !Configuration.IgnoreRootName)
                    {
                        sw.Write("{1}{0}".FormatString(Indent(XmlNamespaceEndElementText(Configuration.XmlNamespaceManager.Value,
                            Configuration.RootName, Configuration.DefaultNamespacePrefix, Configuration.NS), _indent--), EOLDelimiter));

                        if (!Configuration.DocumentElements.IsNullOrEmpty())
                        {
                            foreach (var e in Configuration.DocumentElements.Reverse())
                            {
                                sw.Write(Indent("{0}<{1}>".FormatString(EOLDelimiter, e), _indent--));
                            }
                        }
                    }
                }
            }
            catch { }

            RaiseEndWrite(sw);
        }

        private string GetXmlDeclaration()
        {
            XmlDocument doc = new XmlDocument();
            return doc.CreateXmlDeclaration(Configuration.XmlVersion, Configuration.Encoding.WebName, null).OuterXml;
        }


        private IEnumerable<object> GetRecords(IEnumerator<object> records)
        {
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return Marshall(rec);

            while (records.MoveNext())
                yield return Marshall(records.Current);
        }

        Type marshallType = null;
        private object Marshall(object rec)
        {
            if (rec == null)
                return rec;

            if (rec.GetType().IsGenericType && rec.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return Activator.CreateInstance(typeof(ChoKeyValuePair<,>).MakeGenericType(rec.GetType().GetGenericArguments()), rec);
            }
            else
                return rec;
        }

        private object GetFirstNotNullRecord(IEnumerator<object> recEnum)
        {
            if (Writer.Context.FirstNotNullRecord != null)
                return Writer.Context.FirstNotNullRecord;

            while (recEnum.MoveNext())
            {
                _recBuffer.Value.Add(recEnum.Current);
                if (recEnum.Current != null)
                {
                    Writer.Context.FirstNotNullRecord = recEnum.Current;
                    return Writer.Context.FirstNotNullRecord;
                }
            }
            return null;
        }

        bool _firstElement = true;
        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            _sw = writer;
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (records == null) yield break;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            _se = new Lazy<XmlSerializer>(() => Configuration.XmlSerializer == null ? null : Configuration.XmlSerializer);

            string recText = String.Empty;
            if (!Configuration.OmitXmlDeclaration)
                sw.Write("{0}{1}", GetXmlDeclaration(), EOLDelimiter);

            try
            {
                var recEnum = records.GetEnumerator();
                object notNullRecord = GetFirstNotNullRecord(recEnum);
                if (notNullRecord == null)
                    yield break;

                foreach (object record in GetRecords(recEnum))
                {
                    _index++;

                    if (TraceSwitch.TraceVerbose)
                    {
                        if (record is IChoETLNameableObject)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(((IChoETLNameableObject)record).Name));
                        else
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(_index));
                    }

                    recText = String.Empty;
                    if (predicate == null || predicate(record))
                    {
                        //Discover and load Xml columns from first record
                        if (!_configCheckDone)
                        {
                            if (notNullRecord != null)
                            {
                                string[] fieldNames = null;
                                if (Configuration.RecordType == typeof(object))
                                {
                                    Type recordType = ElementType == null ? record.GetType() : ElementType;
                                    RecordType = Configuration.RecordType = recordType.GetUnderlyingType(); //.ResolveType();
                                    Configuration.IsDynamicObject = recordType.IsDynamicType();
                                }

                                if (typeof(IDictionary).IsAssignableFrom(Configuration.RecordType)
                                    || typeof(IList).IsAssignableFrom(Configuration.RecordType))
                                    Configuration.UseXmlSerialization = true;

                                if (!Configuration.IsDynamicObject)
                                {
                                    if (Configuration.XmlRecordFieldConfigurations.Count == 0)
                                        Configuration.MapRecordFields(Configuration.RecordType);
                                }

                                if (Configuration.IsDynamicObject)
                                {
                                    var dict = notNullRecord.ToDynamicObject() as IDictionary<string, Object>;
                                    fieldNames = dict.Keys.ToArray();
                                }
                                else
                                {
                                    fieldNames = ChoTypeDescriptor.GetProperties<ChoXmlNodeRecordFieldAttribute>(Configuration.RecordType).Select(pd => pd.Name).ToArray();
                                    if (fieldNames.Length == 0)
                                    {
                                        fieldNames = ChoType.GetProperties(Configuration.RecordType).Select(p => p.Name).ToArray();
                                    }
                                }

                                Configuration.Validate(fieldNames);

                                _configCheckDone = true;

                                if (!BeginWrite.Value)
                                    yield break;

                                bool first = true;
                                if (!Configuration.RootName.IsNullOrWhiteSpace() && !Configuration.IgnoreRootName)
                                {
                                    if (!Configuration.DocumentElements.IsNullOrEmpty())
                                    {
                                        foreach (var e in Configuration.DocumentElements)
                                        {
                                            if (first)
                                            {
                                                first = false;
                                                sw.Write("{0}".FormatString(XmlNamespaceStartElementText(Configuration.XmlNamespaceManager.Value,
                                                    e, Configuration.DefaultNamespacePrefix, Configuration.NS)));
                                            }
                                            else
                                            {
                                                sw.Write(Indent("{0}<{1}>".FormatString(EOLDelimiter, e), ++_indent));
                                            }
                                        }
                                    }

                                    if (first)
                                    {
                                        sw.Write("{0}".FormatString(XmlNamespaceStartElementText(Configuration.XmlNamespaceManager.Value,
                                            Configuration.RootName, Configuration.DefaultNamespacePrefix, Configuration.NS)));
                                    }
                                    else
                                    {
                                        sw.Write(Indent("{0}<{1}>".FormatString(EOLDelimiter, Configuration.RootName), ++_indent));
                                    }
                                }
                            }
                        }
                        //Check record 
                        if (record != null)
                        {
                            Type rt = record.GetType().ResolveType();
                            if (Configuration.IsDynamicObject)
                            {
                                if (ElementType != null)
                                {

                                }
                                else if (!rt.IsDynamicType())
                                    throw new ChoWriterException("Invalid record found.");
                            }
                            else
                            {
                                if (!Configuration.RecordType.IsAssignableFrom(rt) && !Configuration.UseXmlSerialization)
                                    throw new ChoWriterException("Invalid record found.");
                            }
                        }


                        if (!RaiseBeforeRecordWrite(record, _index, ref recText))
                            yield break;

                        if (recText == null)
                            continue;
                        else if (recText.Length > 0)
                        {
                            sw.Write(recText);
                            continue;
                        }

                        try
                        {
                            string eolDelimiter = null;
                            if (_firstElement)
                                _firstElement = false;
                            else
                                eolDelimiter = EOLDelimiter;

                            if (!Configuration.UseXmlSerialization)
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);

                                if (ToText(_index, record, Configuration, out recText))
                                {
                                    if (!recText.IsNullOrEmpty())
                                    {
                                        if (Configuration.IgnoreRootName)
                                        {
                                            sw.Write("{1}{0}", recText, eolDelimiter);
                                        }
                                        else
                                        {
                                            sw.Write("{1}{0}", Indent(recText, _indent + 1), EOLDelimiter);
                                        }
                                    }

                                    if (!RaiseAfterRecordWrite(record, _index, recText))
                                        yield break;
                                }
                            }
                            else
                            {
                                eolDelimiter = EOLDelimiter;

                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                                    record.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);

                                if (record != null)
                                {
                                    string innerXml1 = null;
                                    if (_se.Value != null)
                                    {
                                        XmlWriterSettings settings = new XmlWriterSettings();
                                        settings.OmitXmlDeclaration = true;
                                        settings.Indent = Configuration.Formatting == Formatting.Indented;
                                        settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;

                                        StringBuilder xml = new StringBuilder();
                                        using (XmlWriter xw = XmlWriter.Create(xml, settings))
                                        {
                                            _se.Value.Serialize(xw, record);
                                        }
                                        innerXml1 = xml.ToString();
                                    }
                                    else
                                    {
                                        innerXml1 = ChoUtility.XmlSerialize(record, null, eolDelimiter, Configuration.NullValueHandling, Configuration.DefaultNamespacePrefix, Configuration.EmitDataType,
                                            useXmlArray: Configuration.UseXmlArray, ns: Configuration.XmlNamespaceManager.Value.XmlSerializerNamespaces).RemoveXmlNamespaces();
                                    }

                                    if (!Configuration.IgnoreNodeName && !Configuration.NodeName.IsNullOrWhiteSpace())
                                    {
                                        innerXml1 = ReplaceXmlNodeIfAppl(innerXml1, Configuration.NodeName);
                                    }
                                    sw.Write(eolDelimiter);
                                    sw.Write(Indent(innerXml1, _indent + 1));

                                    if (!RaiseAfterRecordWrite(record, _index, null))
                                        yield break;
                                }
                            }
                        }
                        //catch (ChoParserException)
                        //{
                        //    throw;
                        //}
                        catch (Exception ex)
                        {
                            ChoETLFramework.HandleException(ref ex);
                            if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                            {
                                ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            }
                            else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                            {
                                if (!RaiseRecordWriteError(record, _index, recText, ex))
                                    throw;
                                else
                                {
                                    //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                }
                            }
                            else
                                throw;
                        }
                    }

                    yield return record;

                    if (Configuration.NotifyAfter > 0 && _index % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsWritten(_index))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                            yield break;
                        }
                    }
                }
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
            }
        }

        private string ReplaceXmlNodeIfAppl(string xml, string nodeName)
        {
            if (_beginNSTagRegex.Match(xml).Success)
            {
                xml = _beginNSTagRegex.Replace(xml, delegate (Match m)
                {
                    return m.Groups[1].Value + ":" + nodeName + m.Groups[3].Value;
                });
                xml = _endNSTagRegex.Replace(xml, delegate (Match m)
                {
                    return m.Groups[2].Value + "</{0}:{1}>".FormatString(m.Groups[1].Value, nodeName);
                });
            }
            else
            {
                if (Configuration.DefaultNamespacePrefix.IsNullOrWhiteSpace())
                {
                    xml = _beginTagRegex.Replace(xml, delegate (Match m)
                    {
                        return "<" + nodeName + m.Groups[2].Value;
                    });
                    xml = _endTagRegex.Replace(xml, delegate (Match m)
                    {
                        return m.Groups[1].Value + "</{0}>".FormatString(nodeName);
                    });
                }
                else
                {
                    xml = _beginTagRegex.Replace(xml, delegate (Match m)
                    {
                        return "<" + XmlNamespaceElementName(nodeName, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                    });
                    xml = _endTagRegex.Replace(xml, delegate (Match m)
                    {
                        return m.Groups[1].Value + "</{0}>".FormatString(XmlNamespaceElementName(nodeName, Configuration.DefaultNamespacePrefix));
                    });


                }
            }
            return xml;
        }

        private string Indent(string value, int indentValue = 1)
        {
            if (value == null)
                return value;

            return Configuration.Formatting == Formatting.Indented ? value.Indent(Configuration.Indent * indentValue, Configuration.IndentChar.ToString()) : value;
        }

        private string Unindent(string value, int indentValue = 1)
        {
            if (value == null)
                return value;

            return Configuration.Formatting == Formatting.Indented ? value.Unindent(Configuration.Indent * indentValue, Configuration.IndentChar.ToString()) : value;
        }

        private string EOLDelimiter
        {
            get
            {
                return Configuration.Formatting == Formatting.Indented ? Configuration.EOLDelimiter : String.Empty;
            }
        }

        private string GetNamespaceText()
        {
            if (Configuration.NamespaceManager == null)
                return null;

            StringBuilder nsText = new StringBuilder();
            foreach (var kvp in Configuration.XmlNamespaceManager.Value.NSDict)
            {
                if (kvp.Key == "xml")
                    continue;

                if (nsText.Length > 0)
                    nsText.Append(' ');

                nsText.Append($"xmlns:{kvp.Key}=\"{kvp.Value}\"");
            }

            if (nsText.Length == 0)
                return null;
            else
                return " " + nsText.ToString();
        }

        private IEnumerable<KeyValuePair<string, ChoXmlRecordFieldConfiguration>> GetOrderedKVP(ChoXmlRecordConfiguration config)
        {
            if (config.RecordFieldConfigurationsDict == null)
                yield break;

            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in config.RecordFieldConfigurationsDict)
            {
                if (kvp.Value.IsXmlAttribute)
                    yield return kvp;
            }
            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in config.RecordFieldConfigurationsDict)
            {
                if (!kvp.Value.IsXmlAttribute)
                    yield return kvp;
            }
        }

        private string GetNodeName(long index, object rec)
        {
            var newName = RaiseCustomeNodeNameOverride(index, rec);
            return newName.IsNullOrWhiteSpace() ? Configuration.NodeName : newName;
        }

        private bool ToText(long index, object rec, ChoXmlRecordConfiguration config, out string recText)
        {
            if (typeof(IChoScalarObject).IsAssignableFrom(config.RecordType) && rec != null)
                rec = ChoActivator.CreateInstance(config.RecordType, rec);

            recText = null;
            if (rec == null)
            {
                if (config.NullValueHandling == ChoNullValueHandling.Ignore)
                    return false;
                else if (config.NullValueHandling == ChoNullValueHandling.Default)
                    rec = ChoActivator.CreateInstance(config.RecordType);
                else if (config.NullValueHandling == ChoNullValueHandling.Empty)
                {
                    recText = Indent(@"<{0}/>".FormatString(XmlNamespaceElementName(GetNodeName(index, rec), Configuration.DefaultNamespacePrefix)), _indent + 1);
                    return true;
                }
                else
                {
                    recText = Indent(@"<{0} xmlns:xsi=""{1}"" xsi:nil=""true"" />".FormatString(XmlNamespaceElementName(GetNodeName(index, rec), Configuration.DefaultNamespacePrefix), ChoXmlSettings.XmlSchemaInstanceNamespace
                                ), _indent + 1);
                    return true;
                }
            }

            StringBuilder msg = new StringBuilder();
            object fieldValue = null;
            string fieldText = null;
            ChoXmlRecordFieldConfiguration fieldConfig = null;

            if (config.ColumnCountStrict)
                CheckColumnsStrict(rec);

            //bool firstColumn = true;
            PropertyInfo pi = null;
            object rootRec = rec;

            Dictionary<string, object> attrs = new Dictionary<string, object>();
            Dictionary<string, object> elems = new Dictionary<string, object>();
            HashSet<string> CDATAs = new HashSet<string>();
            var useXmlArray = false;

            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in GetOrderedKVP(config))
            {
                //if (config.IsDynamicObject)
                //{
                if (Configuration.IgnoredFields.Contains(kvp.Key))
                    continue;
                //}

                fieldConfig = kvp.Value;
                fieldValue = null;
                fieldText = String.Empty;
                if (config.PIDict != null)
                    config.PIDict.TryGetValue(kvp.Key, out pi);

                if (fieldConfig.IsArray == null)
                    useXmlArray = Configuration.UseXmlArray;
                else
                    useXmlArray = fieldConfig.IsArray.Value;

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                if (config.ThrowAndStopOnMissingField)
                {
                    if (config.IsDynamicObject)
                    {
                        var dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                        if (!dict.ContainsKey(kvp.Key))
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Xml node.".FormatString(fieldConfig.FieldName));
                    }
                    else
                    {
                        if (pi == null)
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Xml node.".FormatString(fieldConfig.FieldName));
                    }
                }

                try
                {
                    if (config.IsDynamicObject)
                    {
                        IDictionary<string, Object> dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                        fieldValue = dict[kvp.Key]; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
                        if (kvp.Value.FieldType == null)
                        {
                            if (rec is ChoDynamicObject)
                            {
                                var dobj = rec as ChoDynamicObject;
                                kvp.Value.FieldType = dobj.GetMemberType(kvp.Key);
                            }

                            if (kvp.Value.FieldType == null)
                            {
                                if (ElementType == null)
                                    kvp.Value.FieldType = typeof(object);
                                else
                                    kvp.Value.FieldType = ElementType;
                            }
                        }
                        else if (kvp.Value.FieldType == typeof(object))
                        {
                            if (rec is ChoDynamicObject)
                            {
                                var dobj = rec as ChoDynamicObject;
                                var ft = dobj.GetMemberType(kvp.Key);
                                if (ft != null)
                                    kvp.Value.FieldType = ft;
                            }
                        }
                    }
                    else
                    {
                        if (pi != null)
                        {
                            fieldValue = ChoType.GetPropertyValue(rec, pi);
                            if (kvp.Value.FieldType == null)
                                kvp.Value.FieldType = pi.PropertyType;
                        }
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    //Discover default value, use it if null
                    //if (fieldValue == null)
                    //{
                    //    if (fieldConfig.IsDefaultValueSpecified)
                    //        fieldValue = fieldConfig.DefaultValue;
                    //}
                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (!RaiseBeforeRecordFieldWrite(rec, index, kvp.Key, ref fieldValue))
                        return false;

                    if (fieldConfig.ValueConverter != null)
                        fieldValue = fieldConfig.ValueConverter(fieldValue);
                    else
                        rec.GetNConvertMemberValue(kvp.Key, kvp.Value, config.Culture, ref fieldValue, true);

                    if ((config.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.MemberLevel)
                        rec.DoMemberLevelValidation(kvp.Key, kvp.Value, config.ObjectValidationMode, fieldValue);

                    if (!RaiseAfterRecordFieldWrite(rec, index, kvp.Key, fieldValue))
                        return false;
                }
                catch (ChoParserException)
                {
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    if (config.ThrowAndStopOnMissingField)
                        throw;
                }
                catch (Exception ex)
                {
                    ChoETLFramework.HandleException(ref ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw;

                    try
                    {
                        if (config.IsDynamicObject)
                        {
                            var dict = rec.ToDynamicObject() as IDictionary<string, Object>;

                            if (dict.GetFallbackValue(kvp.Key, kvp.Value, config.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, config.ObjectValidationMode, fieldValue);
                            else if (dict.GetDefaultValue(kvp.Key, kvp.Value, config.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, config.ObjectValidationMode, fieldValue);
                            else
                            {
                                var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                                fieldValue = null;
                                throw ex1;
                            }
                        }
                        else if (pi != null)
                        {
                            if (rec.GetFallbackValue(kvp.Key, kvp.Value, config.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, config.ObjectValidationMode);
                            else if (rec.GetDefaultValue(kvp.Key, kvp.Value, config.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, config.ObjectValidationMode, fieldValue);
                            else
                            {
                                var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                                fieldValue = null;
                                throw ex1;
                            }
                        }
                        else
                        {
                            var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                            fieldValue = null;
                            throw ex1;
                        }
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
                                if (!RaiseRecordFieldWriteError(rec, index, kvp.Key, ref fieldValue, ex))
                                    throw new ChoWriterException($"Failed to write '{fieldValue}' value of '{kvp.Key}' member.", ex);
                            }
                        }
                        else
                        {
                            throw new ChoWriterException("Failed to use '{0}' fallback value for '{1}' member.".FormatString(fieldValue, kvp.Key), innerEx);
                        }
                    }
                }

                if (fieldConfig.CustomSerializer != null)
                {
                    recText = fieldConfig.CustomSerializer(fieldValue) as string;
                    //recText = Indent(recText);
                    return true;
                }

                if (RaiseRecordFieldSerialize(rec, index, kvp.Key, ref fieldValue) && fieldValue is string)
                {
                    recText = fieldValue as string;
                    //recText = Indent(recText);
                    return true;
                }

                if (fieldValue == null && fieldConfig.NullValue != null)
                    fieldValue = fieldConfig.NullValue;

                if (!fieldConfig.IsAnyXmlNode)
                {
                    if (fieldConfig.IsXmlAttribute)
                    {
                        attrs.Add(fieldConfig.FieldName, fieldValue);
                    }
                    else
                    {
                        elems.Add(fieldConfig.FieldName, fieldValue);

                        if (fieldConfig.IsXmlCDATA)
                            CDATAs.Add(fieldConfig.FieldName);
                    }
                }
                else
                {
                    if (fieldValue == null || fieldValue.GetType().IsSimple())
                        attrs.Add(fieldConfig.FieldName, fieldValue);
                    else
                        elems.Add(fieldConfig.FieldName, fieldValue);

                    if (fieldConfig.IsXmlCDATA)
                        CDATAs.Add(fieldConfig.FieldName);
                }
            }

            string nodeName = GetNodeName(index, rec);
            if (rec is ChoDynamicObject && ((ChoDynamicObject)rec).DynamicObjectName != ChoDynamicObject.DefaultName)
            {
                ChoDynamicObject dobj = rec as ChoDynamicObject;
                nodeName = dobj.DynamicObjectName;
            }

            var nsMgr = Configuration.XmlNamespaceManager.Value;
            XNamespace ns = Configuration.NS;
            XElement ele = null; // NewXElement(nsMgr, nodeName); //, Configuration.DefaultNamespacePrefix, ns);
            string innerXml1 = null;
            if (typeof(IChoScalarObject).IsAssignableFrom(config.RecordType))
            {
                ele = NewXElement(nsMgr, nodeName, Configuration.DefaultNamespacePrefix, Configuration.NS, elems.First().Value);
            }
            else
            {
                ele = NewXElement(nsMgr, nodeName, Configuration.DefaultNamespacePrefix, Configuration.NS);
                foreach (var kvp in attrs)
                {
                    object value = kvp.Value;
                    if (value == null)
                    {
                        if (config.NullValueHandling == ChoNullValueHandling.Ignore)
                            continue;
                        else
                            value = String.Empty;
                    }

                    ele.Add(new XAttribute(kvp.Key, value));
                }
                foreach (var kvp in elems.Where(e => e.Key.StartsWith("@")))
                {
                    object value = kvp.Value;
                    if (value == null)
                    {
                        if (config.NullValueHandling == ChoNullValueHandling.Ignore)
                            continue;
                        else
                            value = String.Empty;
                    }

                    ele.Add(new XAttribute(kvp.Key.Replace("@", ""), value));
                }
                foreach (var kvp in elems.Where(e => !e.Key.StartsWith("@")))
                {
                    if (kvp.Value == null)
                    {
                        string innerXml = null;
                        if (config.NullValueHandling == ChoNullValueHandling.Ignore)
                        {
                            continue;
                        }
                        else if (config.NullValueHandling == ChoNullValueHandling.Default)
                        {
                            rec = ChoActivator.CreateInstance(config.RecordType);
                            innerXml = ChoUtility.XmlSerialize(rec, null, EOLDelimiter, Configuration.NullValueHandling, Configuration.DefaultNamespacePrefix, Configuration.EmitDataType,
                                useXmlArray: useXmlArray, ns: Configuration.XmlNamespaceManager.Value.XmlSerializerNamespaces);

                            innerXml1 = ReplaceXmlNodeIfAppl(innerXml1, kvp.Key);

                            //if (_beginNSTagRegex.Match(innerXml1).Success)
                            //{
                            //    innerXml = _beginNSTagRegex.Replace(innerXml, delegate (Match m)
                            //    {
                            //        return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            //    });
                            //}
                            //else
                            //{
                            //    innerXml = _beginTagRegex.Replace(innerXml, delegate (Match m)
                            //    {
                            //        return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            //    });
                            //}
                            //innerXml = _endTagRegex.Replace(innerXml, delegate (Match thisMatch)
                            //{
                            //    return "</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix));
                            //});
                        }
                        else if (config.NullValueHandling == ChoNullValueHandling.Empty)
                        {
                            innerXml = @"<{0}/>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix));
                        }
                        else
                        {
                            innerXml = @"<{0} xmlns:xsi=""{1}"" xsi:nil=""true"" />".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix), ChoXmlSettings.XmlSchemaInstanceNamespace);
                        }
                        ele.Add(XElement.Parse(innerXml));

                    }
                    else if (kvp.Value.GetType().IsSimple())
                    {
                        var isCDATA = CDATAs.Contains(kvp.Key);

                        if (ElementType == null)
                        {
                            var name = Configuration.TurnOffAutoCorrectXNames ? kvp.Key : kvp.Key.ToValidVariableName();
                            if (kvp.Value is ChoCDATA)
                                ele.Add(ns != null ? new XElement(ns + name, new XCData(((ChoCDATA)kvp.Value).Value)) : new XElement(kvp.Key, new XCData(((ChoCDATA)kvp.Value).Value)));
                            else if (isCDATA)
                                ele.Add(ns != null ? new XElement(ns + name, new XCData(kvp.Value.ToNString())) : new XElement(kvp.Key, new XCData(kvp.Value.ToNString())));
                            else
                            {
                                XElement e = NewXElement(nsMgr, kvp.Key, Configuration.DefaultNamespacePrefix, Configuration.NS, kvp.Value, Configuration.EmitDataType);
                                //var e = ns != null ? new XElement(ns + kvp.Key, kvp.Value) : new XElement(kvp.Key, kvp.Value);
                                ele.Add(e);
                            }
                        }
                        else if (isCDATA)
                        {
                            ele.Add(new XCData(kvp.Value.ToNString()));
                        }
                        else
                            ele.Value = kvp.Value.ToNString();
                    }
                    else
                    {
                        innerXml1 = ChoUtility.XmlSerialize(kvp.Value, null, EOLDelimiter, Configuration.NullValueHandling, Configuration.DefaultNamespacePrefix, Configuration.EmitDataType,
                            useXmlArray: useXmlArray, ns: Configuration.XmlNamespaceManager.Value.XmlSerializerNamespaces);

                        if (!kvp.Value.GetType().IsArray && !typeof(IList).IsAssignableFrom(kvp.Value.GetType()))
                        {
                            innerXml1 = ReplaceXmlNodeIfAppl(innerXml1, kvp.Key);

                            //if (_beginNSTagRegex.Match(innerXml1).Success)
                            //{
                            //    innerXml1 = _beginNSTagRegex.Replace(innerXml1, delegate (Match m)
                            //    {
                            //        return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            //    });
                            //}
                            //else
                            //{
                            //    innerXml1 = _beginTagRegex.Replace(innerXml1, delegate (Match m)
                            //    {
                            //        return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            //    });
                            //}
                            //innerXml1 = _endTagRegex.Replace(innerXml1, delegate (Match m)
                            //{
                            //    return m.Groups[1].Value + "</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix));
                            //});
                        }
                        else
                        {
                            var en = kvp.Key.ToSingular();
                            var eleName1 = GetElementName(innerXml1);

                            if (!eleName1.IsNullOrWhiteSpace())
                            {
                                innerXml1 = Regex.Replace(innerXml1, $"<{eleName1}", $"<{en}");
                                innerXml1 = Regex.Replace(innerXml1, $"</{eleName1}", $"</{en}");
                            }

                            if (useXmlArray)
                            {
                                string eleName = XmlNamespaceElementName(kvp.Key.ToSingular(), Configuration.DefaultNamespacePrefix);
                                innerXml1 = innerXml1.Replace("<dynamic>", "<{0}>".FormatString(eleName));
                                innerXml1 = innerXml1.Replace("</dynamic>", "</{0}>".FormatString(eleName));

                                if (eleName == kvp.Key)
                                    innerXml1 = "<{0}>{1}</{0}>".FormatString(XmlNamespaceElementName(kvp.Key.ToPlural(), Configuration.DefaultNamespacePrefix), innerXml1);
                                else
                                    innerXml1 = "<{0}>{1}</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix), innerXml1);
                            }
                        }
                        if (EOLDelimiter != null)
                            ele.Add(new XText(EOLDelimiter));
                        ele.Add(ParseElement(innerXml1, Configuration.NamespaceManager, Configuration.DefaultNamespacePrefix, ns));
                    }
                }
            }

            if (config.IgnoreNodeName)
            {
                innerXml1 = ele.InnerXML();
                //if (_beginNSTagRegex.Match(innerXml1).Success)
                //{
                //    innerXml1 = _beginNSTagRegex.Replace(innerXml1, delegate (Match m)
                //    {
                //        return null;
                //    });
                //}
                //else
                //{
                //    innerXml1 = _beginTagRegex.Replace(innerXml1, delegate (Match m)
                //    {
                //        return String.Empty;
                //    });
                //}

                //innerXml1 = _endTagRegex.Replace(innerXml1, delegate (Match thisMatch)
                //{
                //    return null;
                //});

                //try
                //{
                //    innerXml1 = XElement.Parse(innerXml1).ToString();
                //}
                //catch { }
            }
            else
                innerXml1 = config.Formatting == Formatting.Indented ? ele.ToString(SaveOptions.OmitDuplicateNamespaces) : ele.ToString(SaveOptions.OmitDuplicateNamespaces | SaveOptions.DisableFormatting);

            innerXml1 = Regex.Replace(innerXml1, @"^\s*$\n", "", RegexOptions.Multiline).TrimEnd();

            if (config.Formatting == Formatting.Indented)
            {
                if (!config.TurnOffXmlFormatting)
                    innerXml1 = FormatXml(innerXml1);
            }
            //recText = config.IgnoreRootName ? innerXml1 : Indent(innerXml1, config.Indent);
            recText = innerXml1;

            //Remove namespaces
            recText = recText.RemoveXmlNamespaces(); // Regex.Replace(recText, @"\sxmlns[^""]+""[^""]+""", String.Empty);

            return true;
        }

        private string GetElementName(string xml)
        {
            Regex regEx = new Regex(@"<(\w+)");
            var match = regEx.Match(xml);
            return !match.Success ? null : match.Groups[1].Value;
        }

        private string XmlNamespaceElementName(string name, string nsPrefix = null, XNamespace xs = null)
        {
            if (xs == null)
            {
                return "{0}".FormatString(name);
            }
            else
            {
                return "{0}".FormatString(name);
            }
        }

        private string XmlNamespaceEndElementText(ChoXmlNamespaceManager nsMgr, string name,
            string defaultNSPrefix, XNamespace NS)
        {
            string prefix = name.Contains(":") ? name.SplitNTrim(":").First() : null;
            name = name.Contains(":") ? name.SplitNTrim(":").Skip(1).First() : name;

            XNamespace ns = null;
            if (prefix != null)
                ns = nsMgr.GetNamespaceForPrefix(prefix);

            if (prefix == null)
            {
                if (defaultNSPrefix.IsNullOrWhiteSpace())
                    return "</{0}>".FormatString(name);
                else
                    return "</{0}:{1}>".FormatString(defaultNSPrefix, name);
            }
            else
            {
                if (ns == null)
                    throw new ChoParserException($"Missing namespace for '{prefix}' prefix.");

                return @"</{0}:{1}>".FormatString(prefix, name);
            }

        }

        private string XmlNamespaceStartElementText(ChoXmlNamespaceManager nsMgr, string name,
            string defaultNSPrefix, XNamespace NS)
        {
            string prefix = name.Contains(":") ? name.SplitNTrim(":").First() : null;
            name = name.Contains(":") ? name.SplitNTrim(":").Skip(1).First() : name;

            XNamespace ns = null;
            if (prefix != null)
                ns = nsMgr.GetNamespaceForPrefix(prefix);

            if (prefix == null)
            {
                if (defaultNSPrefix.IsNullOrWhiteSpace())
                    return "<{0}{1}>".FormatString(name, nsMgr.ToString(Configuration));
                else
                    return "<{0}:{1}{2}>".FormatString(defaultNSPrefix, name, nsMgr.ToString(Configuration));
            }
            else
            {
                if (ns == null)
                    throw new ChoParserException($"Missing namespace for '{prefix}' prefix.");

                return @"<{0}:{1}{2}>".FormatString(prefix, name, nsMgr.ToString(Configuration));
            }
        }

        private string XmlNamespaceElementName(string name, string nsPrefix = null)
        {
            string prefix = name.Contains(":") ? name.SplitNTrim(":").First() : null;
            name = name.Contains(":") ? name.SplitNTrim(":").Skip(1).First() : name;

            if (prefix == null)
            {
                if (nsPrefix.IsNullOrWhiteSpace())
                {
                    return name;
                }
                else
                {
                    return @"{0}:{1}".FormatString(nsPrefix, name);
                }
            }
            else
            {
                return name;
            }
        }

        private XElement NewXElement(ChoXmlNamespaceManager nsMgr, string name, string defaultNSPrefix, XNamespace NS, object value = null, bool emitType = false,
            bool? isXmlValue = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(nsMgr, nameof(nsMgr));

            string prefix = name.Contains(":") ? name.SplitNTrim(":").First() : null;
            name = name.Contains(":") ? name.SplitNTrim(":").Skip(1).First() : name;

            name = Configuration.TurnOffAutoCorrectXNames ? name : name.ToValidVariableName();

            XElement e = null;
            XNamespace ns = null;
            if (isXmlValue == null && value is string)
            {
                string t = value as string;
                if (t.StartsWith("<![CDATA[") && t.EndsWith("]]>"))
                    value = t = t.Replace("<![CDATA[", "").Replace("]]>", "");
                if (t.StartsWith("<") && t.EndsWith(">") && !t.StartsWith("<![CDATA["))
                    isXmlValue = true;
                else
                    isXmlValue = false;
            }
            if (isXmlValue == null)
                isXmlValue = false;

            if (prefix == null)
            {
                if (defaultNSPrefix.IsNullOrWhiteSpace())
                {
                    e = value == null ? new XElement(name) : !isXmlValue.Value ? new XElement(name, value) : new XElement(name, XElement.Parse(value.ToNString()));
                }
                else
                {
                    prefix = defaultNSPrefix;
                    ns = nsMgr.GetNamespaceForPrefix(defaultNSPrefix);
                    if (ns == null)
                        throw new ChoParserException($"Missing namespace for '{defaultNSPrefix}' prefix.");
                    e = value == null ? new XElement(ns + name) : !isXmlValue.Value ? new XElement(ns + name, value) : new XElement(ns + name, XElement.Parse(value.ToNString()));
                }
            }
            else
            {
                ns = nsMgr.GetNamespaceForPrefix(prefix);
                if (ns == null)
                    throw new ChoParserException($"Missing namespace for '{prefix}' prefix.");

                e = value == null ? new XElement(ns + name) : !isXmlValue.Value ? new XElement(ns + name, value) : new XElement(ns + name, XElement.Parse(value.ToNString()));
            }

            if (ns != null)
            {
                var nsAttr = new XAttribute(XNamespace.Xmlns + prefix, ns);
                e.Add(nsAttr);
                e.Name = ns + e.Name.LocalName;
            }

            if (emitType && value != null && value.GetType().IsSimple())
            {
                e.AddXSTypeAttribute(value, Configuration.XmlSchemaNamespace);
            }

            return e;
        }

        private XElement[] ParseElement(string strXml, XmlNamespaceManager mngr, string nsPrefix = null, XNamespace xs = null)
        {
            XmlParserContext parserContext = new XmlParserContext(null, mngr, null, XmlSpace.None);
            XElement[] es = null;

            try
            {
                //strXml = strXml.Replace("<ecls:issuer>", @"<ecls:issuer xmlns:ecls=""https://www.aade.gr/myDATA/incomeClassificaton/v1.0"">");
                var txtReader = new XmlTextReader(strXml, XmlNodeType.Element, parserContext);
                es = new XElement[] { XElement.Load(txtReader) };
            }
            catch
            {
                XmlTextReader txtReader = new XmlTextReader($"<root>{strXml}</root>", XmlNodeType.Element, parserContext);
                es = XElement.Load(txtReader).Elements().ToArray();
            }
            if (xs != null)
            {
                foreach (var e in es)
                {
                    try
                    {
                        var nsAttr = new XAttribute(XNamespace.Xmlns + nsPrefix, xs);
                        e.Add(nsAttr);
                    }
                    catch { }
                    foreach (XElement ce in e.DescendantsAndSelf())
                        ce.Name = xs + ce.Name.LocalName;
                    //if (!Configuration.IgnoreRootName)
                    //{
                    //	e.Attribute(XNamespace.Xmlns + nsPrefix).Remove();
                    //}
                }
            }
            return es;
        }

        private string FormatXml(string xml)
        {
            try
            {
                return XElement.Parse(xml).ToString();
            }
            catch
            {
                try
                {
                    return XElement.Parse($"<root>{xml}</root>").ToString(SaveOptions.OmitDuplicateNamespaces)
                        .Replace($"<root>{EOLDelimiter}", null).Replace($"{EOLDelimiter}</root>", null)
                        .Replace($"<root>", null).Replace($"</root>", null)
                        .Unindent(2, " ");
                }
                catch
                {

                }
                return xml;
            }
            // Format the XML text.
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;

            XmlDocument xml_document = new XmlDocument();
            xml_document.LoadXml(xml);
            xml_document.WriteTo(xw);

            // Display the result.
            return sw.ToString();
        }
        //private string SerializeObject(string propName, object value, Attribute[] attrs)
        //{
        //    ChoXmlRecordConfiguration config = null;

        //    string recText = null;
        //    if (value is IList)
        //    {
        //        Type itemType = value.GetType().GetItemType().GetUnderlyingType();
        //        if (!itemType.IsSimple())
        //        {
        //            config = new ChoXmlRecordConfiguration(value.GetType().GetItemType().GetUnderlyingType());
        //            config.Validate(null);
        //        }
        //        else
        //            config = Configuration;

        //        string arrElementName = propName;
        //        string itemName = propName.ToSingular();

        //        StringBuilder msg = new StringBuilder();

        //        msg.AppendFormat("<{0}>", arrElementName);
        //        foreach (var item in (IList)value)
        //        {
        //            if (itemType.IsSimple())
        //            {
        //                recText = "{2}<{0}>{1}</{0}>".FormatString(itemName, item.ToString(), config.EOLDelimiter);
        //            }
        //            else
        //                ToText(0, item, config, out recText);

        //            msg.Append(Indent(recText));
        //        }
        //        msg.AppendFormat("{1}</{0}>", arrElementName, config.EOLDelimiter);

        //        recText = msg.ToString();
        //    }
        //    else if (value is IDictionary)
        //    {
        //        Type[] arguments = value.GetType().GetGenericArguments();
        //        Type keyType = arguments[0].GetUnderlyingType();
        //        Type valueType = arguments[1].GetUnderlyingType();
        //        ChoXmlRecordConfiguration keyConfig = Configuration;
        //        ChoXmlRecordConfiguration valueConfig = Configuration;
        //        if (!keyType.IsSimple())
        //        {
        //            config = new ChoXmlRecordConfiguration(keyType);
        //            config.Validate(null);
        //        }
        //        if (!valueType.IsSimple())
        //        {
        //            config = new ChoXmlRecordConfiguration(valueType);
        //            config.Validate(null);
        //        }

        //        string arrElementName = propName;
        //        string itemName = propName.ToSingular();
        //        string keyElementName = "Key";
        //        string valueElementName = "Value";

        //        StringBuilder msg = new StringBuilder();

        //        msg.AppendFormat("<{0}>", arrElementName);
        //        foreach (var key in ((IDictionary)value).Keys)
        //        {
        //            if (keyType.IsSimple())
        //            {
        //                recText = "{2}<{0}>{1}</{0}>".FormatString(keyElementName, key.ToString(), config.EOLDelimiter);
        //            }
        //            else
        //            {
        //                ToText(0, key, config, out recText);
        //                recText = "{0}<{1}>{0}{2}{0}</{1}>".FormatString(config.EOLDelimiter, keyElementName, recText.Indent());
        //            }

        //            msg.Append(Indent(recText));

        //            object dictValue = ((IDictionary)value)[key];
        //            if (valueType.IsSimple())
        //            {
        //                recText = "<{0}>{1}</{0}>{2}".FormatString(valueElementName, dictValue.ToString(), config.EOLDelimiter);
        //            }
        //            else
        //            {
        //                ToText(0, dictValue, config, out recText);
        //                recText = Indent("<{1}>{0}{2}{0}</{1}>".FormatString(config.EOLDelimiter, valueElementName, recText), config.Indent);
        //            }
        //            msg.Append(recText);
        //        }
        //        msg.AppendFormat("{1}</{0}>", arrElementName, config.EOLDelimiter);

        //        recText = msg.ToString();

        //    }
        //    else
        //    {
        //        config = new ChoXmlRecordConfiguration(value.GetType().GetUnderlyingType());
        //        config.Validate(null);

        //        ToText(0, value, config, out recText);
        //    }
        //    if (config != null)
        //        return Unindent(recText, config.Indent);
        //    else
        //        return recText;
        //}

        private ChoFieldValueJustification GetFieldValueJustification(ChoFieldValueJustification? fieldValueJustification, Type fieldType)
        {
            return fieldValueJustification == null ? ChoFieldValueJustification.None : fieldValueJustification.Value;
        }

        private char GetFillChar(char? fillChar, Type fieldType)
        {
            return fillChar == null ? ' ' : fillChar.Value;
        }

        private void CheckColumnsStrict(object rec)
        {
            if (Configuration.IsDynamicObject)
            {
                var eoDict = rec == null ? new Dictionary<string, object>() : rec.ToDynamicObject() as IDictionary<string, Object>;

                if (eoDict.Count != Configuration.XmlRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.XmlRecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.XmlRecordFieldConfigurations.Select(v => v.Name).Except(eoDict.Keys).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = rec == null ? new PropertyDescriptor[] { } : ChoTypeDescriptor.GetProperties<ChoXmlNodeRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.XmlRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.XmlRecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.XmlRecordFieldConfigurations.Select(v => v.Name).Except(pds.Select(pd => pd.Name)).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
        }

        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool? quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false, bool isXmlAttribute = false,
            bool? encodeValue = null, ChoFieldValueTrimOption? fieldValueTrimOption = null)
        {
            string lFieldValue = fieldValue;
            bool retValue = false;

            if (retValue)
                return lFieldValue;

            if (fieldValue.IsNull())
                fieldValue = String.Empty;

            if (quoteField == null || !quoteField.Value)
            {
                if (fieldValue.StartsWith("\"") && fieldValue.EndsWith("\""))
                {

                }
                else
                {
                    if (!EOLDelimiter.IsNullOrEmpty() && fieldValue.Contains(EOLDelimiter))
                    {
                        if (isHeader)
                            throw new ChoParserException("Field header '{0}' value contains EOL delimiter character.".FormatString(fieldName));
                        else
                            fieldValue = "\"{0}\"".FormatString(fieldValue);
                    }
                }
            }
            else
            {
                if (fieldValue.StartsWith("\"") && fieldValue.EndsWith("\""))
                {

                }
                else
                {
                    fieldValue = "\"{0}\"".FormatString(fieldValue);
                }
            }

            if (size != null)
            {
                if (fieldValue.Length < size.Value)
                {
                    if (fillChar != ChoCharEx.NUL)
                    {
                        if (fieldValueJustification == ChoFieldValueJustification.Right)
                            fieldValue = fieldValue.PadLeft(size.Value, fillChar);
                        else if (fieldValueJustification == ChoFieldValueJustification.Left)
                            fieldValue = fieldValue.PadRight(size.Value, fillChar);
                    }
                }
                else if (fieldValue.Length > size.Value)
                {
                    if (truncate)
                    {
                        if (fieldValueTrimOption != null)
                        {
                            if (fieldValueTrimOption == ChoFieldValueTrimOption.TrimStart)
                                fieldValue = fieldValue.Right(size.Value);
                            else
                                fieldValue = fieldValue.Substring(0, size.Value);
                        }
                        else
                            fieldValue = fieldValue.Substring(0, size.Value);
                    }
                    else
                    {
                        if (isHeader)
                            throw new ApplicationException("Field header value length overflowed for '{0}' member [Expected: {1}, Actual: {2}].".FormatString(fieldName, size, fieldValue.Length));
                        else
                            throw new ApplicationException("Field value length overflowed for '{0}' member [Expected: {1}, Actual: {2}].".FormatString(fieldName, size, fieldValue.Length));
                    }
                }
            }

            if (fieldValue.StartsWith("<![CDATA["))
                return fieldValue;

            if (encodeValue != null && !encodeValue.Value)
                return fieldValue;

            return System.Net.WebUtility.HtmlEncode(fieldValue);
        }

        #region Event Raisers

        private bool RaiseBeginWrite(object state)
        {
            if (Writer != null && Writer.HasBeginWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeginWrite(state), true);
            }
            else if (_callbackFileWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFileWrite.BeginWrite(state), true);
            }
            return true;
        }

        private void RaiseEndWrite(object state)
        {
            if (Writer != null && Writer.HasEndWriteSubscribed)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseEndWrite(state));
            }
            else if (_callbackFileWrite != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackFileWrite.EndWrite(state));
            }
        }

        private bool RaiseBeforeRecordWrite(object target, long index, ref string state)
        {
            if (Writer != null && Writer.HasBeforeRecordWriteSubscribed)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            else if (_callbackRecordWrite != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.BeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordWrite(object target, long index, string state)
        {
            if (Writer != null && Writer.HasAfterRecordWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordWrite(target, index, state), true);
            }
            else if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.AfterRecordWrite(target, index, state), true);
            }
            return true;
        }

        private bool RaiseRecordWriteError(object target, long index, string state, Exception ex)
        {
            if (Writer != null && Writer.HasRecordWriteErrorSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordWriteError(target, index, state, ex), false);
            }
            else if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.RecordWriteError(target, index, state, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (Writer != null && Writer.HasBeforeRecordFieldWriteSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (_callbackRecordFieldWrite != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            if (Writer != null && Writer.HasAfterRecordFieldWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (_callbackRecordFieldWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = true;
            object state = value;

            if (Writer != null && Writer.HasRecordFieldWriteErrorSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).RecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (_callbackRecordFieldWrite != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.RecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            return retValue;
        }

        #endregion Event Raisers

        private bool RaiseRecordFieldSerialize(object target, long index, string propName, ref object value)
        {
            if (Writer is IChoSerializableWriter && ((IChoSerializableWriter)Writer).HasRecordFieldSerializeSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoSerializableWriter)Writer).RaiseRecordFieldSerialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (target is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoRecordFieldSerializable)target).RecordFieldSerialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (_callbackRecordSeriablizable is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldSerialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            return false;
        }

        private string RaiseCustomeNodeNameOverride(long index, object record)
        {
            if (Writer != null && Writer.HasCustomeNodeNameOverrideSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseCustomeNodeNameOverride(index, record), null);
            }
            else if (_callbackCustomNodeNameOverrideable != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackCustomNodeNameOverrideable.GetOverrideNodeName(index, record), null);
            }
            return null;
        }
    }
}
