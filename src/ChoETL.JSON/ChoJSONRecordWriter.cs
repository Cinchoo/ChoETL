using Newtonsoft.Json;
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
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChoETL
{
    internal class ChoJSONRecordWriter : ChoRecordWriter
    {
        private IChoNotifyFileWrite _callbackFileWrite;
        private IChoNotifyRecordWrite _callbackRecordWrite;
        private IChoNotifyRecordFieldWrite _callbackRecordFieldWrite;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private long _index = 0;
        bool isFirstRec = true;
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        //private Lazy<List<object>> _recBuffer = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONRecordWriter(Type recordType, ChoJSONRecordConfiguration configuration) : base(recordType, true)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);

            //_recBuffer = new Lazy<List<object>>(() =>
            //{
            //    if (Writer != null)
            //    {
            //        var b = Writer.Context.ContainsKey("RecBuffer") ? Writer.Context.RecBuffer : null;
            //        if (b == null)
            //            Writer.Context.RecBuffer = new List<object>();

            //        return Writer.Context.RecBuffer;
            //    }
            //    else
            //        return new List<object>();
            //}, true);

            //Configuration.Validate();

            BeginWrite = new Lazy<bool>(() =>
            {
                TextWriter sw = _sw as TextWriter;
                if (sw != null)
                    return RaiseBeginWrite(sw);

                return false;
            });
        }

        private bool SupportMultipleContent
        {
            get { return Configuration.SupportMultipleContent == null ? false : Configuration.SupportMultipleContent.Value; }
        }

        internal void EndWrite(object writer)
        {
            TextWriter sw = writer as TextWriter;

            try
            {
                if (Configuration.IsInitialized)
                {
                    if (!SupportMultipleContent)
                    {
                        if (Configuration.IgnoreRootName || Configuration.RootName.IsNullOrWhiteSpace())
                            sw.Write(String.Format("{0}]", EOLDelimiter));
                        else
                        {
                            sw.Write(String.Format("{0}]}}", EOLDelimiter));
                        }
                    }
                    else
                    {
                        if (!Configuration.SingleElement.Value)
                            sw.Write(String.Format("{0}}}", EOLDelimiter));
                    }
                }
            }
            catch { }

            RaiseEndWrite(sw);
        }

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            _sw = writer;
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (records == null) yield break;
            if (Configuration.SingleElement == null)
                Configuration.SingleElement = false;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            string recText = String.Empty;
            bool recordIgnored = false;
            try
            {
                foreach (object record in records)
                {
                    _index++;

                    if (!isFirstRec)
                    {
                        if (!recordIgnored)
                            sw.Write(",");
                        else
                            recordIgnored = false;
                    }

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
                        if (!Configuration.IsInitialized)
                        {
                            if (record == null)
                                continue;

                            string[] fieldNames = null;
                            Type recordType = ElementType == null ? record.GetType() : ElementType;
                            Configuration.RecordType = recordType; //.ResolveType();
                            Configuration.IsDynamicObject = recordType.IsDynamicType();
                            if (typeof(IDictionary).IsAssignableFrom(Configuration.RecordType)
                                || typeof(IList).IsAssignableFrom(Configuration.RecordType))
                                Configuration.UseJSONSerialization = true;

                            if (!Configuration.IsDynamicObject)
                            {
                                if (!Configuration.SingleElement.Value)
                                {
                                    if (Configuration.RootName.IsNullOrWhiteSpace())
                                    {
                                        var root = Configuration.RecordType.GetCustomAttribute<ChoJSONNRootNameAttribute>();
                                        if (root != null)
                                        {
                                            Configuration.RootName = root.Name;
                                        }
                                    }

                                    if (Configuration.NodeName.IsNullOrWhiteSpace())
                                    {
                                        var root = Configuration.RecordType.GetCustomAttribute<ChoJSONNRootNameAttribute>();
                                        if (root != null)
                                        {
                                            Configuration.NodeName = root.Name;
                                        }
                                        else
                                        {
                                            var xmlRoot = Configuration.RecordType.GetCustomAttribute<XmlRootAttribute>();
                                            if (xmlRoot != null)
                                            {
                                                Configuration.NodeName = xmlRoot.ElementName;
                                            }
                                            else
                                                Configuration.NodeName = Configuration.RecordType.Name;

                                        }
                                    }
                                }

                                if (Configuration.JSONRecordFieldConfigurations.Count == 0)
                                    Configuration.MapRecordFields(Configuration.RecordType);
                            }

                            if (Configuration.IsDynamicObject)
                            {
                                var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                                fieldNames = dict.Keys.ToArray();
                            }
                            else
                            {
                                fieldNames = ChoTypeDescriptor.GetProperties<ChoJSONRecordFieldAttribute>(Configuration.RecordType).Select(pd => pd.Name).ToArray();
                                if (fieldNames.Length == 0)
                                {
                                    fieldNames = ChoType.GetProperties(Configuration.RecordType).Select(p => p.Name).ToArray();
                                }
                            }

                            Configuration.Validate(fieldNames);

                            Configuration.IsInitialized = true;

                            if (!BeginWrite.Value)
                                yield break;

                            if (!SupportMultipleContent)
                            {
                                if (Configuration.IgnoreRootName || Configuration.RootName.IsNullOrWhiteSpace())
                                    sw.Write("[");
                                else
                                {
                                    sw.Write(@"{{""{0}"": [".FormatString(Configuration.RootName.NTrim()));
                                }
                            }
                            else
                            {
                                if (!Configuration.SingleElement.Value)
                                    sw.Write(String.Format("{{{0}", EOLDelimiter));
                            }
                        }

                        if (!RaiseBeforeRecordWrite(record, _index, ref recText))
                            yield break;

                        if (recText == null)
                            continue;

                        try
                        {
                            if (!Configuration.UseJSONSerialization)
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);

                                if (ToText(_index, record, out recText))
                                {
                                    if (!recText.IsNullOrEmpty())
                                    {
                                        if (!SupportMultipleContent)
                                            sw.Write("{1}{0}", Indent(recText), EOLDelimiter);
                                        else
                                        {
                                            if (Configuration.SingleElement.Value)
                                            {
                                                sw.Write(Unindent(recText));
                                            }
                                            else
                                            {
                                                if (_index == 1)
                                                    sw.Write("{0}", Indent(recText));
                                                else
                                                    sw.Write("{1}{0}", Indent(recText), EOLDelimiter);
                                            }
                                        }

                                        if (!RaiseAfterRecordWrite(record, _index, recText))
                                            yield break;
                                    }
                                }
                            }
                            else
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                                    record.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);

                                recText = JsonConvert.SerializeObject(record, Configuration.Formatting, Configuration.JsonSerializerSettings);
                                if (!SupportMultipleContent)
                                    sw.Write("{1}{0}", Indent(recText), EOLDelimiter);
                                else
                                {
                                    if (Configuration.SingleElement.Value)
                                    {
                                        sw.Write(Unindent(recText));
                                    }
                                    else
                                    {
                                        if (_index == 1)
                                            sw.Write("{0}", Indent(recText));
                                        else
                                            sw.Write("{1}{0}", Indent(recText), EOLDelimiter);
                                    }
                                }

                                if (!RaiseAfterRecordWrite(record, _index, recText))
                                    yield break;
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
                                recordIgnored = true;
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            }
                            else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                            {
                                if (!RaiseRecordWriteError(record, _index, recText, ex))
                                    throw;
                                else
                                {
                                    recordIgnored = true;
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

                    isFirstRec = false;
                }
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
            }
        }

        private bool ToText(long index, object rec, out string recText)
        {
            if (typeof(IChoScalarObject).IsAssignableFrom(Configuration.RecordType))
                rec = ChoActivator.CreateInstance(Configuration.RecordType, rec);

            if (!Configuration.IsDynamicObject)
            {
                if (rec.ToTextIfCustomSerialization(out recText))
                    return true;

                //Check if KVP object
                if (rec.GetType().IsKeyValueType())
                {
                    recText = SerializeObject(rec);
                    return true;
                }
            }

            recText = null;
            if (rec == null)
            {
                if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                    return false;
                else if (Configuration.NullValueHandling == ChoNullValueHandling.Default)
                    rec = ChoActivator.CreateInstance(Configuration.RecordType);
                else
                {
                    recText = "{{{0}}}".FormatString(EOLDelimiter);
                    return true;
                }
            }

            StringBuilder msg = new StringBuilder();
            object fieldValue = null;
            string fieldText = null;
            ChoJSONRecordFieldConfiguration fieldConfig = null;
            string fieldName = null;

            if (Configuration.ColumnCountStrict)
                CheckColumnsStrict(rec);

            //bool firstColumn = true;
            PropertyInfo pi = null;
            bool isFirst = true;
            object rootRec = rec;

            if (!Configuration.IgnoreNodeName)
            {
                if (Configuration.SupportMultipleContent == null || !Configuration.SupportMultipleContent.Value)
                    Configuration.IgnoreNodeName = true;
            }
            if (!Configuration.IgnoreNodeName)
            {
                if (Configuration.NodeName.IsNullOrWhiteSpace())
                {
                    if (Configuration.IsDynamicObject && rec is ChoDynamicObject && ((ChoDynamicObject)rec).DynamicObjectName != ChoDynamicObject.DefaultName)
                        msg.AppendFormat(@"""{1}"": {{{0}", EOLDelimiter, ((ChoDynamicObject)rec).DynamicObjectName);
                    else if(!RecordType.IsSimple())
                        msg.AppendFormat("{{{0}", EOLDelimiter);
                }
                else
                {
                    if (!RecordType.IsSimple())
                        msg.AppendFormat(@"""{1}"": {{{0}", EOLDelimiter, Configuration.NodeName);
                    else
                        msg.AppendFormat(@"""{1}"": {0}", EOLDelimiter, Configuration.NodeName);
                }
            }
            else if (!RecordType.IsSimple())
                msg.AppendFormat("{{{0}", EOLDelimiter);

            foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                //if (Configuration.IsDynamicObject)
                //{
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                //}

                fieldConfig = kvp.Value;
                fieldName = fieldConfig.FieldName;
                fieldValue = null;
                fieldText = String.Empty;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);
                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                if (Configuration.ThrowAndStopOnMissingField)
                {
                    if (Configuration.IsDynamicObject)
                    {
                        var dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                        if (!dict.ContainsKey(kvp.Key))
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' JSON node.".FormatString(fieldConfig.FieldName));
                    }
                    else
                    {
                        if (pi == null)
                        {
                            if (!RecordType.IsSimple())
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' JSON node.".FormatString(fieldConfig.FieldName));
                        }
                    }
                }

                try
                {
                    if (Configuration.IsDynamicObject)
                    {
                        IDictionary<string, Object> dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                        fieldValue = dict[kvp.Key]; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
                        if (rec is ChoDynamicObject)
                        {
                            if (((ChoDynamicObject)rec).IsAttribute(fieldName)
                                && Configuration.EnableXmlAttributePrefix)
                                fieldName = "@{0}".FormatString(fieldName);
                        }

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
                    else if (RecordType.IsSimple())
                        fieldValue = rec;
                    else
                        rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, true);

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.MemberLevel)
                        rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);

                    if (!RaiseAfterRecordFieldWrite(rec, index, kvp.Key, fieldValue))
                        return false;
                }
                catch (ChoParserException)
                {
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    if (Configuration.ThrowAndStopOnMissingField)
                        throw;
                }
                catch (Exception ex)
                {
                    ChoETLFramework.HandleException(ref ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw;

                    try
                    {
                        if (Configuration.IsDynamicObject)
                        {
                            var dict = rec.ToDynamicObject() as IDictionary<string, Object>;

                            if (dict.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                            else if (dict.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                            else
                            {
                                var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                                fieldValue = null;
                                throw ex1;
                            }
                        }
                        else if (pi != null)
                        {
                            if (rec.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
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

                bool isSimple = true;

                if (fieldConfig.CustomSerializer != null)
                {
                    recText = fieldConfig.CustomSerializer(fieldValue) as string;
                }
                else if (RaiseRecordFieldSerialize(rec, index, kvp.Key, ref fieldValue))
                {
                    recText = fieldValue as string;
                }
                else
                {
                    Type ft = fieldValue == null ? typeof(object) : fieldValue.GetType();
                    if (fieldConfig.IgnoreFieldValue(fieldValue))
                        fieldText = null;
                    else if (fieldValue == null)
                    {
                        //if (fieldConfig.FieldType == null || fieldConfig.FieldType == typeof(object))
                        //{
                        //    if (fieldConfig.NullValue == null)
                        //        fieldText = !fieldConfig.IsArray ? "null" : "[]";
                        //    else
                        //        fieldText = fieldConfig.NullValue;
                        //}
                        if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                            fieldText = null;
                        else if (Configuration.NullValueHandling == ChoNullValueHandling.Default)
                            fieldText = JsonConvert.SerializeObject(ChoActivator.CreateInstance(fieldConfig.FieldType));
                        else if (Configuration.NullValueHandling == ChoNullValueHandling.Empty && fieldConfig.FieldType == typeof(string))
                            fieldText = String.Empty;
                        else
                        {
                            if (fieldConfig.NullValue == null)
                            {
                                if (fieldConfig.FieldType == null || fieldConfig.FieldType == typeof(object))
                                    fieldText = !fieldConfig.IsArray ? "null" : "[]";
                                else
                                    fieldText = !typeof(IList).IsAssignableFrom(fieldConfig.FieldType) ? "null" : "[]";
                            }
                            else
                                fieldText = fieldConfig.NullValue;
                        }
                    }
                    else if (ft == typeof(string) || ft == typeof(char))
                        fieldText = JsonConvert.SerializeObject(NormalizeFieldValue(kvp.Key, fieldValue.ToString(), kvp.Value.Size, kvp.Value.Truncate, false, GetFieldValueJustification(kvp.Value.FieldValueJustification, kvp.Value.FieldType), GetFillChar(kvp.Value.FillChar, kvp.Value.FieldType), false, kvp.Value.GetFieldValueTrimOption(kvp.Value.FieldType, Configuration.FieldValueTrimOption)));
                    else if (ft == typeof(DateTime) || ft == typeof(TimeSpan))
                        fieldText = JsonConvert.SerializeObject(fieldValue);
                    else if (ft.IsEnum)
                    {
                        fieldText = JsonConvert.SerializeObject(fieldValue);
                    }
                    else if (ft == typeof(ChoCurrency))
                        fieldText = "\"{0}\"".FormatString(fieldValue.ToString());
                    else if (ft == typeof(bool))
                        fieldText = JsonConvert.SerializeObject(fieldValue);
                    else if (ft.IsNumeric())
                        fieldText = fieldValue.ToString();
                    else
                        isSimple = false;

                    if (fieldText != null)
                    {
                        if (isFirst)
                        {
                            if (RecordType.IsSimple())
                            {
                                msg.AppendFormat(fieldText);
                            }
                            else
                            {
                                msg.AppendFormat("{2}\"{0}\":{1}", fieldName, isSimple ? " {0}".FormatString(fieldText) :
                                    Indent(SerializeObject(fieldValue, fieldConfig.UseJSONSerialization)).Substring(1),
                                    Indent(String.Empty));
                            }
                        }
                        else
                        {
                            if (RecordType.IsSimple())
                            {
                                msg.AppendFormat($",{fieldText}");
                            }
                            else
                            {
                                msg.AppendFormat(",{2}{3}\"{0}\":{1}", fieldName, isSimple ? " {0}".FormatString(fieldText) :
                                    Indent(SerializeObject(fieldValue, fieldConfig.UseJSONSerialization)).Substring(1),
                                    EOLDelimiter, Indent(String.Empty));
                            }
                        }
                        isFirst = false;
                    }
                }
            }

            if (!RecordType.IsSimple())
                msg.AppendFormat("{0}}}", EOLDelimiter);
            recText = Configuration.IgnoreNodeName ? Unindent(msg.ToString()) : msg.ToString();

            return true;
        }

        private string Indent(string value, int indentValue = 1)
        {
            if (value == null)
                return value;

            return Configuration.Formatting == Formatting.Indented ? value.Indent(indentValue, "  ") : value;
        }

        private string Unindent(string value)
        {
            if (value == null)
                return value;

            return Configuration.Formatting == Formatting.Indented ? value.Unindent(1, "  ") : value;
        }

        private string EOLDelimiter
        {
            get
            {
                return Configuration.Formatting == Formatting.Indented ? Configuration.EOLDelimiter : String.Empty;
            }
        }

        private string SerializeObject(object target, bool? useJSONSerialization = null)
        {
            bool lUseJSONSerialization = useJSONSerialization == null ? Configuration.UseJSONSerialization : useJSONSerialization.Value;
            if (lUseJSONSerialization)
                return JsonConvert.SerializeObject(target, Configuration.Formatting);
            else
            {
                //return JsonConvert.SerializeObject(target, Configuration.Formatting);

                Type objType = target.GetType();
                if (objType.IsSimple())
                    return JsonConvert.SerializeObject(target);
                else
                {
                    if (target is IEnumerable && !(target is IDictionary) && !target.GetType().IsDynamicType())
                    {
                        StringBuilder msg = new StringBuilder();
                        bool first = true;
                        foreach (var item in (IEnumerable)target)
                        {
                            if (first)
                                first = false;
                            else
                                msg.Append($",{EOLDelimiter}");

                            if (item == null)
                            {
                                if (Configuration.JsonSerializerSettings != null && Configuration.JsonSerializerSettings.NullValueHandling == NullValueHandling.Ignore)
                                {

                                }
                                else
                                    msg.Append(JsonConvert.SerializeObject(null));
                            }
                            else if (item.GetType().IsSimple())
                                msg.Append(JsonConvert.SerializeObject(item, Configuration.Formatting));
                            else
                            {
                                //var obj = MapToDictionary(item);
                                msg.Append(JsonConvert.SerializeObject(item, Configuration.Formatting));
                            }
                        }

                        return "[{0}{1}{0}]".FormatString(EOLDelimiter, Indent(msg.ToString()));
                    }
                    else
                        return JsonConvert.SerializeObject(target /*MapToDictionary(target)*/, Configuration.Formatting, Configuration.JsonSerializerSettings);
                }
            }
        }

  //      public IEnumerable<IDictionary<string, object>> MapToDictionary(IList source)
  //      {
  //          foreach (var item in source)
  //              return MapToDictionary(item);

        //	return Enumerable.Empty<IDictionary<string, object>>();
        //}

        public object MapToDictionary(object source)
        {
            IDictionary<string, object> dict = null;
            if (source != null && source.GetType().IsDynamicType())
            {
                if (source is ChoDynamicObject)
                {
                    ChoDynamicObject dobj = source as ChoDynamicObject;

                    dict = new Dictionary<string, object>();
                    foreach (var kvp in dobj)
                    {
                        if (dobj.IsAttribute(kvp.Key) && Configuration.EnableXmlAttributePrefix)
                            dict.Add("@{0}".FormatString(kvp.Key), kvp.Value);
                        else
                            dict.Add(kvp.Key, kvp.Value);
                    }
                }
                else if (source is IDictionary<string, object>)
                    dict = source as IDictionary<string, object>;
                else
                    return source;
            }
            else
            {
                var dictionary = new Dictionary<string, object>();
                MapToDictionaryInternal(dictionary, source);
                dict = dictionary;
            }

            if (dict is ChoDynamicObject && dict.Keys.Count == 1 && ((ChoDynamicObject)dict).DynamicObjectName == dict.Keys.First().ToPlural())
            {
                object x = dict[dict.Keys.First()];
                if (!(x is IList))
                    return FixArray(x as IDictionary<string, object>);
                else
                {
                    return ((IList)x).Cast<object>().Select(i => FixArray(i as IDictionary<string, object>)).ToArray();
                }
            }
            else
            {
                return FixArray(dict);
            }
        }

        private IDictionary<string, object> FixArray(IDictionary<string, object> dict)
        {
            if (dict == null)
                return dict;

            foreach (var key in dict.Keys.ToArray())
            {
                object value = dict[key];
                if (value is IList && ((IList)value).Cast<object>().All(i => i is ChoDynamicObject))
                {
                    if (((IList)value).Cast<ChoDynamicObject>().All(i => i.Count == 1 && i.HasText()))
                    {
                        dict[key] = ((IList)value).Cast<ChoDynamicObject>().Select(i => i.GetText()).ToArray();
                    }
                }
                else if (value is IDictionary<string, object>)
                {
                    dict[key] = MapToDictionary(value as IDictionary<string, object>);
                    //if (!(value1 is IList))
                    //	dict[key] = MapToDictionary(value as IDictionary<string, object>);
                    //else
                    //	dict[key] = MapToDictionary(value as IDictionary<string, object>).ToArray();
                }
                else if (value is IList)
                {
                    List<object> list = new List<object>();
                    foreach (var obj in (IList)value)
                    {
                        if (obj is IDictionary<string, object>)
                        {
                            object value1 = MapToDictionary(obj as IDictionary<string, object>);
                            if (value1 is IList)
                                list.AddRange(((IList)value1).Cast<object>().ToArray());
                            else
                                list.Add(value1);
                        }
                        else
                            list.Add(obj);
                    }

                    dict[key] = list.ToArray();
                }
            }
            return dict;
        }

        private object SimpleTypeValue(object source)
        {
            if (source.GetType() == typeof(ChoCurrency))
                return ((ChoCurrency)source).Amount;
            else
                return source;
        }

        private object Marshal(object source)
        {
            if (source == null)
                return null;
            if (source.GetType().IsSimple())
                return SimpleTypeValue(source);

            return MapToDictionary(source);
        }

        private void MapToDictionaryInternal(IDictionary<string, object> dictionary, object source)
        {
            var isKVPAttrDefined = source.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null;

            //check if object is KeyValuePair
            Type valueType = source.GetType();
            if (valueType.IsGenericType)
            {
                Type baseType = valueType.GetGenericTypeDefinition();
                if (baseType == typeof(KeyValuePair<,>))
                {
                    object kvpKey = valueType.GetProperty("Key").GetValue(source, null);
                    object kvpValue = valueType.GetProperty("Value").GetValue(source, null);
                    if (kvpValue is IEnumerable)
                        dictionary[kvpKey.ToNString()] = MapToDictionary(kvpValue as IEnumerable);
                    else if (kvpValue != null)
                        dictionary[kvpKey.ToNString()] = MapToDictionary(kvpValue);
                }
            }

            if (isKVPAttrDefined)
            {
                var kP = source.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoKeyAttribute>() != null).FirstOrDefault();
                var vP = source.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoValueAttribute>() != null).FirstOrDefault();


                if (kP != null && vP != null)
                {
                    object value = vP.GetValue(source);
                    if (value is IEnumerable)
                        dictionary[kP.GetValue(source).ToNString()] = MapToDictionary(value as IEnumerable);
                    else if (value != null)
                        dictionary[kP.GetValue(source).ToNString()] = MapToDictionary(value);
                    return;
                }
            }
            if (typeof(IChoKeyValueType).IsAssignableFrom(source.GetType()))
            {
                IChoKeyValueType kvp = source as IChoKeyValueType;
                object value = kvp.Value;
                if (value.GetType().IsDynamicType())
                    dictionary[kvp.Key.ToNString()] = value;
                else if (value is IEnumerable && !(value is IDictionary))
                    dictionary[kvp.Key.ToNString()] = MapToDictionary(value as IEnumerable);
                else if (value != null)
                    dictionary[kvp.Key.ToNString()] = MapToDictionary(value);
                return;
            }
            if (source is IDictionary<string, object>)
            {
                foreach (string key in (source as IDictionary<string, object>).Keys)
                {
                    dictionary.Add(key, ((IDictionary<string, object>)source)[key]);
                }
                return;
            }

            var properties = ChoType.GetProperties(source.GetType()); // source.GetType().GetProperties();
            foreach (var p in properties)
            {
                var key = p.Name.StartsWith("_") ? p.Name.Substring(1) : p.Name;
                var attr = p.GetCustomAttribute<JsonPropertyAttribute>();
                if (attr != null && !attr.PropertyName.IsNullOrWhiteSpace())
                    key = attr.PropertyName.NTrim();

                object value = p.GetValue(source, null);
                if (value == null)
                {
                    if (attr != null && attr.NullValueHandling == NullValueHandling.Ignore)
                    {

                    }
                    else
                        dictionary[key] = null;

                    continue;
                }
                valueType = value.GetType();

                if (valueType.IsSimple())
                {
                    dictionary[key] = Marshal(value);
                }
                else if (value.GetType().IsDynamicType())
                {
                    dictionary[key] = value;
                }
                else if (value is IDictionary)
                {
                    IDictionary dict = ((IDictionary)value);
                    foreach (var key1 in dict.Keys)
                    {
                        var val = dict[key];
                        dictionary[key1.ToNString()] = Marshal(value);
                    }
                    dictionary[key] = dict;
                }
                else if (value is IEnumerable)
                    dictionary[key] = MapToDictionary((IEnumerable)value);
                else
                    dictionary[key] = Marshal(value);
            }
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

                if (eoDict.Count != Configuration.JSONRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.JSONRecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.JSONRecordFieldConfigurations.Select(v => v.Name).Except(eoDict.Keys).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = rec == null ? new PropertyDescriptor[] { } : ChoTypeDescriptor.GetProperties<ChoJSONRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.JSONRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.JSONRecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.JSONRecordFieldConfigurations.Select(v => v.Name).Except(pds.Select(pd => pd.Name)).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
        }

        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool? quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false, ChoFieldValueTrimOption? fieldValueTrimOption = null)
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

            //return fieldValue.StartsWith("<![CDATA[") ? fieldValue : System.Net.WebUtility.HtmlEncode(fieldValue);

            //escape quotes
            if (fieldValue.Contains('"'))
                fieldValue = fieldValue.Replace(@"""", @"\""");

            return fieldValue;
        }

        private bool RaiseBeginWrite(object state)
        {
            if (_callbackFileWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFileWrite.BeginWrite(state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeginWrite(state), true);
            }
            return true;
        }

        private void RaiseEndWrite(object state)
        {
            if (_callbackFileWrite != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackFileWrite.EndWrite(state));
            }
            else if (Writer != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseEndWrite(state));
            }
        }

        private bool RaiseBeforeRecordWrite(object target, long index, ref string state)
        {
            if (_callbackRecordWrite != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.BeforeRecordWrite(target, index, ref inState), true);
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
            if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.AfterRecordWrite(target, index, state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordWrite(target, index, state), true);
            }
            return true;
        }

        private bool RaiseRecordWriteError(object target, long index, string state, Exception ex)
        {
            if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.RecordWriteError(target, index, state, ex), false);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordWriteError(target, index, state, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (_callbackRecordFieldWrite != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.BeforeRecordFieldWrite(target, index, propName, ref state), true);

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
            if (_callbackRecordFieldWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackRecordFieldWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.RecordFieldWriteError(target, index, propName, value, ex), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, value, ex), true);
            }
            return true;
        }

        private bool RaiseRecordFieldSerialize(object target, long index, string propName, ref object value)
        {
            if (_callbackRecordSeriablizable != null)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldSerialize(target, index, propName, ref state), false);

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
            return false;
        }
    }
}
