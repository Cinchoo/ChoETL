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
        private IChoNotifyRecordWrite _callbackRecord;
        private bool _configCheckDone = false;
        private long _index = 0;
        private Lazy<XmlSerializer> _se = null;
        private readonly Regex _beginTagRegex = new Regex(@"^(<\w+)(.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _endTagRegex = new Regex("</.*>$");
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        private Lazy<List<object>> _recBuffer = null;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlRecordWriter(Type recordType, ChoXmlRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _recBuffer = new Lazy<List<object>>(() =>
            {
                var b = Writer.Context.RecBuffer;
                if (b == null)
                    Writer.Context.RecBuffer = new List<object>();

                return Writer.Context.RecBuffer;
            });
            //Configuration.Validate();
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
                        sw.Write("{1}{0}".FormatString(XmlNamespaceEndElementText(Configuration.RootName, Configuration.DefaultNamespacePrefix, Configuration.NS), Configuration.EOLDelimiter));
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
                yield return rec;

            while (records.MoveNext())
                yield return records.Current;
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

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (records == null) yield break;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            _se = new Lazy<XmlSerializer>(() => Configuration.XmlSerializer == null ? null : Configuration.XmlSerializer);

            string recText = String.Empty;
            if (!Configuration.OmitXmlDeclaration)
                sw.Write("{0}{1}", GetXmlDeclaration(), Configuration.EOLDelimiter);

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
                                Type recordType = ElementType == null ? notNullRecord.GetType() : ElementType;
                                Configuration.RecordType = recordType.ResolveType();
                                Configuration.IsDynamicObject = recordType.IsDynamicType();
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

                                if (!RaiseBeginWrite(sw))
                                    yield break;

                                if (!Configuration.RootName.IsNullOrWhiteSpace() && !Configuration.IgnoreRootName)
                                    sw.Write("{0}".FormatString(XmlNamespaceStartElementText(Configuration.RootName, Configuration.DefaultNamespacePrefix, Configuration.NS)));
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
                                if (rt != Configuration.RecordType)
                                    throw new ChoWriterException("Invalid record found.");
                            }
                        }


                        if (!RaiseBeforeRecordWrite(record, _index, ref recText))
                            yield break;

                        if (recText == null)
                            continue;

                        try
                        {
                            if (!Configuration.UseXmlSerialization)
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);

                                if (ToText(_index, record, Configuration, out recText))
                                {
                                    if (!recText.IsNullOrEmpty())
                                    {
                                        if (!Configuration.IgnoreRootName)
                                        {
                                            sw.Write("{1}{0}", recText, Configuration.EOLDelimiter);
                                        }
                                        else
                                        {
                                            sw.Write(recText);
                                        }
                                    }

                                    if (!RaiseAfterRecordWrite(record, _index, recText))
                                        yield break;
                                }
                            }
                            else
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                                    record.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations);

                                if (record != null)
                                {
                                    if (_se.Value != null)
                                        _se.Value.Serialize(sw, record);
                                    else
                                        sw.Write("{1}{0}", ChoUtility.XmlSerialize(record, null, Configuration.EOLDelimiter, Configuration.NullValueHandling).Indent(2, Configuration.IndentChar.ToString()), Configuration.EOLDelimiter);

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
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            }
                            else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                            {
                                if (!RaiseRecordWriteError(record, _index, recText, ex))
                                    throw;
                                else
                                {
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
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

        private string GetNamespaceText()
        {
            if (Configuration.NamespaceManager == null)
                return null;

            StringBuilder nsText = new StringBuilder();
            foreach (var kvp in new ChoXmlNamespaceManager(Configuration.NamespaceManager).NSDict)
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
                    recText = @"<{0}/>".FormatString(XmlNamespaceElementName(config.NodeName, Configuration.DefaultNamespacePrefix)).Indent(config.Indent * 1, config.IndentChar.ToString());
                    return true;
                }
                else
                {
                    recText = @"<{0} xmlns:xsi=""{1}"" xsi:nil=""true"" />".FormatString(XmlNamespaceElementName(config.NodeName, Configuration.DefaultNamespacePrefix), ChoXmlSettings.XmlSchemaInstanceNamespace
                                ).Indent(config.Indent * 1, config.IndentChar.ToString());
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

            foreach (KeyValuePair<string, ChoXmlRecordFieldConfiguration> kvp in GetOrderedKVP(config))
            {
                fieldConfig = kvp.Value;
                fieldValue = null;
                fieldText = String.Empty;
                if (config.PIDict != null)
                    config.PIDict.TryGetValue(kvp.Key, out pi);

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
                            if (ElementType == null)
                            {
								kvp.Value.FieldType = typeof(object);

								//if (fieldValue == null)
        //                            kvp.Value.FieldType = typeof(object);
        //                        else
        //                            kvp.Value.FieldType = fieldValue.GetType();
                            }
                            else
                                kvp.Value.FieldType = ElementType;
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
                    if (fieldValue == null)
                    {
                        if (fieldConfig.IsDefaultValueSpecified)
                            fieldValue = fieldConfig.DefaultValue;
                    }

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
                                if (!RaiseRecordFieldWriteError(rec, index, kvp.Key, fieldText, ex))
                                    throw new ChoWriterException($"Failed to write '{fieldValue}' value of '{kvp.Key}' member.", ex);
                            }
                        }
                        else
                        {
                            throw new ChoWriterException("Failed to use '{0}' fallback value for '{1}' member.".FormatString(fieldValue, kvp.Key), innerEx);
                        }
                    }
                }

                RaiseRecordFieldSerialize(rec, index, kvp.Key, ref fieldValue);

				if (!fieldConfig.IsAnyXmlNode)
				{
					if (fieldConfig.IsXmlAttribute)
					{
						attrs.Add(fieldConfig.FieldName, fieldValue);
					}
					else
					{
						elems.Add(fieldConfig.FieldName, fieldValue);
					}
				}
				else
				{
					if (fieldValue == null || fieldValue.GetType().IsSimple())
						attrs.Add(fieldConfig.FieldName, fieldValue);
					else
						elems.Add(fieldConfig.FieldName, fieldValue);
				}
            }

            string nodeName = config.NodeName;
            if (rec is ChoDynamicObject && ((ChoDynamicObject)rec).DynamicObjectName != ChoDynamicObject.DefaultName)
            {
                ChoDynamicObject dobj = rec as ChoDynamicObject;
                nodeName = dobj.DynamicObjectName;
            }

			XNamespace ns = Configuration.NS;
			XElement ele = NewXElement(nodeName, Configuration.DefaultNamespacePrefix, ns);
			string innerXml1 = null;
            if (typeof(IChoScalarObject).IsAssignableFrom(config.RecordType))
            {
				ele = NewXElement(nodeName, Configuration.DefaultNamespacePrefix, ns, elems.First().Value);
            }
            else
            {
				ele = NewXElement(nodeName, Configuration.DefaultNamespacePrefix, ns);
				foreach (var kvp in attrs)
                    ele.Add(new XAttribute(kvp.Key, kvp.Value));
                foreach (var kvp in elems)
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
                            innerXml = ChoUtility.XmlSerialize(rec, null, Configuration.EOLDelimiter, Configuration.NullValueHandling, Configuration.DefaultNamespacePrefix);
                            innerXml = _beginTagRegex.Replace(innerXml, delegate (Match m)
                            {
                                return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            });
                            innerXml = _endTagRegex.Replace(innerXml, delegate (Match thisMatch)
                            {
                                return "</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix));
                            });
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
                        if (ElementType == null)
                        {
                            if (kvp.Value is ChoCDATA)
                                ele.Add(ns != null ? new XElement(ns + kvp.Key, new XCData(((ChoCDATA)kvp.Value).Value)) : new XElement(kvp.Key, new XCData(((ChoCDATA)kvp.Value).Value)));
                            else
                                ele.Add(ns != null ? new XElement(ns + kvp.Key, kvp.Value) : new XElement(kvp.Key, kvp.Value));
                        }
                        else
                            ele.Value = kvp.Value.ToNString();
                    }
                    else
                    {
                        innerXml1 = ChoUtility.XmlSerialize(kvp.Value, null, Configuration.EOLDelimiter, Configuration.NullValueHandling, Configuration.DefaultNamespacePrefix);
                        if (!kvp.Value.GetType().IsArray)
                        {
                            innerXml1 = _beginTagRegex.Replace(innerXml1, delegate (Match m)
                            {
                                return "<" + XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix) + m.Groups[2].Value;
                            });
                            innerXml1 = _endTagRegex.Replace(innerXml1, delegate (Match thisMatch)
                            {
                                return "</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix));
                            });
                        }
                        else
                        {
                            string eleName = XmlNamespaceElementName(kvp.Key.ToSingular(), Configuration.DefaultNamespacePrefix);
                            innerXml1 = innerXml1.Replace("<dynamic>", "<{0}>".FormatString(eleName));
                            innerXml1 = innerXml1.Replace("</dynamic>", "</{0}>".FormatString(eleName));

                            if (eleName == kvp.Key)
                                innerXml1 = "<{0}>{1}</{0}>".FormatString(XmlNamespaceElementName(kvp.Key.ToPlural(), Configuration.DefaultNamespacePrefix), innerXml1);
                            else
                                innerXml1 = "<{0}>{1}</{0}>".FormatString(XmlNamespaceElementName(kvp.Key, Configuration.DefaultNamespacePrefix), innerXml1);
                        }
                        ele.Add(ParseElement(innerXml1, Configuration.NamespaceManager, Configuration.DefaultNamespacePrefix, ns));

                    }
                }
            }

            innerXml1 = ele.ToString(SaveOptions.OmitDuplicateNamespaces);
            if (config.IgnoreNodeName)
            {
                innerXml1 = _beginTagRegex.Replace(innerXml1, delegate (Match m)
                {
                    return null;
                });
                innerXml1 = _endTagRegex.Replace(innerXml1, delegate (Match thisMatch)
                {
                    return null;
                });
                innerXml1 = XElement.Parse(innerXml1).ToString();
            }

            recText = config.IgnoreRootName ? innerXml1 : innerXml1.Indent(config.Indent, config.IndentChar.ToString());

            return true;
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

		private string XmlNamespaceEndElementText(string name, string nsPrefix = null, XNamespace xs = null)
		{
			if (xs == null)
			{
				return "</{0}>".FormatString(name);
			}
			else
			{
				return "</{0}:{1}>".FormatString(nsPrefix, name);
			}
		}

		private string XmlNamespaceStartElementText(string name, string nsPrefix = null, XNamespace xs = null)
		{
			if (xs == null)
			{
				return "<{0}>".FormatString(name);
			}
			else
			{
				return @"<{0}:{1} xmlns:{0}=""{2}"">".FormatString(nsPrefix, name, xs.ToString());
			}
		}

		private string XmlNamespaceElementName(string name, string nsPrefix = null)
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

		private XElement NewXElement(string name, string nsPrefix = null, XNamespace xs = null, object value = null)
		{
			var e = value == null ? new XElement(name) : new XElement(name, value);

			if (xs != null)
			{
				var nsAttr = new XAttribute(XNamespace.Xmlns + nsPrefix, xs);
				e.Add(nsAttr);
				e.Name = xs + e.Name.LocalName;
			}
			return e;
		}

		private XElement ParseElement(string strXml, XmlNamespaceManager mngr, string nsPrefix = null, XNamespace xs = null)
		{
			XmlParserContext parserContext = new XmlParserContext(null, mngr, null, XmlSpace.None);
			XmlTextReader txtReader = new XmlTextReader(strXml, XmlNodeType.Element, parserContext);
			var e = XElement.Load(txtReader);
			if (xs != null)
			{
				var nsAttr = new XAttribute(XNamespace.Xmlns + nsPrefix, xs);
				e.Add(nsAttr);
				foreach (XElement ce in e.DescendantsAndSelf())
					ce.Name = xs + ce.Name.LocalName;
				//if (!Configuration.IgnoreRootName)
				//{
				//	e.Attribute(XNamespace.Xmlns + nsPrefix).Remove();
				//}
			}
			return e;
		}
		private string SerializeObject(string propName, object value, Attribute[] attrs)
        {
            ChoXmlRecordConfiguration config = null;

            string recText = null;
            if (value is IList)
            {
                Type itemType = value.GetType().GetItemType().GetUnderlyingType();
                if (!itemType.IsSimple())
                {
                    config = new ChoXmlRecordConfiguration(value.GetType().GetItemType().GetUnderlyingType());
                    config.Validate(null);
                }
                else
                    config = Configuration;

                string arrElementName = propName;
                string itemName = propName.ToSingular();

                StringBuilder msg = new StringBuilder();

                msg.AppendFormat("<{0}>{1}", arrElementName, config.EOLDelimiter);
                foreach (var item in (IList)value)
                {
                    if (itemType.IsSimple())
                    {
                        recText = "<{0}>{1}</{0}>{2}".FormatString(itemName, item.ToString(), config.EOLDelimiter).Indent(config.Indent, config.IndentChar.ToString());
                    }
                    else
                        ToText(0, item, config, out recText);

                    msg.Append(recText + config.EOLDelimiter);
                }
                msg.AppendFormat("</{0}>{1}", arrElementName, config.EOLDelimiter);

                recText = msg.ToString();
            }
            else if (value is IDictionary)
            {
                Type[] arguments = value.GetType().GetGenericArguments();
                Type keyType = arguments[0].GetUnderlyingType();
                Type valueType = arguments[1].GetUnderlyingType();
                ChoXmlRecordConfiguration keyConfig = Configuration;
                ChoXmlRecordConfiguration valueConfig = Configuration;
                if (!keyType.IsSimple())
                {
                    config = new ChoXmlRecordConfiguration(keyType);
                    config.Validate(null);
                }
                if (!valueType.IsSimple())
                {
                    config = new ChoXmlRecordConfiguration(valueType);
                    config.Validate(null);
                }

                string arrElementName = propName;
                string itemName = propName.ToSingular();
                string keyElementName = "Key";
                string valueElementName = "Value";

                StringBuilder msg = new StringBuilder();

                msg.AppendFormat("<{0}>{1}", arrElementName, config.EOLDelimiter);
                foreach (var key in ((IDictionary)value).Keys)
                {
                    if (keyType.IsSimple())
                    {
                        recText = "<{0}>{1}</{0}>{2}".FormatString(keyElementName, key.ToString(), config.EOLDelimiter).Indent(config.Indent, config.IndentChar.ToString());
                    }
                    else
                    {
                        ToText(0, key, config, out recText);
                        recText = "<{1}>{0}{2}{0}</{1}>".FormatString(config.EOLDelimiter, keyElementName, recText).Indent(config.Indent, config.IndentChar.ToString());
                    }

                    msg.Append(recText + config.EOLDelimiter);

                    object dictValue = ((IDictionary)value)[key];
                    if (valueType.IsSimple())
                    {
                        recText = "<{0}>{1}</{0}>{2}".FormatString(valueElementName, dictValue.ToString(), config.EOLDelimiter).Indent(config.Indent, config.IndentChar.ToString());
                    }
                    else
                    {
                        ToText(0, dictValue, config, out recText);
                        recText = "<{1}>{0}{2}{0}</{1}>".FormatString(config.EOLDelimiter, valueElementName, recText).Indent(config.Indent, config.IndentChar.ToString());
                    }
                    msg.Append(recText + config.EOLDelimiter);
                }
                msg.AppendFormat("</{0}>{1}", arrElementName, config.EOLDelimiter);

                recText = msg.ToString();

            }
            else
            {
                config = new ChoXmlRecordConfiguration(value.GetType().GetUnderlyingType());
                config.Validate(null);

                ToText(0, value, config, out recText);
            }
            if (config != null)
                return recText.Unindent(config.Indent, config.IndentChar.ToString());
            else
                return recText;
        }

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
                    if (fieldValue.Contains(Configuration.EOLDelimiter))
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

        private bool RaiseBeginWrite(object state)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginWrite(state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeginWrite(state), true);
            }
            return true;
        }

        private void RaiseEndWrite(object state)
        {
            if (_callbackRecord != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndWrite(state));
            }
            else if (Writer != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseEndWrite(state));
            }
        }

        private bool RaiseBeforeRecordWrite(object target, long index, ref string state)
        {
            if (_callbackRecord != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            else if (Writer != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordWrite(object target, long index, string state)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordWrite(target, index, state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordWrite(target, index, state), true);
            }
            return true;
        }

        private bool RaiseRecordWriteError(object target, long index, string state, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordWriteError(target, index, state, ex), false);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordWriteError(target, index, state, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (_callbackRecord != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (Writer != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldWriteError(target, index, propName, value, ex), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, value, ex), true);
            }
            return true;
        }

        private bool RaiseRecordFieldSerialize(object target, long index, string propName, ref object value)
        {
            if (_callbackRecord is IChoSerializable)
            {
                IChoSerializable rec = _callbackRecord as IChoSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldSerialize(target, index, propName, ref state), true);

                value = state;

                return retValue;
            }
            else if (Writer != null && Writer is IChoSerializableWriter)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoSerializableWriter)Writer).RaiseRecordFieldSerialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            return true;
        }
    }
}
