using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoFixedLengthRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoCustomColumnMappable _customColumnMappableRecord;
        private IChoEmptyLineReportable _emptyLineReportableRecord;
        private bool _headerFound = false;
        private string[] _fieldNames = new string[] { };
        private bool _configCheckDone = false;
        internal ChoReader Reader = null;

        public ChoFixedLengthRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoFixedLengthRecordReader(Type recordType, ChoFixedLengthRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _customColumnMappableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoCustomColumnMappable>(recordType);
            _emptyLineReportableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoEmptyLineReportable>(recordType);
            //Configuration.Validate();
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            if (source == null)
                return Enumerable.Empty<object>();

            InitializeRecordConfiguration(Configuration);
            return AsEnumerable(source, TraceSwitch, filterFunc);
        }

        private IEnumerable<object> AsEnumerable(object source, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            TextReader sr = source as TextReader;
            if (!(source is IEnumerable<string>))
                ChoGuard.ArgumentNotNull(sr, "TextReader");

            if (sr is StreamReader)
                ((StreamReader)sr).Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(source))
                yield break;

            string[] commentTokens = Configuration.Comments;
            bool? skip = false;
            bool abortRequested = false;
            long runningCount = 0;
            long recCount = 0;
            bool headerLineLoaded = false;
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;
            bool? skipUntil = true;
            bool? doWhile = true;

            using (ChoPeekEnumerator<Tuple<long, string>> e = new ChoPeekEnumerator<Tuple<long, string>>(
                new ChoIndexedEnumerator<string>(source is IEnumerable<string> ? (IEnumerable<string>)source : 
                sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, false /*Configuration.MayContainEOLInData*/, Configuration.MaxLineSize)).ToEnumerable(),
                (pair) =>
                {
                    //bool isStateAvail = IsStateAvail();
                    skip = false;

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
                    //if (isStateAvail)
                    //{
                    //    if (!IsStateMatches(item))
                    //    {
                    //        skip = filterFunc != null ? filterFunc(item) : false;
                    //    }
                    //    else
                    //        skip = true;
                    //}
                    //else
                    //    skip = filterFunc != null ? filterFunc(item) : false;

                    if (skip == null)
                        return null;


                    if (TraceSwitch.TraceVerbose)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, Environment.NewLine);

                        if (!skip.Value)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading line [{0}]...".FormatString(pair.Item1));
                        else
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping line [{0}]...".FormatString(pair.Item1));
                    }

                    if (skip.Value)
                        return new Tuple<bool?, Tuple<long, string>>(skip, pair);

                    //if (!(sr.BaseStream is MemoryStream))
                    //    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, ChoETLFramework.Switch.TraceVerbose, "Loading line [{0}]...".FormatString(item.Item1));

                    //if (Task != null)
                    //    return !IsStateNOTExistsOrNOTMatch(item);

                    if (pair.Item2.IsNullOrWhiteSpace())
                    {
                        if (RaiseReportEmptyLine(this, pair.Item1))
                        {
                            if (TraceSwitch.TraceVerbose)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring empty line found at [{0}].".FormatString(pair.Item1));
                            return new Tuple<bool?, Tuple<long, string>>(true, pair);
                        }
                        else
                        {
                            if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
                            {
                                if (_headerFound)
                                    return new Tuple<bool?, Tuple<long, string>>(false, pair);
                                else
                                    return new Tuple<bool?, Tuple<long, string>>(true, pair);
                            }
                            else
                                return new Tuple<bool?, Tuple<long, string>>(false, pair);
                        }

                        //if (!Configuration.IgnoreEmptyLine)
                        //    throw new ChoParserException("Empty line found at [{0}] location.".FormatString(pair.Item1));
                        //else
                        //{
                        //    if (TraceSwitch.TraceVerbose)
                        //        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring empty line found at [{0}].".FormatString(pair.Item1));
                        //    return new Tuple<bool?, Tuple<long, string>>(true, pair);
                        //}
                    }

                    if (commentTokens != null && commentTokens.Length > 0)
                    {
                        foreach (string comment in commentTokens)
                        {
                            if (!pair.Item2.IsNull() && pair.Item2.StartsWith(comment, StringComparison.Ordinal)) //, true, Configuration.Culture))
                            {
                                if (TraceSwitch.TraceVerbose)
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Comment line found at [{0}]...".FormatString(pair.Item1));
                                return new Tuple<bool?, Tuple<long, string>>(true, pair);
                            }
                        }
                    }

                    if (Configuration.FileHeaderConfiguration.HeaderLineAt > 0)
                    {
                        if (pair.Item1 < Configuration.FileHeaderConfiguration.HeaderLineAt)
                        {
                            if (TraceSwitch.TraceVerbose)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Header line at {1}. Skipping [{0}] line...".FormatString(pair.Item1, Configuration.FileHeaderConfiguration.HeaderLineAt));
                            return new Tuple<bool?, Tuple<long, string>>(true, pair);
                        }
                    }

                    if (Reader is IChoSanitizableReader)
                    {
                        pair = new Tuple<long, string>(pair.Item1, ((IChoSanitizableReader)Reader).RaiseSanitizeLine(pair.Item1, pair.Item2));
                    }

                    //LoadHeader if any
                    if ((Configuration.FileHeaderConfiguration.HasHeaderRecord
                        || Configuration.FileHeaderConfiguration.HeaderLineAt > 0)
                        && !_headerFound)
                    {
                        if (!_configCheckDone)
                        {
                            if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null && !Configuration.RecordTypeMapped)
                            {
                            }
                            else
                            {
                                object headers = GetHeaders(pair.Item2);
                                Configuration.Validate(headers == null ? pair : headers);
                            }
                            var dict = recFieldTypes = Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                            if (Configuration.MaxScanRows == 0)
                                RaiseMembersDiscovered(dict);
                            Configuration.UpdateFieldTypesIfAny(dict);
                            _configCheckDone = true;
                        }

                        if (Configuration.FileHeaderConfiguration.IgnoreHeader)
                        {
                            if (TraceSwitch.TraceVerbose)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring header line at [{0}]...".FormatString(pair.Item1));
                        }
                        else
                        {
                            if (TraceSwitch.TraceVerbose)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading header line at [{0}]...".FormatString(pair.Item1));

                            headerLineLoaded = true;
                        }
                        _headerFound = true;
                        LoadHeaderLine(pair);
                        return new Tuple<bool?, Tuple<long, string>>(true, pair);
                    }
                    else
                    {
                        if (!_configCheckDone)
                        {
                            if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null && !Configuration.RecordTypeMapped)
                            {
                            }
                            else
                            {
                                object headers = GetHeaders(pair.Item2);
                                Configuration.Validate(headers == null ? pair : headers);
                            }
                            var dict = recFieldTypes = Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                            if (Configuration.MaxScanRows == 0)
                                RaiseMembersDiscovered(dict);
                            Configuration.UpdateFieldTypesIfAny(dict);
                            _configCheckDone = true;
                        }
                    }
                    return new Tuple<bool?, Tuple<long, string>>(false, pair);
                }))
            {
                while (true)
                {
                    recCount++;
                    Tuple<long, string> pair = e.Peek;
                    if (pair == null)
                    {
                        if (!abortRequested)
                            RaisedRowsLoaded(runningCount);

                        RaiseEndLoad(source);
                        yield break;
                    }
                    runningCount = pair.Item1;

                    object rec = null;
                    if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null)
                    {
                        Type recType = Configuration.RecordSelector(pair);
                        if (recType == null)
                        {
                            if (Configuration.IgnoreIfNoRecordTypeFound)
                            {
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, $"No record type found for [{pair.Item1}] line to parse.");
                                e.MoveNext();
                                continue;
                            }
                            else
                                throw new ChoParserException($"No record type found for [{pair.Item1}] line to parse.");
                        }

                        if (!Configuration.RecordTypeMapped)
                        {
                            Configuration.MapRecordFields(recType);
                            Configuration.Validate(null);
                        }

                        rec = recType.IsDynamicType() ? new ChoDynamicObject(new Dictionary<string, object>(Configuration.FileHeaderConfiguration.StringComparer))
                        {
                            ThrowExceptionIfPropNotExists = true,
                            AlternativeKeys = Configuration.AlternativeKeys
                        } : ChoActivator.CreateInstance(recType);
                    }
                    else
                    {
                        rec = Configuration.IsDynamicObject ? new ChoDynamicObject(new Dictionary<string, object>(Configuration.FileHeaderConfiguration.StringComparer))
                        {
                            ThrowExceptionIfPropNotExists = true,
                            AlternativeKeys = Configuration.AlternativeKeys
                        } : ChoActivator.CreateInstance(RecordType);

                    }

                    if (!LoadLine(pair, ref rec))
                        yield break;

                    //StoreState(e.Current, rec != null);

                    e.MoveNext();

                    if (rec == null)
                        continue;

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        if (Configuration.AreAllFieldTypesNull && Configuration.AutoDiscoverFieldTypes && Configuration.MaxScanRows > 0 && recCount <= Configuration.MaxScanRows)
                        {
                            buffer.Add(rec);
                            if (recFieldTypes == null)
                                recFieldTypes = Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                            RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, recCount == Configuration.MaxScanRows);
                            if (recCount == Configuration.MaxScanRows || e.Peek == null)
                            {
                                Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                                var dict = recFieldTypes = Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                                RaiseMembersDiscovered(dict);

                                foreach (object rec1 in buffer)
                                    yield return ConvertToNestedObjectIfApplicable(new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes)) as object, headerLineLoaded);
                            }
                        }
                        else
                        {
                            yield return ConvertToNestedObjectIfApplicable(rec, headerLineLoaded);
                        }
                    }
                    else
                        yield return rec;

                    if (Configuration.NotifyAfter > 0 && pair.Item1 % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsLoaded(pair.Item1))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
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
            }
        }

        private object ConvertToNestedObjectIfApplicable(object rec, bool headerLineFound)
        {
            if (!headerLineFound || !Configuration.IsDynamicObject || Configuration.NestedColumnSeparator == null)
                return rec;

            IDictionary<string, object> dict = rec as IDictionary<string, object>;
            dynamic dict1 = new ChoDynamicObject(dict.ToDictionary(kvp => Configuration.RecordFieldConfigurationsDict[kvp.Key].FieldName, kvp => kvp.Value));

            return dict1.ConvertToNestedObject(Configuration.NestedColumnSeparator == null ? '/' : Configuration.NestedColumnSeparator.Value, Configuration.ArrayIndexSeparator, null,
                 Configuration.AllowNestedArrayConversion);
        }

        private bool LoadLine(Tuple<long, string> pair, ref object rec)
        {
            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping...");
                    rec = null;
                    return true;
                }

                if (Configuration.CustomTextSelecter != null)
                {
                    pair = new Tuple<long, string>(pair.Item1, Configuration.CustomTextSelecter(pair.Item2));
                }

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }
                else if (pair.Item2 == String.Empty)
                    return true;

                if (!pair.Item2.IsNullOrWhiteSpace())
                {
                    if (!FillRecord(rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.FixedLengthRecordFieldConfigurations);
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

        private bool FillRecord(object rec, Tuple<long, string> pair)
        {
            long lineNo;
            string line = null;

            lineNo = pair.Item1;
            line = pair.Item2;

            if (line.Length != Configuration.RecordLength)
                throw new ChoParserException("Incorrect record length [Length: {0}] found. Expected record length: {1}".FormatString(line.Length, Configuration.RecordLength));

            object fieldValue = null;
            ChoFixedLengthRecordFieldConfiguration fieldConfig = null;
            PropertyInfo pi = null;
            object rootRec = rec;
            foreach (KeyValuePair<string, ChoFixedLengthRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                fieldValue = null;
                fieldConfig = kvp.Value;
                
                //ChoCSVRecordReader can't find property by FieldName #118
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
                try
                {
                    if (fieldConfig.ValueSelector == null)
                    {
                        if (fieldConfig.StartIndex + fieldConfig.Size > line.Length)
                        {
                            if (Configuration.ColumnCountStrict)
                                throw new ChoParserException("Missing '{0}' field value.".FormatString(kvp.Key));
                        }
                        else
                            fieldValue = line.Substring(fieldConfig.StartIndex, fieldConfig.Size.Value);
                    }
                    else
                    {
                        fieldValue = fieldConfig.ValueSelector(new ChoDynamicObject(new Dictionary<string, object>() { { "Line", line } }));
                    }

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        //if (kvp.Value.FieldType == null)
                        //    kvp.Value.FieldType = typeof(string);
                    }
                    else
                    {
                        if (pi != null)
                            kvp.Value.FieldType = pi.PropertyType;
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    fieldValue = fieldValue is string ? CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string) : fieldValue;

                    if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                        continue;

                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
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
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else if (!Configuration.SupportsMultiRecordTypes)
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
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
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
                                ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring field...".FormatString(ex.Message));
                                continue;
                            }
                            else
                            {
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, ref fieldValue, ex))
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

                                            dict.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture);
                                        }
                                        else
                                        {
                                            if (pi != null)
                                                rec.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture);
                                            else
                                                throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(kvp.Key, ChoType.GetTypeName(rec)));
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

            return true;
        }

        private string CleanFieldValue(ChoFixedLengthRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForRead(fieldType, Configuration.FieldValueTrimOption);

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

            char startChar;
            char endChar;
            char quoteChar = Configuration.QuoteChar == '\0' ? '"' : Configuration.QuoteChar;

            if (fieldValue.Length >= 2)
            {
                startChar = fieldValue[0];
                endChar = fieldValue[fieldValue.Length - 1];

                if (config.QuoteField != null && config.QuoteField.Value && startChar == quoteChar && endChar == quoteChar)
                    return fieldValue.Substring(1, fieldValue.Length - 2);
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

            if (config.NullValue != null)
            {
                if (String.Compare(config.NullValue, fieldValue, true) == 0)
                    fieldValue = null;
            }

            return fieldValue;
        }

        private string[] GetHeaders(string line, bool force = false)
        {
            string[] headers = null;
            if (force || (Configuration.FileHeaderConfiguration.HasHeaderRecord && !Configuration.FileHeaderConfiguration.IgnoreHeader))
            {
                //Fields are specified, load them
                if (Configuration.RecordFieldConfigurationsDict != null && Configuration.RecordFieldConfigurationsDict.Count > 0)
                {
                    List<string> headersList = new List<string>();
                    string fieldValue = null;
                    ChoFixedLengthRecordFieldConfiguration fieldConfig = null;
                    foreach (KeyValuePair<string, ChoFixedLengthRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
                    {
                        fieldValue = null;
                        fieldConfig = kvp.Value;

                        if (fieldConfig.StartIndex + fieldConfig.Size > line.Length)
                        {
                            if (Configuration.ColumnCountStrict)
                                throw new ChoParserException("Missing '{0}' field.".FormatString(kvp.Key));
                        }
                        else
                            fieldValue = line.Substring(fieldConfig.StartIndex, fieldConfig.Size.Value);

                        fieldValue = CleanFieldValue(fieldConfig, typeof(object), fieldValue as string);
                        headersList.Add(fieldValue);
                    }

                    headers = headersList.ToArray();

                    List<string> newHeaders = new List<string>();
                    int index = 1;
                    string newColName = null;
                    foreach (string header in headers)
                    {
                        if (RaiseMapColumn(this, index, header, out newColName))
                            newHeaders.Add(newColName);
                        else
                            newHeaders.Add(header);

                        index++;
                    }
                    headers = newHeaders.ToArray();

                    //Check for any empty column headers
                    if (headers.Where(h => h.IsNullOrEmpty()).Any())
                    {
                        if (!Configuration.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader)
                        {
                            var c = headers.Select((t, i) => String.IsNullOrWhiteSpace(t) ? (int?)i + 1 : null).Where(t => t != null).ToArray();
                            throw new ChoParserException("Atleast one of the field header is empty. Please check the field headers at [{0}].".FormatString(String.Join(",", c)));
                        }
                        else
                        {
                            index = 0;
                            newHeaders = new List<string>();
                            foreach (string header in headers)
                            {
                                if (header.IsNullOrWhiteSpace())
                                    newHeaders.Add("_Column{0}".FormatString(++index));
                                else
                                    newHeaders.Add(header);
                            }
                            headers = newHeaders.ToArray();
                        }
                    }

                    Configuration.Context.Headers = headers;
                }
                else
                {
                    if (Configuration.RecordLength != 0 && line.Length != Configuration.RecordLength)
                        throw new ChoParserException("Incorrect header length [Length: {0}] found. Expected header length: {1}".FormatString(line.Length, Configuration.RecordLength));
                }
            }
            else
            {
            }

            return headers;
        }

        private void LoadHeaderLine(Tuple<long, string> pair)
        {
            string line = pair.Item2;

            //Validate header
            _fieldNames = GetHeaders(line, true);
            
            if (_fieldNames == null || _fieldNames.Length == 0)
                throw new ChoParserException("No headers found.");

            //Check any header value empty
            if (_fieldNames.Where(i => i.IsNullOrWhiteSpace()).Any())
            {
                var c = _fieldNames.Select((t, i) => String.IsNullOrWhiteSpace(t) ? (int?)i + 1 : null).Where(t => t != null).ToArray();
                throw new ChoParserException("Atleast one of the field header is empty. Please check the field headers at [{0}].".FormatString(String.Join(",", c)));
            }

            if (Configuration.ColumnCountStrict)
            {
                if (_fieldNames.Length != Configuration.FixedLengthRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of field headers found. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.FixedLengthRecordFieldConfigurations.Count, _fieldNames.Length));

                string[] foundList = Configuration.FixedLengthRecordFieldConfigurations.Select(i => i.FieldName).Except(_fieldNames, Configuration.FileHeaderConfiguration.StringComparer).ToArray();
                if (foundList.Any())
                    throw new ChoParserException("Header name(s) [{0}] are not found in file header.".FormatString(String.Join(",", foundList)));
            }

            if (Configuration.ColumnOrderStrict)
            {
                //Not applicable in FixedLength file
            }
        }

        private string CleanHeaderValue(string headerValue)
        {
            if (headerValue.IsNull()) return headerValue;

            ChoFileHeaderConfiguration config = Configuration.FileHeaderConfiguration;
            if (headerValue != null)
            {
                switch (config.TrimOption)
                {
                    case ChoFieldValueTrimOption.Trim:
                        headerValue = headerValue.Trim();
                        break;
                    case ChoFieldValueTrimOption.TrimStart:
                        headerValue = headerValue.TrimStart();
                        break;
                    case ChoFieldValueTrimOption.TrimEnd:
                        headerValue = headerValue.TrimEnd();
                        break;
                }
            }

            if (Configuration.QuoteAllFields != null && Configuration.QuoteAllFields.Value && headerValue.StartsWith(@"""") && headerValue.EndsWith(@""""))
                return headerValue.Substring(1, headerValue.Length - 2);
            else
                return headerValue;
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

        private bool? RaiseSkipUntil(Tuple<long, string> pair)
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

        private bool? RaiseDoWhile(Tuple<long, string> pair)
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

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, string> pair)
        {
            if (Reader != null && Reader.HasBeforeRecordLoadSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, string>(index, state as string);

                return retValue;
            }
            else if (_callbackRecordRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, string>(index, state as string);

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, string> pair, ref bool skip)
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

        private bool RaiseRecordLoadError(object target, Tuple<long, string> pair, Exception ex)
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

        private bool RaiseMapColumn(object target, int colPos, string colName, out string newColName)
        {
            newColName = null;
            if (Reader != null && Reader.HasMapColumnSubscribed)
            {
                string lnewColName = null;
                bool retVal = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseMapColumn(colPos, colName, out lnewColName), false);
                if (retVal)
                    newColName = lnewColName;

                return retVal;
            }
            else if (target is IChoCustomColumnMappable)
            {
                bool retVal = false;
                string lnewColName = null;
                retVal = ChoFuncEx.RunWithIgnoreError(() => ((IChoCustomColumnMappable)target).MapColumn(colPos, colName, out lnewColName), false);
                if (retVal)
                    newColName = lnewColName;
                return retVal;
            }
            else if (_customColumnMappableRecord != null)
            {
                bool retVal = false;
                string lnewColName = null;
                retVal = ChoFuncEx.RunWithIgnoreError(() => _customColumnMappableRecord.MapColumn(colPos, colName, out lnewColName), false);
                if (retVal)
                    newColName = lnewColName;
                return retVal;
            }
            return false;
        }

        private bool RaiseReportEmptyLine(object target, long index)
        {
            if (Reader != null && Reader.HasReportEmptyLineSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseReportEmptyLine(index), false);
            }
            else if (target is IChoEmptyLineReportable)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoEmptyLineReportable)target).EmptyLineFound(index), false);
            }
            else if (_emptyLineReportableRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _emptyLineReportableRecord.EmptyLineFound(index), false);
            }
            return true;
        }

        #endregion Event Raisers
    }
}
