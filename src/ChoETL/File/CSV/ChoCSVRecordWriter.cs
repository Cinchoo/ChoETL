using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChoETL
{
    internal class ChoCSVRecordWriter : ChoRecordWriter
    {
        private IChoNotifyFileHeaderArrange _callbackFileHeaderArrange;
        private IChoNotifyFileHeaderWrite _callbackFileHeaderWrite;
        private IChoNotifyFileWrite _callbackFileWrite;
        private IChoNotifyRecordWrite _callbackRecordWrite;
        private IChoNotifyRecordFieldWrite _callbackRecordFieldWrite;
        private bool _configCheckDone = false;
        private long _index = 0;
        private bool _hadHeaderWritten = false;
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        private Lazy<List<object>> _recBuffer = null;
        private bool _firstLine = true;
        private string _customHeader = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordWriter(Type recordType, ChoCSVRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackFileHeaderArrange = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileHeaderArrange>(recordType);
            _callbackFileHeaderWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileHeaderWrite>(recordType);
            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);

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

            BeginWrite = new Lazy<bool>(() =>
            {
                TextWriter sw = _sw as TextWriter;
                if (sw != null)
                    return RaiseBeginWrite(sw);

                return false;
            });
            //Configuration.Validate();
        }

        public void Dispose()
        {
            TextWriter sw = _sw as TextWriter;
            if (sw != null)
                RaiseEndWrite(sw);
        }

        //private List<object> _recBuffer = new List<object>();
        //private IEnumerable<object> GetRecords(IEnumerator<object> records)
        //{
        //    foreach (var rec in _recBuffer)
        //        yield return rec;

        //    while (records.MoveNext())
        //        yield return records.Current;
        //}

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

        private object GetFirstNotNullRecord(IEnumerator<object> recEnum)
        {
            if (Writer != null && !Object.ReferenceEquals(Writer.Context.FirstNotNullRecord, null))
                return Writer.Context.FirstNotNullRecord;

            while (recEnum.MoveNext())
            {
                _recBuffer.Value.Add(recEnum.Current);
                if (recEnum.Current != null)
                {
                    if (Writer != null)
                    {
                        Writer.Context.FirstNotNullRecord = recEnum.Current;
                        return Writer.Context.FirstNotNullRecord;
                    }
                    else
                        return recEnum.Current;
                }
            }
            return null;
        }

        public void WriteFields(object writer, params object[] fieldValues)
        {
            _sw = writer;
            if (fieldValues == null)
                return;

            if (!BeginWrite.Value)
                return;

            bool first = true;
            StringBuilder line = new StringBuilder();
            foreach (var fv in fieldValues)
            {
                if (first)
                    first = false;
                else
                    line.Append(Configuration.Delimiter);

                line.Append(fv.ToNString());
            }
            Write(writer, line.ToString());
        }

        public void WriteHeader(object writer, params string[] fieldNames)
        {
            if (fieldNames.IsNullOrEmpty())
                return;

            _customHeader = String.Join(Configuration.Delimiter, fieldNames);
        }

        public void WriteCustomHeader(object writer, string header)
        {
            if (header.IsNullOrEmpty())
                return;

            _customHeader = header;
        }

        public void WriteComment(object writer, string commentText, bool silent = true)
        {
            _sw = writer;
            if (Configuration.Comments.IsNullOrEmpty())
            {
                if (silent) return;
                throw new ChoParserException("No comment character set.");
            }

            if (!BeginWrite.Value)
                return;

            string comment = Configuration.Comments.First();
            Write(writer, $"{comment}{commentText}");
        }

        private void Write(object writer, string text)
        {
            _sw = writer;
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (_firstLine)
                sw.Write(text);
            else
                sw.Write("{1}{0}", text, Configuration.EOLDelimiter);

            _firstLine = false;
        }

        private bool _rowScanComplete = false;
        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            _sw = writer;
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (records == null) yield break;

            if (!BeginWrite.Value)
                yield break;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            string recText = String.Empty;
            long recCount = 0;
            var recEnum = records.GetEnumerator();

            try
            {
                object notNullRecord = GetFirstNotNullRecord(recEnum);
                if (notNullRecord == null)
                    yield break;

                if (Configuration.IsDynamicObject)
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

                                    //var fns1 = GetFields(record1).ToList();
                                    //if (fns.Count < fns1.Count)
                                    //    fns = fns1.Union(fns).ToList();
                                    //else
                                    //    fns = fns.Union(fns1).ToList();

                                    if (recCount == Configuration.MaxScanRows)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        _rowScanComplete = true;
                        var fns = GetFields(_recBuffer.Value).ToList();
                        RaiseFileHeaderArrange(ref fns);

                        Configuration.Validate(fns.ToArray());
                        WriteHeaderLine(sw);
                        _configCheckDone = true;
                    }
                }

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
                    if (record != null)
                    {
                        if (predicate == null || predicate(record))
                        {
                            //Discover and load CSV columns from first record
                            if (!_configCheckDone)
                            {
                                if (notNullRecord != null)
                                {
                                    var fieldNames = GetFields(notNullRecord).ToList();
                                    RaiseFileHeaderArrange(ref fieldNames);
                                    Configuration.Validate(fieldNames.ToArray());
                                    WriteHeaderLine(sw);
                                    _configCheckDone = true;
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
                            else if (recText.Length > 0)
                            {
                                //sw.Write("{1}{0}", recText, _hadHeaderWritten || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                                Write(sw, recText);
                                continue;
                            }

                            try
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.CSVRecordFieldConfigurations);

                                if (ToText(_index, record, out recText))
                                {
                                    //if (_index == 1)
                                    //    sw.Write("{1}{0}", recText, _hadHeaderWritten || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                                    //else
                                    //    sw.Write("{1}{0}", recText, Configuration.EOLDelimiter);
                                    Write(sw, recText);

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

        private string[] GetFields(List<object> records)
        {
            string[] fieldNames = null;
            Type recordType = ElementType == null ? records.First().GetType() : ElementType;
            Configuration.RecordType = recordType.ResolveType();

            Configuration.IsDynamicObject = recordType.IsDynamicType();
            if (!Configuration.IsDynamicObject)
            {
                if (Configuration.CSVRecordFieldConfigurations.Count == 0)
                    Configuration.MapRecordFields(Configuration.RecordType);
            }

            if (Configuration.IsDynamicObject)
            {
                var record = new Dictionary<string, object>();
                foreach (var r in records.Select(r => (IDictionary<string, Object>)r.ToDynamicObject()))
                {
                    record.Merge(r);
                }

                if (Configuration.UseNestedKeyFormat)
                    fieldNames = record.Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToDictionary().Keys.ToArray();
                else
                    fieldNames = record.Keys.ToArray();
            }
            else
            {
                fieldNames = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(Configuration.RecordType).Select(pd => pd.Name).ToArray();
                if (fieldNames.Length == 0)
                {
                    fieldNames = ChoType.GetProperties(Configuration.RecordType).Select(p => p.Name).ToArray();
                }
            }
            return fieldNames;
        }

        private string[] GetFields(object record)
        {
            string[] fieldNames = null;
            Type recordType = ElementType == null ? record.GetType() : ElementType;
            Configuration.RecordType = recordType.ResolveType();

            Configuration.IsDynamicObject = recordType.IsDynamicType();
            if (!Configuration.IsDynamicObject)
            {
                if (Configuration.CSVRecordFieldConfigurations.Count == 0)
                    Configuration.MapRecordFields(Configuration.RecordType);
            }

            if (Configuration.IsDynamicObject)
            {
                var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                if (Configuration.UseNestedKeyFormat)
                {
                    if (Configuration.IgnoreRootNodeName && dict is ChoDynamicObject)
                    {
                        ((ChoDynamicObject)dict).DynamicObjectName = ChoDynamicObject.DefaultName;
                    }
                    fieldNames = dict.Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToArray().ToDictionary().Keys.ToArray();
                }
                else
                    fieldNames = dict.Keys.ToArray();
            }
            else
            {
                fieldNames = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(Configuration.RecordType).Select(pd => pd.Name).ToArray();
                if (fieldNames.Length == 0)
                {
                    fieldNames = ChoType.GetProperties(Configuration.RecordType).Select(p => p.Name).ToArray();
                }
            }
            return fieldNames;
        }

        StringBuilder msg = new StringBuilder(6400);
        object fieldValue = null;
        string fieldText = null;
        ChoCSVRecordFieldConfiguration fieldConfig = null;
        IDictionary<string, Object> dict = null;
        private bool ToText(long index, object rec, out string recText)
        {
            if (typeof(IChoScalarObject).IsAssignableFrom(Configuration.RecordType))
                rec = ChoActivator.CreateInstance(Configuration.RecordType, rec);

            recText = null;
            msg.Clear();

            if (Configuration.ColumnCountStrict)
                CheckColumnCountStrict(rec);
            if (Configuration.ColumnOrderStrict)
                CheckColumnOrderStrict(rec);

            bool isInit = false;
            bool firstColumn = true;
            PropertyInfo pi = null;
            object rootRec = rec;
            foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict.OrderBy(kvp => kvp.Value.Priority))
            {
                //if (Configuration.IsDynamicObject)
                //{
                if (Configuration.IgnoredFields.Contains(kvp.Key))
                    continue;
                //}

                fieldConfig = kvp.Value;
                fieldValue = null;
                fieldText = String.Empty;
               
                if (Configuration.PIDict != null)
                {
                    // if FieldName is set
                    if (!string.IsNullOrEmpty(fieldConfig.FieldName))
                    {
                        // match using FieldName
                        Configuration.PIDict.TryGetValue(fieldConfig.FieldName, out pi);
                    }
                    if (pi == null)
                    {
                        // otherwise match usign the property name
                        Configuration.PIDict.TryGetValue(kvp.Key, out pi);
                    }
                }

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec, fieldConfig.ArrayIndex);

                if (!isInit)
                {
                    isInit = true;
                    if (Configuration.IsDynamicObject)
                        dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                    if (Configuration.IsDynamicObject && Configuration.UseNestedKeyFormat)
                    {
                        if (Configuration.IgnoreRootNodeName && dict is ChoDynamicObject)
                        {
                            ((ChoDynamicObject)dict).DynamicObjectName = ChoDynamicObject.DefaultName;
                        }

                        dict = dict.Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToArray().ToDictionary();
                    }
                }

                if (Configuration.ThrowAndStopOnMissingField)
                {
                    if (Configuration.IsDynamicObject)
                    {
                        if (!dict.ContainsKey(kvp.Key))
                        {
                            if (!Configuration.FileHeaderConfiguration.IgnoreHeader)
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                            if (fieldConfig.FieldPosition > dict.Count)
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                        }
                    }
                    else
                    {
                        if (pi == null)
                            pi = Configuration.PIDict.Where(kvp1 => kvp.Value.FieldPosition == kvp.Value.FieldPosition).FirstOrDefault().Value;

                        if (pi == null)
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                    }
                }

                try
                {
                    if (Configuration.IsDynamicObject)
                    {
                        if (Configuration.FileHeaderConfiguration.IgnoreHeader)
                            fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] :
                                fieldConfig.FieldPosition > 0 && fieldConfig.FieldPosition - 1 < dict.Keys.Count
                                && Configuration.RecordFieldConfigurationsDict.Count == dict.Keys.Count ? dict[dict.Keys.ElementAt(fieldConfig.FieldPosition - 1)] : null; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
                        else
                            fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] : null;

                        if (kvp.Value.FieldType == null)
                        {
                            if (rec is ChoDynamicObject)
                            {
                                var dobj = rec as ChoDynamicObject;
                                kvp.Value.FieldType = dobj.GetMemberType(kvp.Key);
                            }
                            if (kvp.Value.FieldType == null)
                            {
                                if (fieldValue == null)
                                    kvp.Value.FieldType = typeof(string);
                                else
                                    kvp.Value.FieldType = fieldValue.GetType();
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
                            fieldValue = GetPropertyValue(rec, pi, fieldConfig);
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

                    if (fieldConfig.ValueSelector == null)
                    {
                        if (fieldConfig.ValueConverter != null)
                            fieldValue = fieldConfig.ValueConverter(fieldValue);
                        else
                            rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue);
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
                        throw;

                    try
                    {
                        if (Configuration.IsDynamicObject)
                        {
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

                var quoteField = kvp.Value.QuoteField;
                if (fieldValue == null)
                    fieldText = String.Empty;
                else
                {
                    if (fieldValue is IList)
                    {
                        quoteField = false;

                        StringBuilder sb = new StringBuilder();
                        bool first = true;
                        foreach (var item in (IList)fieldValue)
                        {
                            if (first)
                            {
                                sb.Append(NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, quoteField, ChoFieldValueJustification.None, GetFillChar(kvp.Value.FillChar)));
                                first = false;
                            }
                            else
                                sb.AppendFormat("{0}{1}", Configuration.ArrayValueSeparator == null ? ',' : Configuration.ArrayValueSeparator, NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, quoteField, ChoFieldValueJustification.None, GetFillChar(kvp.Value.FillChar)));
                        }
                        fieldText = sb.ToString();
                    }
                    else
                        fieldText = fieldValue.ToString();
                }

                quoteField = kvp.Value.QuoteField;
                if (firstColumn)
                {
                    msg.Append(NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, quoteField,
                        GetFieldValueJustification(kvp.Value.FieldValueJustification), GetFillChar(kvp.Value.FillChar),
                        false, kvp.Value.NullValue, kvp.Value.GetFieldValueTrimOption(kvp.Value.FieldType, Configuration.FieldValueTrimOption), fieldConfig));
                    firstColumn = false;
                }
                else
                    msg.AppendFormat("{0}{1}", Configuration.Delimiter, NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, quoteField, GetFieldValueJustification(kvp.Value.FieldValueJustification),
                        GetFillChar(kvp.Value.FillChar), false, kvp.Value.NullValue, kvp.Value.GetFieldValueTrimOption(kvp.Value.FieldType, Configuration.FieldValueTrimOption), fieldConfig));
            }

            recText = msg.ToString();
            return true;
        }

        public object GetPropertyValue(object target, PropertyInfo propertyInfo, ChoCSVRecordFieldConfiguration fieldConfig)
        {
            if (typeof(IList).IsAssignableFrom(target.GetType()))
            {
                if (fieldConfig.ArrayIndex != null)
                {
                    var item = ((IList)target).OfType<object>().Skip(fieldConfig.ArrayIndex.Value).FirstOrDefault();
                    if (item != null)
                        return ChoType.GetPropertyValue(item, propertyInfo);
                }
                return null;
            }
            else
            {
                var item = ChoType.GetPropertyValue(target, propertyInfo);
                if (item != null && typeof(IList).IsAssignableFrom(item.GetType()))
                {
                    if (fieldConfig.ArrayIndex != null)
                    {
                        return ((IList)item).OfType<object>().Skip(fieldConfig.ArrayIndex.Value).FirstOrDefault();
                    }
                    return item;
                }
                else if (item != null && typeof(IDictionary<string, object>).IsAssignableFrom(item.GetType()))
                {
                    if (fieldConfig.DictKey != null && ((IDictionary<string, object>)item).ContainsKey(fieldConfig.DictKey))
                    {
                        return ((IDictionary<string, object>)item)[fieldConfig.DictKey];
                    }
                    else
                        return null;
                }
                else
                    return item;
            }
        }

        private ChoFieldValueJustification GetFieldValueJustification(ChoFieldValueJustification? fieldValueJustification)
        {
            return fieldValueJustification == null ? ChoFieldValueJustification.None : fieldValueJustification.Value;
        }

        private char GetFillChar(char? fillChar)
        {
            return fillChar == null ? ' ' : fillChar.Value;
        }

        private void CheckColumnOrderStrict(object rec)
        {
            if (Configuration.IsDynamicObject)
            {
                var eoDict = rec.ToDynamicObject() as IDictionary<string, Object>;

                if (!Enumerable.SequenceEqual(Configuration.CSVRecordFieldConfigurations.OrderBy(v => v.FieldPosition).Select(v => v.Name), eoDict.Keys))
                    throw new ChoParserException("Incorrect column order found.");
            }
            else
            {
                PropertyDescriptor[] pds = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(rec.GetType()).ToArray();
                if (!Enumerable.SequenceEqual(Configuration.CSVRecordFieldConfigurations.OrderBy(v => v.FieldPosition).Select(v => v.Name), pds.Select(pd => pd.Name)))
                    throw new ChoParserException("Incorrect column order found.");
            }
        }

        private void CheckColumnCountStrict(object rec)
        {
            if (Configuration.IsDynamicObject)
            {
                var eoDict = rec.ToDynamicObject() as IDictionary<string, Object>;

                if (eoDict.Count != Configuration.CSVRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.CSVRecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.CSVRecordFieldConfigurations.Select(v => v.Name).Except(eoDict.Keys/*, Configuration.FileHeaderConfiguration.StringComparer*/).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.CSVRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.CSVRecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.CSVRecordFieldConfigurations.Select(v => v.Name).Except(pds.Select(pd => pd.Name)/*, Configuration.FileHeaderConfiguration.StringComparer*/).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
        }

        private void WriteHeaderLine(TextWriter sw)
        {
            if (HasExcelSeparator && _firstLine)
                Write(sw, "sep={0}".FormatString(Configuration.Delimiter));

            if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
            {
                string header = ToHeaderText();
                if (RaiseFileHeaderWrite(ref header))
                {
                    if (header.IsNullOrWhiteSpace())
                        return;

                    //sw.Write("{1}{0}", header, HasExcelSeparator ? Configuration.EOLDelimiter : "");
                    Write(sw, header);
                    _hadHeaderWritten = true;
                }
            }
        }

        private bool HasExcelSeparator
        {
            get
            {
                return Configuration.HasExcelSeparator != null && Configuration.HasExcelSeparator.Value;
            }
        }

        private string ToHeaderText()
        {
            if (!_customHeader.IsNullOrWhiteSpace())
                return _customHeader;

            string delimiter = Configuration.Delimiter;
            StringBuilder msg = new StringBuilder();
            string value;
            foreach (var member in Configuration.RecordFieldConfigurationsDict.OrderBy(kvp => kvp.Value.Priority).Select(kvp => kvp.Value).ToArray())
            {
                if (Configuration.IgnoredFields.Contains(member.Name))
                    continue;

                if (member.HeaderSelector == null)
                {
                    var ignoreCheckDelimiter = false;
                    var fv = member.FieldName;
                    if (member.PropConverters != null)
                    {
                        var vc = member.PropConverters.OfType<IChoHeaderConverter>().FirstOrDefault();
                        if (vc != null)
                        {
                            fv = vc.GetHeader(member.Name, member.FieldName, member.PropConverterParams, Configuration.Culture);
                            ignoreCheckDelimiter = true;
                        }
                    }
                    value = NormalizeFieldValue(member.Name, fv, null,
                    Configuration.FileHeaderConfiguration.Truncate == null ? true : Configuration.FileHeaderConfiguration.Truncate.Value,
                        Configuration.FileHeaderConfiguration.QuoteAllHeaders,
                        Configuration.FileHeaderConfiguration.Justification == null ? ChoFieldValueJustification.None : Configuration.FileHeaderConfiguration.Justification.Value,
                        Configuration.FileHeaderConfiguration.FillChar == null ? ' ' : Configuration.FileHeaderConfiguration.FillChar.Value,
                        true, null, null, member, ignoreCheckDelimiter);
                }
                else
                    value = member.HeaderSelector();

                if (msg.Length == 0)
                    msg.Append(value);
                else
                    msg.AppendFormat("{0}{1}", delimiter, value);
            }

            return msg.ToString();
        }

        private char[] searchStrings = null;
        bool quoteValue = false;
        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool? quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false, string nullValue = null,
            ChoFieldValueTrimOption? fieldValueTrimOption = null, ChoCSVRecordFieldConfiguration fieldConfig = null,
            bool ignoreCheckDelimiter = false)
        {
            string lFieldValue = fieldValue;
            bool retValue = false;
            quoteValue = quoteField != null ? quoteField.Value : false;

            if (retValue)
                return lFieldValue;

            if (fieldValue.IsNull())
                fieldValue = String.Empty;

            if (fieldValue.StartsWith(Configuration.QuoteChar.ToString()) && fieldValue.EndsWith(Configuration.QuoteChar.ToString()))
            {

            }
            else
            {
                if (quoteField == null)
                {
                    if (searchStrings == null)
                        searchStrings = (Configuration.QuoteChar.ToString() + Configuration.Delimiter + Configuration.EOLDelimiter).ToArray();

                    if (fieldValue.IndexOfAny(searchStrings) >= 0)
                    {
                        //******** ORDER IMPORTANT *********

                        //Fields that contain double quote characters must be surounded by double-quotes, and the embedded double-quotes must each be represented by a pair of consecutive double quotes.
                        if (fieldValue.IndexOf(Configuration.QuoteChar) >= 0)
                        {
                            if (!Configuration.EscapeQuoteAndDelimiter)
                                fieldValue = fieldValue.Replace(Configuration.QuoteChar.ToString(), Configuration.DoubleQuoteChar);
                            else
                                fieldValue = fieldValue.Replace(Configuration.QuoteChar.ToString(), "\\{0}".FormatString(Configuration.QuoteChar));

                            quoteValue = true;
                        }

                        if (!ignoreCheckDelimiter && fieldValue.IndexOf(Configuration.Delimiter) >= 0)
                        {
                            if (isHeader)
                            {
                                if (fieldConfig == null || fieldConfig.ValueSelector == null)
                                    throw new ChoParserException("Field header '{0}' value contains delimiter character.".FormatString(fieldName));
                            }
                            else
                            {
                                //Fields with embedded commas must be delimited with double-quote characters.
                                if (Configuration.EscapeQuoteAndDelimiter)
                                    fieldValue = fieldValue.Replace(Configuration.Delimiter, "\\{0}".FormatString(Configuration.Delimiter));

                                quoteValue = true;
                                //throw new ChoParserException("Field '{0}' value contains delimiter character.".FormatString(fieldName));
                            }
                        }

                        if (fieldValue.IndexOf(Configuration.EOLDelimiter) >= 0)
                        {
                            if (isHeader)
                                throw new ChoParserException("Field header '{0}' value contains EOL delimiter character.".FormatString(fieldName));
                            else
                            {
                                //A field that contains embedded line-breaks must be surounded by double-quotes
                                //if (Configuration.EscapeQuoteAndDelimiters)
                                //    fieldValue = fieldValue.Replace(Configuration.EOLDelimiter, "\\{0}".FormatString(Configuration.EOLDelimiter));

                                quoteValue = true;
                                //throw new ChoParserException("Field '{0}' value contains EOL delimiter character.".FormatString(fieldName));
                            }
                        }
                    }

                    if (!isHeader)
                    {
                        //Fields with leading or trailing spaces must be delimited with double-quote characters.
                        if (!fieldValue.IsNullOrWhiteSpace() && (char.IsWhiteSpace(fieldValue[0]) || char.IsWhiteSpace(fieldValue[fieldValue.Length - 1])))
                        {
                            quoteValue = true;
                        }
                    }
                }
                else
                    quoteValue = quoteField.Value;
            }
            //}
            //else
            //{
            //    if (fieldValue.StartsWith(Configuration.QuoteChar.ToString()) && fieldValue.EndsWith(Configuration.QuoteChar.ToString()))
            //    {

            //    }
            //    else
            //    {
            //        //Fields that contain double quote characters must be surrounded by double-quotes, and the embedded double-quotes must each be represented by a pair of consecutive double quotes.
            //        if (fieldValue.IndexOf(Configuration.QuoteChar) >= 0)
            //        {
            //            fieldValue = "{1}{0}{1}".FormatString(fieldValue.Replace(Configuration.QuoteChar.ToString(), Configuration.DoubleQuoteChar), Configuration.QuoteChar);
            //        }
            //        else
            //            fieldValue = "{1}{0}{1}".FormatString(fieldValue, Configuration.QuoteChar);
            //    }
            //}

            //if (quoteValue)
            //	size = size - 2;

            if (fieldValue.IsNullOrEmpty())
            {
                if (nullValue != null)
                    fieldValue = nullValue;
            }

            if (size != null)
            {
                if (quoteValue)
                    size = size.Value - 2;

                if (size <= 0)
                    return String.Empty;

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

            //quotes are quoted and doubled (excel) i.e. 15" -> field1,"15""",field3
            if (fieldValue.Contains(Configuration.QuoteChar))
            {
                fieldValue = fieldValue.Replace(Configuration.QuoteChar.ToString(), Configuration.DoubleQuoteChar);

                if (!isHeader)
                {
                    if (fieldConfig != null && fieldConfig.ExcelField)
                    {
                        if (Configuration.QuoteChar == '"')
                            fieldValue = $"={fieldValue}";
                        else
                            fieldValue = $"=\"{fieldValue}\"";
                    }
                }
            }
            else
            {
                if (!isHeader)
                {
                    if (fieldConfig != null && fieldConfig.ExcelField)
                        fieldValue = $"=\"{fieldValue}\"";
                }
            }
            if (fieldConfig != null && fieldConfig.ValueSelector != null)
                quoteValue = false;

            if (quoteValue)
                fieldValue = "{1}{0}{1}".FormatString(fieldValue, Configuration.QuoteChar);

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

        private bool RaiseFileHeaderWrite(ref string headerText)
        {
            string ht = headerText;
            bool retValue = true;
            if (Writer != null && Writer.HasFileHeaderWriteSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseFileHeaderWrite(ref ht), false);
                if (retValue)
                    headerText = ht;
            }
            else if (_callbackFileHeaderWrite != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackFileHeaderWrite.FileHeaderWrite(ref ht), false);
                if (retValue)
                    headerText = ht;
            }
            return retValue;
        }

        private void RaiseFileHeaderArrange(ref List<string> fields)
        {
            var fs = fields;

            if (Writer != null && Writer.HasFileHeaderArrangeSubscribed)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseFileHeaderArrange(ref fs));
            }
            else if (_callbackFileHeaderArrange != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackFileHeaderArrange.FileHeaderArrange(fs));
            }

            if (fs != null)
                fields = fs;
        }
    }
}
