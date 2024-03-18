using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpYaml.Serialization;
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
    internal class ChoYamlRecordWriter : ChoRecordWriter
    {
        private IChoNotifyFileWrite _callbackFileWrite;
        private IChoNotifyRecordWrite _callbackRecordWrite;
        private IChoNotifyRecordFieldWrite _callbackRecordFieldWrite;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private long _index = 0;
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        private Lazy<List<object>> _recBuffer = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;
        private bool _rowScanComplete = false;

        public ChoYamlRecordConfiguration Configuration
        {
            get;
            private set;
        }
        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoYamlRecordWriter(Type recordType, ChoYamlRecordConfiguration configuration) : base(recordType, true)
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

        internal void EndWrite(object writer)
        {
            TextWriter sw = writer as TextWriter;

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

            if (records == null) yield break;
            if (Configuration.SingleDocument == null)
                Configuration.SingleDocument = false;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            string recText = String.Empty;
            bool recordIgnored = false;
            long recCount = 0;
            string[] combinedFieldNames = null;
            try
            {
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
                                    if (!record1.GetType().IsDynamicType())
                                        throw new ChoParserException("Invalid record found.");

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

                object record = null;
                bool abortRequested = false;

                foreach (object rec in GetRecords(recEnum))
                {
                    _index++;
                    record = rec;

                    //if (!isFirstRec)
                    //{
                    //    if (!recordIgnored)
                    //        sw.Write(",");
                    //    else
                    //        recordIgnored = false;
                    //}

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
                            if (Configuration.RecordTypeInternal == typeof(object))
                            {
                                Type recordType = ElementType == null ? record.GetType() : ElementType;
                                RecordType = Configuration.RecordTypeInternal = recordType.GetUnderlyingType(); //.ResolveType();
                                Configuration.IsDynamicObjectInternal = recordType.IsDynamicType();
                            }
                            if (typeof(IDictionary).IsAssignableFrom(Configuration.RecordTypeInternal)
                                || typeof(IList).IsAssignableFrom(Configuration.RecordTypeInternal))
                                Configuration.UseYamlSerialization = true;

                            if (!Configuration.IsDynamicObjectInternal)
                            {
                                if (Configuration.YamlRecordFieldConfigurations.Count == 0)
                                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                            }

                            if (Configuration.IsDynamicObjectInternal)
                            {
                                var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                                fieldNames = dict.Keys.ToArray();
                            }
                            else
                            {
                                fieldNames = ChoTypeDescriptor.GetProperties<ChoYamlRecordFieldAttribute>(Configuration.RecordTypeInternal).Select(pd => pd.Name).ToArray();
                                if (fieldNames.Length == 0)
                                {
                                    fieldNames = ChoType.GetProperties(Configuration.RecordTypeInternal).Select(p => p.Name).ToArray();
                                }
                            }

                            Configuration.ValidateInternal(fieldNames);

                            Configuration.IsInitialized = true;

                            if (!BeginWrite.Value)
                                yield break;
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
                            if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                record.DoObjectLevelValidation(Configuration, Configuration.YamlRecordFieldConfigurations);

                            if (ToText(_index, record, out recText))
                            {
                                if (recText.EndsWith("..."))
                                    recText = recText.Left(recText.Length - 3);

                                if (recText.EndsWith($"...{Environment.NewLine}"))
                                    recText = recText.Left(recText.Length - $"...{Environment.NewLine}".Length);

                                if (!Configuration.SingleDocument.Value)
                                    sw.Write($"---{EOLDelimiter}");

                                sw.Write("{0}", recText);

                                if (!Configuration.SingleDocument.Value)
                                    sw.Write($"...{EOLDelimiter}");

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

                    yield return record;
                    record = null;

                    if (Configuration.NotifyAfter > 0 && _index % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsWritten(_index))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                            abortRequested = true;
                            yield break;
                        }
                    }
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
            if (Configuration.UseYamlSerialization)
            {
                if (Configuration.UseJsonSerialization)
                {
                    Writer.ContractResolverState = new ChoContractResolverState
                    {
                        Index = index,
                        Record = rec,
                    };
                    var json = JsonConvert.SerializeObject(rec, Configuration.JsonSerializerSettings);
                    rec = JsonConvert.DeserializeObject(json, Configuration.JsonSerializerSettings);

                    Type targetType = Configuration.TargetRecordType ?? typeof(ExpandoObject); // typeof(Dictionary<string, object>);
                    if (rec is JArray)
                    {
                        rec = ((JArray)rec).Children<JObject>().Select(o => o.ToObject(targetType)).ToArray();
                    }
                    else if (rec is JObject)
                    {
                        rec = ((JObject)rec).ToObject(targetType);
                    }
                    else if (rec is JValue)
                    {
                        if (targetType is IDictionary)
                        {
                            var dict = new Dictionary<string, object>();
                            dict.Add("Value", ((JValue)rec).Value);

                            rec = dict;
                        }
                        else
                            rec = ((JValue)rec).Value.CastObjectTo(targetType);
                    }
                }

                recText = Configuration.YamlSerializer.Serialize(rec);
                return true;
            }
            StringBuilder msg = new StringBuilder();
            object fieldValue = null;
            string fieldText = null;
            ChoYamlRecordFieldConfiguration fieldConfig = null;
            string fieldName = null;

            if (Configuration.ColumnCountStrict)
                CheckColumnsStrict(rec);

            //bool firstColumn = true;
            PropertyInfo pi = null;
            bool isFirst = true;
            object rootRec = rec;

            Dictionary<string, object> output = new Dictionary<string, object>();
            foreach (KeyValuePair<string, ChoYamlRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict.OrderBy(kvp => kvp.Value.Order))
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
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Yaml node.".FormatString(fieldConfig.FieldName));
                        }
                        else
                        {
                            if (pi == null)
                            {
                                if (!RecordType.IsSimple())
                                    throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Yaml node.".FormatString(fieldConfig.FieldName));
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
                                var ft1 = dobj.GetMemberType(kvp.Key);
                                if (ft1 != null)
                                    kvp.Value.FieldType = ft1;
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
                            fieldValue = new List<object> { rec };
                        else
                            rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, true, config: Configuration);
                    }
                    else
                    {
                        fieldValue = fieldConfig.ValueSelector(rec);
                        rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, true, config: Configuration);
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

                bool isSimple = true;

                Type ft = fieldValue == null ? typeof(object) : fieldValue.GetType();

                if (fieldConfig.CustomSerializer != null)
                    fieldText = fieldConfig.CustomSerializer(fieldValue) as string;
                else if (RaiseRecordFieldSerialize(rec, index, kvp.Key, ref fieldValue))
                    fieldText = fieldValue as string;
                else if (fieldConfig.PropCustomSerializer != null)
                    fieldText = ChoCustomSerializer.Serialize(fieldValue, typeof(string), fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name) as string;
                else
                {
                    //if (fieldConfig.IgnoreFieldValue(fieldValue))
                    //    fieldText = null;

                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
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
                            fieldText = ChoActivator.CreateInstance(fieldConfig.FieldType).ToNString();
                        else if (Configuration.NullValueHandling == ChoNullValueHandling.Empty && fieldConfig.FieldType == typeof(string))
                            fieldText = String.Empty;
                        else
                        {
                            if (fieldConfig.NullValue == null)
                            {
                            }
                            else
                                fieldText = fieldConfig.NullValue;
                        }
                    }
                    else if (ft == typeof(string) || ft == typeof(char))
                        fieldText = NormalizeFieldValue(kvp.Key, fieldValue.ToString(), kvp.Value.Size, kvp.Value.Truncate, false, GetFieldValueJustification(kvp.Value.FieldValueJustification, kvp.Value.FieldType),
                            GetFillChar(kvp.Value.FillChar, kvp.Value.FieldType), false, kvp.Value.GetFieldValueTrimOptionInternal(kvp.Value.FieldType, Configuration.FieldValueTrimOption));
                    else if (ft == typeof(DateTime) || ft == typeof(TimeSpan))
                        fieldText = ChoConvert.ConvertTo(fieldValue, typeof(String), Configuration.Culture, config: Configuration) as string;
                    else if (ft.IsEnum)
                        fieldText = ChoConvert.ConvertTo(fieldValue, typeof(String), Configuration.Culture, config: Configuration) as string;
                    else if (ft == typeof(ChoCurrency))
                        fieldText = fieldValue.ToString();
                    else if (ft == typeof(bool))
                        fieldText = ChoConvert.ConvertTo(fieldValue, typeof(String), Configuration.Culture, config: Configuration) as string;
                    else if (ft.IsNumeric())
                        fieldText = ChoConvert.ConvertTo(fieldValue, typeof(String), Configuration.Culture, config: Configuration) as string;
                    else
                        isSimple = false;
                }

                object objValue = null;
                if (isSimple)
                    objValue = fieldText;
                else if (fieldValue is ChoDynamicObject)
                    objValue = ((ChoDynamicObject)fieldValue).AsDictionary();
                else
                {
                    Writer.ContractResolverState = new ChoContractResolverState
                    {
                        Name = kvp.Key,
                        Index = index,
                        Record = rec,
                        FieldConfig = kvp.Value
                    };
                    var json = JsonConvert.SerializeObject(fieldValue, Configuration.JsonSerializerSettings);

                    if (Configuration.TargetRecordType == null)
                    {
                        if (RecordType.IsSimple())
                        {
                            var type = typeof(IList<>).MakeGenericType(RecordType);
                            objValue = JsonConvert.DeserializeObject(json, type, Configuration.JsonSerializerSettings);
                        }
                        else if (typeof(IList).IsAssignableFrom(ft))
                            objValue = JsonConvert.DeserializeObject<IList<ChoDynamicObject>>(json, Configuration.JsonSerializerSettings)
                                .Select(ele => Unpack(ele)).ToList();
                        else
                            objValue = JsonConvert.DeserializeObject<IDictionary<string, object>>(json, Configuration.JsonSerializerSettings)
                                .ToDictionary(kvp1 => kvp1.Key, kvp1 => Unpack(kvp1.Value));
                    }
                    else
                    {
                        objValue = JsonConvert.DeserializeObject(json, Configuration.TargetRecordType, Configuration.JsonSerializerSettings);
                    }
                }

                if (!RecordType.IsSimple())
                    output.Add(fieldName, objValue);
                else
                    fieldValue = objValue;
            }

            if (!RecordType.IsSimple())
                recText = Configuration.YamlSerializer.Serialize(output, output.GetType());
            else
                recText = Configuration.YamlSerializer.Serialize(fieldValue, fieldValue.GetType());

            return true;
        }

        private object Unpack(object value)
        {
            if (value == null)
                return value;

            if (value is JValue)
                return ((JValue)value).Value;
            else if (value is JObject)
            {
                ChoDynamicObject dobj = new ChoDynamicObject();
                foreach (var kvp in (JObject)value)
                {
                    dobj.Add(kvp.Key, Unpack(kvp.Value));
                }
                return dobj.AsDictionary();
            }
            else if (value is JArray)
            {
                List<object> array = new List<object>();
                foreach (var item in (JArray)value)
                {
                    array.Add(Unpack(item));
                }
                return array.ToArray();
            }
            else if (value is ChoDynamicObject dobj)
                return dobj.AsDictionary();
            else if (value is ExpandoObject eobj)
                return new ChoDynamicObject(eobj).AsDictionary();
            else
                return value;
        }

        private string Indent(string value, int indentValue = 1)
        {
            return value;
        }

        private string Unindent(string value)
        {
            return value;
        }

        private string EOLDelimiter
        {
            get
            {
                return Configuration.EOLDelimiter;
            }
        }

        private string SerializeObject(object target, bool? useYamlSerialization = null)
        {
            return null;
        }

        private object SimpleTypeValue(object source)
        {
            if (source.GetType() == typeof(ChoCurrency))
                return ((ChoCurrency)source).Amount;
            else
                return source;
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

                if (eoDict.Count != Configuration.YamlRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.YamlRecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.YamlRecordFieldConfigurations.Select(v => v.Name).Except(eoDict.Keys).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = rec == null ? new PropertyDescriptor[] { } : ChoTypeDescriptor.GetProperties<ChoYamlRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.YamlRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.YamlRecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.YamlRecordFieldConfigurations.Select(v => v.Name).Except(pds.Select(pd => pd.Name)).ToArray();
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
