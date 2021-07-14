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

namespace ChoETL
{
    internal class ChoFixedLengthRecordWriter : ChoRecordWriter
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

        public ChoFixedLengthRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoFixedLengthRecordWriter(Type recordType, ChoFixedLengthRecordConfiguration configuration) : base(recordType)
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

        public void WriteHeader(object writer, string header)
        {
            _sw = writer;
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

                if (Configuration.MaxScanRows > 0 && !_rowScanComplete)
                {
                    if (Configuration.MaxScanRows > 0)
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
                            //Discover and load FixedLength columns from first record
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
                                //sw.Write("{1}{0}", recText, _hadHeaderWritten ? Configuration.EOLDelimiter : "");
                                Write(sw, recText);

                                continue;
                            }

                            try
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.FixedLengthRecordFieldConfigurations);

                                if (ToText(_index, record, out recText))
                                {
                                    //if (_index == 1)
                                    //{
                                    //    sw.Write("{1}{0}", recText, _hadHeaderWritten ? Configuration.EOLDelimiter : "");
                                    //}
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
                if (Configuration.FixedLengthRecordFieldConfigurations.Count == 0)
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
                if (Configuration.FixedLengthRecordFieldConfigurations.Count == 0)
                    Configuration.MapRecordFields(Configuration.RecordType);
            }

            if (Configuration.IsDynamicObject)
            {
                //var dictKeys = new List<string>();
                //var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                //if (Configuration.UseNestedKeyFormat)
                //    fieldNames = dict.Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToDictionary().Keys.ToArray();
                //else
                //    fieldNames = dict.Keys.ToArray();

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
        ChoFixedLengthRecordFieldConfiguration fieldConfig = null;
        IDictionary<string, Object> dict = null;
        private bool ToText(long index, object rec, out string recText)
        {
            if (typeof(IChoScalarObject).IsAssignableFrom(Configuration.RecordType))
                rec = ChoActivator.CreateInstance(Configuration.RecordType, rec);

            recText = null;
            msg.Clear();

            if (Configuration.ColumnCountStrict)
                CheckColumnsStrict(rec);

            bool isInit = false;
            //bool firstColumn = true;
            PropertyInfo pi = null;
            object rootRec = rec;
            foreach (KeyValuePair<string, ChoFixedLengthRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
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

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

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
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' FixedLength column.".FormatString(fieldConfig.FieldName));
                    }
                    else
                    {
                        if (pi == null)
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' FixedLength column.".FormatString(fieldConfig.FieldName));
                    }
                }

                try
                {
                    if (Configuration.IsDynamicObject)
                    {
                        fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] : null; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
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

                if (fieldValue == null)
                    fieldText = String.Empty;
                else
                {
                    if (fieldValue is IList)
                    {
                        StringBuilder sb = new StringBuilder();
                        bool first = true;
                        foreach (var item in (IList)fieldValue)
                        {
                            if (first)
                            {
                                sb.Append(NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, null, ChoFieldValueJustification.None, ChoCharEx.NUL));
                                first = false;
                            }
                            else
                                sb.Append(NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, null, ChoFieldValueJustification.None, ChoCharEx.NUL));
                        }
                        fieldText = sb.ToString();
                    }
                    else
                        fieldText = fieldValue.ToString();
                }

