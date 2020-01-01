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

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordWriter(Type recordType, ChoCSVRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackFileHeaderWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileHeaderWrite>(recordType);
            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);

            _recBuffer = new Lazy<List<object>>(() =>
            {
                if (Writer != null)
                {
                    var b = Writer.Context.RecBuffer;
                    if (b == null)
                        Writer.Context.RecBuffer = new List<object>();

                    return Writer.Context.RecBuffer;
                }
                else
                    return new List<object>();
            });

            //Configuration.Validate();
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
            object x = Writer != null ? Writer.Context.RecBuffer : null;
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

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            TextWriter sw = writer as TextWriter;
            ChoGuard.ArgumentNotNull(sw, "TextWriter");

            if (records == null) yield break;

            if (!RaiseBeginWrite(sw))
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
                    if (Configuration.MaxScanRows > 0)
                    {
                        List<string> fns = new List<string>();
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
                                    fns = fns.Union(GetFields(record1)).ToList();

                                    if (recCount == Configuration.MaxScanRows)
                                    {
                                        Configuration.Validate(fns.ToArray());
                                        WriteHeaderLine(sw);
                                        _configCheckDone = true;
                                        break;
                                    }
                                }
                            }
                        }
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
                                    string[] fieldNames = GetFields(notNullRecord);
                                    Configuration.Validate(fieldNames);
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
                                sw.Write("{1}{0}", recText, _hadHeaderWritten || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                                continue;
                            }

                            try
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.CSVRecordFieldConfigurations);

                                if (ToText(_index, record, out recText))
                                {
                                    if (_index == 1)
                                        sw.Write("{1}{0}", recText, _hadHeaderWritten || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                                    else
                                        sw.Write("{1}{0}", recText, Configuration.EOLDelimiter);

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

            RaiseEndWrite(sw);
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
                var dictKeys = new List<string>();
                var dict = record.ToDynamicObject() as IDictionary<string, Object>;
                if (Configuration.UseNestedKeyFormat)
                    fieldNames = dict.Flatten(Configuration.NestedColumnSeparator).ToDictionary().Keys.ToArray();
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
            foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
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
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                if (!isInit)
                {
                    isInit = true;
                    if (Configuration.IsDynamicObject)
                        dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                    if (Configuration.IsDynamicObject && Configuration.UseNestedKeyFormat)
                        dict = dict.Flatten(Configuration.NestedColumnSeparator).ToArray().ToDictionary();
                }

                if (Configuration.ThrowAndStopOnMissingField)
                {
                    if (Configuration.IsDynamicObject)
                    {
                        if (!dict.ContainsKey(kvp.Key))
                        {
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
                        fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] :
                            fieldConfig.FieldPosition > 0 && fieldConfig.FieldPosition - 1 < dict.Keys.Count 
                            && Configuration.RecordFieldConfigurationsDict.Count == dict.Keys.Count ? dict[dict.Keys.ElementAt(fieldConfig.FieldPosition - 1)] : null; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
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
                    if (fieldValue == null)
                    {
                        if (fieldConfig.IsDefaultValueSpecified)
                            fieldValue = fieldConfig.DefaultValue;
                    }

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
                                sb.Append(NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, null, ChoFieldValueJustification.None, GetFillChar(kvp.Value.FillChar)));
                                first = false;
                            }
                            else
                                sb.AppendFormat("{0}{1}", Configuration.Delimiter, NormalizeFieldValue(kvp.Key, item.ToNString(), null, false, null, ChoFieldValueJustification.None, GetFillChar(kvp.Value.FillChar)));
                        }
                        fieldText = sb.ToString();
                    }
                    else
                        fieldText = fieldValue.ToString();
                }

                if (firstColumn)
                {
                    msg.Append(NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, kvp.Value.QuoteField, 
                        GetFieldValueJustification(kvp.Value.FieldValueJustification), GetFillChar(kvp.Value.FillChar), 
                        false, kvp.Value.NullValue, kvp.Value.GetFieldValueTrimOption(kvp.Value.FieldType, Configuration.FieldValueTrimOption), fieldConfig));
                    firstColumn = false;
                }
                else
                    msg.AppendFormat("{0}{1}", Configuration.Delimiter, NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, kvp.Value.QuoteField, GetFieldValueJustification(kvp.Value.FieldValueJustification),
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
                var item =  ChoType.GetPropertyValue(target, propertyInfo);
                if (item != null && typeof(IList).IsAssignableFrom(item.GetType()))
                {
                    if (fieldConfig.ArrayIndex != null)
                    {
                        return ((IList)item).OfType<object>().Skip(fieldConfig.ArrayIndex.Value).FirstOrDefault();
                    }
                    return null;
                }
                else if (item != null && item.GetType().IsGenericType && item.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>)
                    && typeof(string) == item.GetType().GetGenericArguments()[0])
                {
                    if (fieldConfig.DictKey != null && ((IDictionary)item).Contains(fieldConfig.DictKey))
                    {
                        return ((IDictionary)item)[fieldConfig.DictKey];
                    }
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
            if (HasExcelSeparator)
                sw.Write("sep={0}".FormatString(Configuration.Delimiter));

            if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
            {
                string header = ToHeaderText();
                if (RaiseFileHeaderWrite(ref header))
                {
                    if (header.IsNullOrWhiteSpace())
                        return;

                    sw.Write("{1}{0}", header, HasExcelSeparator ? Configuration.EOLDelimiter : "");
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
            string delimiter = Configuration.Delimiter;
            StringBuilder msg = new StringBuilder();
            string value;
            foreach (var member in Configuration.CSVRecordFieldConfigurations)
            {
                if (Configuration.IgnoredFields.Contains(member.Name))
                    continue;

                if (member.HeaderSelector == null)
                {
                    value = NormalizeFieldValue(member.Name, member.FieldName, null,
                    Configuration.FileHeaderConfiguration.Truncate == null ? true : Configuration.FileHeaderConfiguration.Truncate.Value,
                        Configuration.FileHeaderConfiguration.QuoteAllHeaders,
                        Configuration.FileHeaderConfiguration.Justification == null ? ChoFieldValueJustification.None : Configuration.FileHeaderConfiguration.Justification.Value,
                        Configuration.FileHeaderConfiguration.FillChar == null ? ' ' : Configuration.FileHeaderConfiguration.FillChar.Value,
                        true, null, null, member);
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
            ChoFieldValueTrimOption? fieldValueTrimOption = null, ChoCSVRecordFieldConfiguration fieldConfig = null)
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

                        if (fieldValue.IndexOf(Configuration.Delimiter) >= 0)
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
                fieldValue = fieldValue.Replace(Configuration.QuoteChar.ToString(), Configuration.DoubleQuoteChar);

            if (fieldConfig != null && fieldConfig.ValueSelector != null)
                quoteValue = false;

            if (quoteValue)
                fieldValue = "{1}{0}{1}".FormatString(fieldValue, Configuration.QuoteChar);

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
        private bool RaiseFileHeaderWrite(ref string headerText)
        {
            string ht = headerText;
            bool retValue = true;
            if (_callbackFileHeaderWrite != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackFileHeaderWrite.FileHeaderWrite(ref ht), false);
            }
            else if (Writer != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseFileHeaderWrite(ref ht), false);
            }
            headerText = ht;
            return !retValue;
        }
    }
}
