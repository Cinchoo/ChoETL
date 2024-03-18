using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
        private Lazy<List<object>> _recBuffer = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;
        private bool _rowScanComplete = false;
        private int _indent = 1;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }
        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoJSONRecordWriter(Type recordType, ChoJSONRecordConfiguration configuration) : base(recordType, true)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
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
                            sw.Write(String.Format("{0}{1}{0}}}", EOLDelimiter, Indent("]")));
                        }
                    }
                    else
                    {
                        if (!Configuration.SingleElement.Value || (!Configuration.IgnoreNodeName && !Configuration.NodeName.IsNullOrWhiteSpace()))
                            sw.Write(String.Format("{0}}}", EOLDelimiter));
                    }
                }
            }
            catch { }

            RaiseEndWrite(sw);
        }

        private IEnumerable<object> GetRecords(IEnumerator<object> records)
        {
            //object x = Writer != null ? Writer.Context.RecBuffer : null;
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return rec;

            while (records.MoveNext())
                yield return records.Current;
        }
        private string[] GetFields(List<object> records)
        {
            string[] fieldNames = null;
            var record = new Dictionary<string, object>();
            foreach (var r in records.Select(r => (IDictionary<string, Object>)r.ToDynamicObject()))
            {
                record.Merge(r);
            }

            fieldNames = record.Keys.ToArray();
            return fieldNames;
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
            Configuration.ResetStatesInternal();
            _sw = writer;
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (Configuration.JsonSerializerSettings.ContractResolver is ChoPropertyRenameAndIgnoreSerializerContractResolver)
            {
                ChoPropertyRenameAndIgnoreSerializerContractResolver cr = Configuration.JsonSerializerSettings.ContractResolver as ChoPropertyRenameAndIgnoreSerializerContractResolver;
                cr.CallbackRecordFieldWrite = _callbackRecordFieldWrite;
                cr.Writer = Writer;
            }
            if (Configuration.IgnoreFieldValueMode == null)
            {
                if (Configuration.JsonSerializerSettings.NullValueHandling == NullValueHandling.Ignore)
                    Configuration.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null;
            }
            else
            {
                if ((Configuration.IgnoreFieldValueMode | ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null)
                    Configuration.JsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            }

            if (records == null) yield break;
            if (Configuration.SingleElement == null)
                Configuration.SingleElement = false;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            string recText = String.Empty;
            bool recordIgnored = false;
            long recCount = 0;
            string[] combinedFieldNames = null;
            bool abortRequested = false;
            try
            {
                object record = null;
                var recEnum = records.GetEnumerator();

                if (Configuration.FlattenNode)
                {
                    if (RecordType.IsDynamicType())
                        recEnum = GetRecords(recEnum).Select(r => r.ConvertToFlattenObject(Configuration.NestedKeySeparator, 
                            Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix)).GetEnumerator();
                    else
                        recEnum = GetRecords(recEnum).Select(r => r.ToDynamicObject().ConvertToFlattenObject(Configuration.NestedKeySeparator, 
                            Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix)).GetEnumerator();
                }

                object notNullRecord = GetFirstNotNullRecord(recEnum);
                if (notNullRecord == null)
                    yield break;

                if (Configuration.FlattenNode)
                {
                    if (Configuration.MaxScanRows > 0 && !_rowScanComplete)
                    {
                        //List<string> fns = new List<string>();
                        foreach (object record1 in GetRecords(recEnum))
                        {
                            recCount++;

                            if (record1 != null)
                            {
                                if (recCount <= Configuration.MaxScanRows)
                                {
                                    _recBuffer.Value.Add(record1);

                                    if (recCount == Configuration.MaxScanRows)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        _rowScanComplete = true;
                        combinedFieldNames = GetFields(_recBuffer.Value).ToArray();
                    }
                }

                foreach (object rec1 in GetRecords(recEnum))
                {
                    _index++;
                    record = rec1;

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
                            Configuration.RecordTypeInternal = recordType; //.ResolveType();
                            if (!recordType.IsSimple())
                                Configuration.IsDynamicObjectInternal = recordType.IsDynamicType();
                            if (typeof(IDictionary).IsAssignableFrom(Configuration.RecordTypeInternal)
                                || typeof(IList).IsAssignableFrom(Configuration.RecordTypeInternal))
                                Configuration.UseJSONSerialization = true;

                            if (!Configuration.IsDynamicObjectInternal)
                            {
                                if (!Configuration.SingleElement.Value)
                                {
                                    if (Configuration.RootName.IsNullOrWhiteSpace())
                                    {
                                        var root = Configuration.RecordTypeInternal.GetCustomAttribute<ChoJSONNRootNameAttribute>();
                                        if (root != null)
                                        {
                                            Configuration.RootName = root.Name;
                                        }
                                    }

                                    if (Configuration.NodeName.IsNullOrWhiteSpace())
                                    {
                                        var root = Configuration.RecordTypeInternal.GetCustomAttribute<ChoJSONNRootNameAttribute>();
                                        if (root != null)
                                        {
                                            Configuration.NodeName = root.Name;
                                        }
                                        else
                                        {
                                            var xmlRoot = Configuration.RecordTypeInternal.GetCustomAttribute<XmlRootAttribute>();
                                            if (xmlRoot != null)
                                            {
                                                Configuration.NodeName = xmlRoot.ElementName;
                                            }
                                            else
                                                Configuration.NodeName = Configuration.RecordTypeInternal.Name;

                                        }
                                    }
                                }

                                if (Configuration.JSONRecordFieldConfigurations.Count == 0)
                                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                            }

                            if (Configuration.FlattenNode)
                            {
                                fieldNames = combinedFieldNames;
                            }
                            else
                            {
                                if (Configuration.IsDynamicObjectInternal)
                                {
                                    var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                                    fieldNames = dict.Keys.ToArray();
                                }
                                else
                                {
                                    fieldNames = ChoTypeDescriptor.GetProperties<ChoJSONRecordFieldAttribute>(Configuration.RecordTypeInternal).Select(pd => pd.Name).ToArray();
                                    if (fieldNames.Length == 0)
                                    {
                                        fieldNames = ChoType.GetProperties(Configuration.RecordTypeInternal).Select(p => p.Name).ToArray();
                                    }
                                }
                            }

                            Configuration.ValidateInternal(fieldNames);
                            Configuration.IsInitialized = true;

                            if (!BeginWrite.Value)
                                yield break;

                            if (!SupportMultipleContent)
                            {
                                if (Configuration.IgnoreRootName || Configuration.RootName.IsNullOrWhiteSpace())
                                    sw.Write("[");
                                else
                                {
                                    sw.Write($"{{{EOLDelimiter}{Indent(ToJSONToken(Configuration.RootName.NTrim()))}: [");
                                    _indent++;
                                }
                            }
                            else
                            {
                                if (!Configuration.SingleElement.Value || (!Configuration.IgnoreNodeName && !Configuration.NodeName.IsNullOrWhiteSpace()))
                                    sw.Write(String.Format("{{{0}", EOLDelimiter));
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
                            bool skip = false;
                            //if (Configuration.NodeConvertersForType.ContainsKey(RecordType) && Configuration.NodeConvertersForType[RecordType] != null)
                            //    record = Configuration.NodeConvertersForType[RecordType](record);

                            if (record is ChoDynamicObject dobj)
                            {
                                if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                                    record = dobj.IgnoreNullValues();
                            }

                            if (record == null)
                            {
                                if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                                    continue;
                                else if (Configuration.NullValueHandling == ChoNullValueHandling.Default)
                                    record = ChoActivator.CreateInstance(Configuration.RecordTypeInternal);
                                else
                                {
                                    recText = "{{{0}}}".FormatString(EOLDelimiter);
                                    skip = true;
                                }
                            }

                            if (!skip)
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
                                                    if (!Configuration.IgnoreNodeName && !Configuration.NodeName.IsNullOrWhiteSpace())
                                                        sw.Write(Indent(recText));
                                                    else
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
                                    Writer.ContractResolverState = new ChoContractResolverState
                                    {
                                        Index = _index,
                                        Record = ChoActivator.CreateInstanceNCache(RecordType),
                                    };

                                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                                        record.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);

                                    //StringBuilder json = new StringBuilder();
                                    //using (StringWriter sw1 = new StringWriter(json))
                                    //using (JsonWriter jw = new JsonTextWriter(sw1))
                                    //{
                                    //    Configuration.JsonSerializer.Serialize(jw, record);
                                    //}
                                    //recText = json.ToString(); // Configuration.JsonSerializer.Serialize(record, Configuration.Formatting, Configuration.JsonSerializerSettings);
                                    recText = Configuration.JsonSerializer.SerializeToJToken(record, Configuration.Formatting, Configuration.JsonSerializerSettings, 
                                        enableXmlAttributePrefix: Configuration.EnableXmlAttributePrefix, keepNSPrefix: Configuration.KeepNSPrefix)
                                        .JTokenToString(Configuration.JsonSerializer, Configuration.JsonSerializerSettings, Configuration.Formatting);
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
                                ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            }
                            else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                            {
                                if (!RaiseRecordWriteError(record, _index, recText, ex))
                                    throw;
                                else
                                {
                                    recordIgnored = true;
                                    //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                }
                            }
                            else
                                throw;
                        }
                    }


                    if (Configuration.NotifyAfter > 0 && _index % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsWritten(_index))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                            abortRequested = true;
                            yield break;
                        }
                    }

                    yield return record;
                    isFirstRec = false;
                    record = null;
                }

                if (!abortRequested && record != null)
                    RaisedRowsWritten(_index, true);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
            }
        }

        private bool ToText(long index, object rec, out string recText)
        {
            if (typeof(IChoScalarObject).IsAssignableFrom(Configuration.RecordTypeInternal))
                rec = ChoActivator.CreateInstance(Configuration.RecordTypeInternal, rec);

            if (!Configuration.IsDynamicObjectInternal)
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

            bool hasTypeConverter = false;
            bool isJSONObject = true;
            if (!Configuration.IgnoreNodeName)
            {
                if (Configuration.SupportMultipleContent == null || !Configuration.SupportMultipleContent.Value)
                    Configuration.IgnoreNodeName = true;
            }
            if (!Configuration.IgnoreNodeName)
            {
                if (Configuration.NodeName.IsNullOrWhiteSpace())
                {
                    if (Configuration.IsDynamicObjectInternal && rec is ChoDynamicObject && ((ChoDynamicObject)rec).DynamicObjectName != ChoDynamicObject.DefaultName)
                        msg.AppendFormat(@"""{1}"": {{{0}", EOLDelimiter, ResolveName(((ChoDynamicObject)rec).DynamicObjectName));
                    else if (!RecordType.IsSimple())
                        msg.AppendFormat("{{{0}", EOLDelimiter);
                }
                else
                {
                    if (!RecordType.IsSimple())
                        msg.AppendFormat(@"""{1}"": {{{0}", EOLDelimiter, ResolveName(Configuration.NodeName));
                    else
                        msg.AppendFormat(@"""{0}"": ", ResolveName(Configuration.NodeName));
                }
            }
            else if (!RecordType.IsSimple())
            {
                //Has typeconverter specified
                if (ChoTypeConverter.Global.Contains(rec.GetNType()))
                {
                    var tc = ChoTypeConverter.Global.GetConverter(rec.GetNType());
                    if (tc != null)
                    {
                        hasTypeConverter = true;
                        if (Configuration.SourceTypeInternal != null)
                            rec = ChoConvert.ConvertTo(rec, Configuration.SourceTypeInternal, rec, new object[] { tc }, null, Configuration.Culture, null, config: Configuration);
                        else
                            rec = ChoConvert.ConvertTo(rec, rec.GetType() /*typeof(ExpandoObject)*/, rec, new object[] { tc }, null, Configuration.Culture, null, config: Configuration);
                    }
                }

                //if (Configuration.RecordFieldConfigurationsDict.Count == 1)
                //{
                //    var fc = Configuration.RecordFieldConfigurationsDict.First().Value;
                //    if (fc != null && fc.PropConverters != null && fc.PropConverters.OfType<IChoValueOnlyConverter>().Any())
                //        isJSONObject = false;

                //}
                if (!hasTypeConverter)
                {
                    if (isJSONObject)
                        msg.AppendFormat("{{{0}", EOLDelimiter);
                }
            }

            if (!hasTypeConverter)
            {
                foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict.OrderBy(kvp => kvp.Value.Order))
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
                    if (Configuration.PIDictInternal != null)
                    {
                        // if FieldName is set
                        if (!string.IsNullOrEmpty(fieldConfig.FieldName))
                        {
                            // match using FieldName
                            Configuration.PIDictInternal.TryGetValue(fieldConfig.FieldName, out pi);
                        }
                        if (pi == null)
                        {
                            // otherwise match usign the property name
                            Configuration.PIDictInternal.TryGetValue(kvp.Key, out pi);
                        }
                    }
                    rec = GetDeclaringRecord(kvp.Value.DeclaringMemberInternal, rootRec);

                    if (Configuration.ThrowAndStopOnMissingField)
                    {
                        if (fieldConfig.ValueSelector == null)
                        {
                            if (Configuration.IsDynamicObjectInternal)
                            {
                                var dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                                if (!dict.ContainsKey(kvp.Key))
                                    throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' JSON node.".FormatString(fieldConfig.FieldName));
                            }
                            else
                            {
                                if (pi == null)
                                {
                                    if (!RecordType.IsSimple() && RecordType != typeof(object))
                                        throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' JSON node.".FormatString(fieldConfig.FieldName));
                                }
                            }
                        }
                    }

                    try
                    {
                        if (Configuration.IsDynamicObjectInternal)
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

                        if (fieldConfig.IgnoreFieldValueMode == null)
                        {
                            if (fieldValue.IsObjectNullOrEmpty() && fieldConfig.IsDefaultValueSpecifiedInternal)
                                fieldValue = fieldConfig.DefaultValue;
                        }
                        else
                        {
                            bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                            if (ignoreFieldValue && fieldConfig.IsDefaultValueSpecifiedInternal)
                                fieldValue = fieldConfig.DefaultValue;
                            ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                            if (ignoreFieldValue)
                                continue;
                        }

                        if (!RaiseBeforeRecordFieldWrite(rec, index, kvp.Key, ref fieldValue))
                            return false;

                        if (fieldConfig.ValueSelector == null)
                        {
                            if (Configuration.ValueConverterBack != null)
                                fieldValue = Configuration.ValueConverterBack(kvp.Key, fieldValue);
                            else if (fieldConfig.ValueConverterBack != null)
                                fieldValue = fieldConfig.ValueConverterBack(fieldValue);
                            else if (fieldConfig.ValueConverter != null)
                                fieldValue = fieldConfig.ValueConverter(fieldValue);
                            else if (RecordType.IsSimple())
                                fieldValue = rec;
                            else
                                rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, true, config: Configuration);
                        }
                        else
                        {
                            fieldValue = fieldConfig.ValueSelector(rec);
                        }

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
                            throw new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);

                        try
                        {
                            if (Configuration.IsDynamicObjectInternal)
                            {
                                var dict = rec.ToDynamicObject() as IDictionary<string, Object>;

                                if (dict.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration, ref fieldValue))
                                    dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                                else if (dict.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration, ref fieldValue))
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
                                if (rec.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration, ref fieldValue))
                                    rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                                else if (rec.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration, ref fieldValue))
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

                    Writer.ContractResolverState = new ChoContractResolverState
                    {
                        Name = kvp.Key,
                        Index = index,
                        Record = rec,
                        FieldConfig = kvp.Value
                    };

                    bool isSimple = true;

                    if (fieldConfig.CustomSerializer != null)
                    {
                        fieldText = fieldConfig.CustomSerializer(fieldValue) as string;
                    }
                    else if (RaiseRecordFieldSerialize(rec, index, kvp.Key, ref fieldValue))
                    {
                        fieldText = fieldValue as string;
                    }
                    else if (fieldConfig.PropCustomSerializer != null)
                        fieldText = ChoCustomSerializer.Serialize(fieldValue, typeof(string), fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name) as string;
                    else
                    {
                        Type ft = fieldValue == null ? typeof(object) : fieldValue.GetType();
                        if (fieldConfig.IgnoreFieldValue(fieldValue))
                        {
                            fieldText = null;
                        }
                        else if (fieldValue == null)
                        {
                            fieldValue = fieldConfig.IsDefaultValueSpecifiedInternal ? fieldConfig.DefaultValue : fieldValue;
                            //if (fieldConfig.FieldType == null || fieldConfig.FieldType == typeof(object))
                            //{
                            //    if (fieldConfig.NullValue == null)
                            //        fieldText = !fieldConfig.IsArray ? "null" : "[]";
                            //    else
                            //        fieldText = fieldConfig.NullValue;
                            //}
                            if (fieldConfig.NullValueHandling != null)
                            {
                                if (fieldConfig.NullValueHandling.Value == NullValueHandling.Ignore)
                                    continue;
                            }
                            else if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                                fieldText = null;
                            else if (Configuration.NullValueHandling == ChoNullValueHandling.Default)
                                fieldText = JsonConvert.SerializeObject(ChoActivator.CreateInstance(fieldConfig.FieldType), Configuration.Formatting, Configuration.JsonSerializerSettings);
                            else if (Configuration.NullValueHandling == ChoNullValueHandling.Empty && fieldConfig.FieldType == typeof(string))
                                fieldText = String.Empty;
                            else
                            {
                                var nullValue = fieldConfig.NullValue;
                                if (nullValue == null)
                                    nullValue = Configuration.NullValue;

                                if (nullValue == null)
                                {
                                    if (fieldConfig.FieldType == null || fieldConfig.FieldType == typeof(object))
                                    {
                                        var isArray = Configuration.IsArray(fieldConfig, fieldValue);
                                        fieldText = isArray == null || !isArray.Value ? "null" : "[]";
                                    }
                                    else
                                        fieldText = !typeof(IList).IsAssignableFrom(fieldConfig.FieldType) ? "null" : "[]";
                                }
                                else
                                    fieldText = "\"{0}\"".FormatString(nullValue);
                            }
                        }
                        else if (ft == typeof(string) || ft == typeof(char))
                            fieldText = JsonConvert.SerializeObject(NormalizeFieldValue(kvp.Key, fieldValue.ToString(), kvp.Value.Size, kvp.Value.Truncate, false, GetFieldValueJustification(kvp.Value.FieldValueJustification, kvp.Value.FieldType), GetFillChar(kvp.Value.FillChar, kvp.Value.FieldType), false, kvp.Value.GetFieldValueTrimOptionInternal(kvp.Value.FieldType, Configuration.FieldValueTrimOption)),
                                Configuration.Formatting, Configuration.JsonSerializerSettings);
                        else if (ft == typeof(DateTime) || ft == typeof(TimeSpan))
                            fieldText = JsonConvert.SerializeObject(fieldValue, Configuration.Formatting, Configuration.JsonSerializerSettings);
                        else if (ft.IsEnum)
                        {
                            fieldText = JsonConvert.SerializeObject(fieldValue, Configuration.Formatting, Configuration.JsonSerializerSettings);
                        }
                        else if (ft == typeof(ChoCurrency))
                            fieldText = "\"{0}\"".FormatString(fieldValue.ToString());
                        else if (ft == typeof(bool))
                            fieldText = JsonConvert.SerializeObject(fieldValue, Configuration.Formatting, Configuration.JsonSerializerSettings);
                        else if (ft.IsNumeric())
                            fieldText = fieldValue.ToString();
                        else
                        {
                            bool? isArray = Configuration.IsArray(fieldConfig, fieldValue);
                            if (!(fieldValue is IList) && isArray != null && isArray.Value)
                            {
                                fieldValue = new object[] { fieldValue };
                            }

                            isSimple = false;
                        }
                    }

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
                                //if (fieldConfig.PropConverters != null && fieldConfig.PropConverters.OfType<IChoValueOnlyConverter>().Any())
                                //{
                                //    msg.AppendFormat(Indent(SerializeObject(fieldValue, fieldConfig.UseJSONSerialization)).Substring(1));
                                //}
                                //else
                                //{
                                msg.AppendFormat("{2}\"{0}\":{1}", ResolveName(fieldName), isSimple ? " {0}".FormatString(fieldText) :
                                    Indent(SerializeObject(fieldValue, fieldConfig.UseJSONSerialization, fieldConfig)).Substring(1),
                                    Indent(String.Empty));
                                //}
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
                                msg.AppendFormat(",{2}{3}\"{0}\":{1}", ResolveName(fieldName), isSimple ? " {0}".FormatString(fieldText) :
                                    Indent(SerializeObject(fieldValue, fieldConfig.UseJSONSerialization, fieldConfig)).Substring(1),
                                    EOLDelimiter, Indent(String.Empty));
                            }
                        }
                        isFirst = false;
                    }
                }
            }
            else
            {
                fieldText = JsonConvert.SerializeObject(rec, Configuration.Formatting);

                msg.Append(Indent(fieldText)); // (SerializeObject(rec, Configuration.UseJSONSerialization, fieldConfig)));
            }

            if (!RecordType.IsSimple())
            {
                if (!hasTypeConverter)
                {
                    if (isJSONObject)
                        msg.AppendFormat("{0}}}", EOLDelimiter);
                }
            }
            recText = Configuration.IgnoreNodeName ? Unindent(msg.ToString()) : msg.ToString();

            return true;
        }

        private string ResolveName(string name)
        {
            try
            {
                if (Configuration.JSONSerializerContractResolver != null && Configuration.JSONSerializerContractResolver.NamingStrategy != null)
                {
                    name = Configuration.JSONSerializerContractResolver.NamingStrategy.GetPropertyName(name, false);
                }
                else if (Configuration.JsonSerializerSettings != null && Configuration.JsonSerializerSettings.ContractResolver is DefaultContractResolver
                    && ((DefaultContractResolver)Configuration.JsonSerializerSettings.ContractResolver).NamingStrategy != null)
                {
                    name = ((DefaultContractResolver)Configuration.JsonSerializerSettings.ContractResolver).NamingStrategy.GetPropertyName(name, false);
                }
                //else
                //    name = name;
            }
            finally
            {
                if (!Configuration.KeepNSPrefix)
                {
                    if (name.Contains(":"))
                        name = name.Substring(name.IndexOf(":") + 1);
                }
                else if (!name.Contains(":"))
                {
                }
            }

            return name;
        }
        private string ToJSONToken(string name)
        {
            return $"\"{name.NTrim()}\"";
        }

        private string Indent(string value, int? indentValue = null)
        {
            if (value == null)
                return value;

            if (indentValue == null)
                indentValue = _indent;

            return Configuration.Formatting == Formatting.Indented ? value.Indent(indentValue.Value, "  ") : value;
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

        private string SerializeObject(object target, bool? useJSONSerialization = null, ChoJSONRecordFieldConfiguration config = null)
        {
            bool lUseJSONSerialization = useJSONSerialization == null ? Configuration.UseJSONSerialization : useJSONSerialization.Value;
            if (true) //lUseJSONSerialization)
            {
                IContractResolver contractResolver = config != null ? config.ContractResolver : null;
                var savedContractResolver = Configuration.JsonSerializer.ContractResolver;

                try
                {
                    if (contractResolver != null)
                        Configuration.JsonSerializer.ContractResolver = contractResolver;

                    return Configuration.JsonSerializer.SerializeToJToken(target, Configuration.Formatting, Configuration.JsonSerializerSettings,
                        enableXmlAttributePrefix: Configuration.EnableXmlAttributePrefix, keepNSPrefix: Configuration.KeepNSPrefix)
                        .JTokenToString(Configuration.JsonSerializer, Configuration.JsonSerializerSettings, Configuration.Formatting);
                }
                finally
                {
                    if (contractResolver != null)
                        Configuration.JsonSerializer.ContractResolver = savedContractResolver;
                }
            }
            //return JsonConvert.SerializeObject(target, Configuration.Formatting, Configuration.JsonSerializerSettings);
            else
            {
                //return JsonConvert.SerializeObject(target, Configuration.Formatting);

                Type objType = target.GetType();
                if (objType.IsSimple())
                    return JsonConvert.SerializeObject(target, Configuration.Formatting, Configuration.JsonSerializerSettings);
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
                                    msg.Append(JsonConvert.SerializeObject(null, Configuration.Formatting, Configuration.JsonSerializerSettings));
                            }
                            else if (item.GetType().IsSimple())
                                msg.Append(JsonConvert.SerializeObject(item, Configuration.Formatting, Configuration.JsonSerializerSettings));
                            else
                            {
                                //var obj = MapToDictionary(item);
                                msg.Append(JsonConvert.SerializeObject(item, Configuration.Formatting, Configuration.JsonSerializerSettings));
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

                    string newKey;
                    dict = new Dictionary<string, object>();
                    foreach (var kvp in dobj)
                    {
                        newKey = kvp.Key;
                        if (!Configuration.KeepNSPrefix)
                        {
                            if (newKey.IndexOf(":") > 0)
                                newKey = newKey.Substring(newKey.IndexOf(":") + 1);
                        }

                        if (dobj.IsAttribute(kvp.Key) && Configuration.EnableXmlAttributePrefix)
                            dict.Add("@{0}".FormatString(newKey), kvp.Value);
                        else
                            dict.Add(newKey, kvp.Value);
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
            if (Configuration.IsDynamicObjectInternal)
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
                            fieldValue = fieldValue.Replace(EOLDelimiter, Configuration.LineBreakChars); // "\"{0}\"".FormatString(fieldValue);
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
            //if (fieldValue.Contains('"'))
            //{
            //    fieldValue = fieldValue.Replace(@"""", @"\""");
            //}

            return fieldValue;
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

    }
}