                msg.Append(NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, kvp.Value.QuoteField, 
                    GetFieldValueJustification(kvp.Value.FieldValueJustification, kvp.Value.FieldType), 
                    GetFillChar(kvp.Value.FillChar, kvp.Value.FieldType), false, kvp.Value.NullValue,
                    kvp.Value.GetFieldValueTrimOption(kvp.Value.FieldType, Configuration.FieldValueTrimOption)));
            }

            recText = msg.ToString();
            return true;
        }

        private ChoFieldValueJustification GetFieldValueJustification(ChoFieldValueJustification? fieldValueJustification, Type fieldType)
        {
            if (fieldValueJustification != null)
                return fieldValueJustification.Value;

            if (fieldType == typeof(int)
              || fieldType == typeof(uint)
              || fieldType == typeof(long)
              || fieldType == typeof(ulong)
              || fieldType == typeof(short)
              || fieldType == typeof(ushort)
              || fieldType == typeof(byte)
              || fieldType == typeof(sbyte)
              || fieldType == typeof(float)
              || fieldType == typeof(double)
              || fieldType == typeof(decimal)
              || fieldType == typeof(Single)
              )
            {
                return ChoFieldValueJustification.Right;
            }
            else
                return ChoFieldValueJustification.Left;
        }

        private char GetFillChar(char? fillChar, Type fieldType)
        {
            if (fillChar != null)
                return fillChar.Value;

            if (fieldType == typeof(int)
                || fieldType == typeof(uint)
                || fieldType == typeof(long)
                || fieldType == typeof(ulong)
                || fieldType == typeof(short)
                || fieldType == typeof(ushort)
                || fieldType == typeof(byte)
                || fieldType == typeof(sbyte)
                || fieldType == typeof(float)
                || fieldType == typeof(double)
                || fieldType == typeof(decimal)
                || fieldType == typeof(Single)
                )
            {
                return '0';
            }
            else
                return ' ';
        }

        private void CheckColumnsStrict(object rec)
        {
            if (Configuration.IsDynamicObject)
            {
                var eoDict = rec.ToDynamicObject() as IDictionary<string, Object>;

                if (eoDict.Count != Configuration.FixedLengthRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.FixedLengthRecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.FixedLengthRecordFieldConfigurations.Select(v => v.Name).Except(eoDict.Keys, Configuration.FileHeaderConfiguration.StringComparer).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = ChoTypeDescriptor.GetProperties<ChoFixedLengthRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.FixedLengthRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.FixedLengthRecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.FixedLengthRecordFieldConfigurations.Select(v => v.Name).Except(pds.Select(pd => pd.Name), Configuration.FileHeaderConfiguration.StringComparer).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
        }

        private void WriteHeaderLine(TextWriter sw)
        {
            if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
            {
                string header = ToHeaderText();
                if (RaiseFileHeaderWrite(ref header))
                {
                    if (header.IsNullOrWhiteSpace())
                        return;

                    //sw.Write(header);
                    Write(sw, header);

                    _hadHeaderWritten = true;
                }
            }
        }

        private string ToHeaderText()
        {
            if (!_customHeader.IsNullOrWhiteSpace())
                return _customHeader;
            StringBuilder msg = new StringBuilder();
            string value;
            foreach (var member in Configuration.FixedLengthRecordFieldConfigurations)
            {
                if (Configuration.IgnoredFields.Contains(member.Name))
                    continue;

                if (member.HeaderSelector == null)
                {
                    value = NormalizeFieldValue(member.Name, member.FieldName, member.Size,
                    Configuration.FileHeaderConfiguration.Truncate == null ? true : Configuration.FileHeaderConfiguration.Truncate.Value,
                        Configuration.FileHeaderConfiguration.QuoteAllHeaders,
                        Configuration.FileHeaderConfiguration.Justification == null ? ChoFieldValueJustification.Left : Configuration.FileHeaderConfiguration.Justification.Value,
                        Configuration.FileHeaderConfiguration.FillChar == null ? ' ' : Configuration.FileHeaderConfiguration.FillChar.Value,
                        true, null, Configuration.FileHeaderConfiguration.TrimOption);
                }
                else
                    value = member.HeaderSelector();

                msg.Append(value);
            }
            return msg.ToString();
        }

        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool? quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false, string nullValue = null, 
            ChoFieldValueTrimOption? fieldValueTrimOption = null)
        {
            string lFieldValue = fieldValue;
            bool retValue = false;

            if (retValue)
                return lFieldValue;

            if (fieldValue.IsNull())
                fieldValue = String.Empty;
            bool quoteValue = quoteField != null ? quoteField.Value : false;

            if (quoteField == null || !quoteField.Value)
            {
                if (fieldValue.StartsWith(Configuration.QuoteChar.ToString()) && fieldValue.EndsWith(Configuration.QuoteChar.ToString()))
                {

                }
                else
                {
                    if (fieldValue.Contains(Configuration.EOLDelimiter))
                    {
                        if (isHeader)
                            throw new ChoParserException("Field header '{0}' value contains EOL delimiter character.".FormatString(fieldName));
                        else
                            quoteValue = true;
                    }
                }
            }
            else
            {
                if (fieldValue.StartsWith(Configuration.QuoteChar.ToString()) && fieldValue.EndsWith(Configuration.QuoteChar.ToString()))
                {

                }
                else
                {
                    quoteValue = true;
                }
            }


            if (fieldValue.IsNullOrEmpty())
            {
                if (nullValue != null)
                    fieldValue = nullValue;
            }

            if (size != null)
            {
                if (quoteValue)
                {
                    size = size.Value - 2;
                }
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
            }
            else
            {
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
            headerText = ht;
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
