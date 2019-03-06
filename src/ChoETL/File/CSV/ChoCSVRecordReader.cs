using System;
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

namespace ChoETL
{
    internal class ChoCSVRecordReader : ChoRecordReader
    {
        private IChoNotifyRecordRead _callbackRecord;
        private IChoNotifyRecordFieldRead _callbackFieldRecord;
        private IChoCustomColumnMappable _customColumnMappableRecord;
        private IChoEmptyLineReportable _emptyLineReportableRecord;
        private bool _headerFound = false;
        private bool _excelSeparatorFound = false;
        private string[] _fieldNames = null;
        private bool _configCheckDone = false;
        private Dictionary<string, object> fieldNameValues = null;
        private Dictionary<string, object> fieldNameValuesEx = null;
        internal ChoReader Reader = null;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordReader(Type recordType, ChoCSVRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;
            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackFieldRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            if (_callbackFieldRecord == null)
                _callbackFieldRecord = _callbackRecord;
            _customColumnMappableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoCustomColumnMappable>(recordType);
            _emptyLineReportableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoEmptyLineReportable>(recordType);
            //Configuration.Validate();

            _recBuffer = new Lazy<List<string>>(() =>
            {
                var b = Reader.Context.RecBuffer;
                if (b == null)
                    Reader.Context.RecBuffer = new List<string>();

                return Reader.Context.RecBuffer;
            });
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            return AsEnumerable(source, TraceSwitch, filterFunc);
        }

        private Lazy<List<string>> _recBuffer = null;
        private void CalcFieldMaxCountIfApplicable(IEnumerator<string> recEnum)
        {
            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {
                long recCount = 0;
                if (Configuration.MaxScanRows <= 0)
                    return;

                while (recEnum.MoveNext())
                {
                    _recBuffer.Value.Add(recEnum.Current);
                    recCount++;

                    string line = recEnum.Current;
                    if (!line.IsNullOrWhiteSpace())
                    {
                        string[] fieldValues = line.Split(Configuration.Delimiter, Configuration.StringSplitOptions, Configuration.QuoteChar, Configuration.QuoteEscapeChar);
                        if (Configuration.MaxFieldPosition < fieldValues.Length)
                            Configuration.MaxFieldPosition = fieldValues.Length;
                    }

                    if (Configuration.MaxScanRows == recCount)
                        break;
                }
            }
        }

        private IEnumerable<string> ReadLines(TextReader sr, string EOLDelimiter = null, char quoteChar = ChoCharEx.NUL, bool mayContainEOLInData = false, int maxLineSize = 32768)
        {
            var recEnum = sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData).GetEnumerator();
            CalcFieldMaxCountIfApplicable(recEnum);

            object x = Reader.Context.RecBuffer;
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return rec;

            foreach (var line in sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData))
                yield return line;
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
                    ReadLines(sr, Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData)).ToEnumerable(),
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
                                if (skipUntil.Value)
                                    skip = skipUntil;
                                else
                                    skip = true;
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
                            if (!Configuration.IgnoreEmptyLine)
                                throw new ChoParserException("Empty line found at [{0}] location.".FormatString(pair.Item1));
                            else
                            {
                                if (TraceSwitch.TraceVerbose)
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring empty line found at [{0}].".FormatString(pair.Item1));
                                return new Tuple<bool?, Tuple<long, string>>(true, pair);
                            }
                        }
                    }

                    //LoadExcelSeparator if any
                    if (pair.Item1 == 1
                        && !_excelSeparatorFound)
                    {
                        if (TraceSwitch.TraceVerbose)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Inspecting for excel separator at [{0}]...".FormatString(pair.Item1));

                        bool retVal = LoadExcelSeperatorIfAny(pair);
                        _excelSeparatorFound = true;

                        if (Configuration.HasExcelSeparator != null
                            && Configuration.HasExcelSeparator.Value
                            && !retVal)
                            throw new ChoParserException("Missing excel separator header line in the file.");

                        if (retVal)
                            return new Tuple<bool?, Tuple<long, string>>(true, pair);
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

                    //if (!_configCheckDone)
                    //{
                    //    if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null && !Configuration.RecordTypeMapped)
                    //    {
                    //    }
                    //    //else
                    //    //    Configuration.Validate(GetHeaders(pair.Item2));
                    //    var dict = recFieldTypes = Configuration.CSVRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                    //    RaiseMembersDiscovered(dict);
                    //    Configuration.UpdateFieldTypesIfAny(dict);
                    //    _configCheckDone = true;
                    //}

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
                                Configuration.Validate(GetHeaders(pair.Item2));
                            var dict = recFieldTypes = Configuration.CSVRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
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
                                Configuration.Validate(GetHeaders(pair.Item2));
                            var dict = recFieldTypes = Configuration.CSVRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                            RaiseMembersDiscovered(dict);
                            Configuration.UpdateFieldTypesIfAny(dict);
                            _configCheckDone = true;
                            LoadHeaderLine(pair);
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
                        //Configuration.SupportsMultiRecordTypes = true;
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
                            RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, recCount == Configuration.MaxScanRows);
                            if (recCount == Configuration.MaxScanRows || e.Peek == null)
                            {
                                Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                                var dict = recFieldTypes = Configuration.CSVRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
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
                            abortRequested = true;
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

            return dict1.ConvertToNestedObject(Configuration.NestedColumnSeparator.Value);
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
                        rec.DoObjectLevelValidation(Configuration, Configuration.CSVRecordFieldConfigurations);
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
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        rec = null;
                    }
                }
                else
                {
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
                }
                return true;
            }

            return true;
        }

        private Dictionary<string, object> InitFieldNameValuesDict()
        {
            if (_fieldNames == null)
                return null;

            Dictionary<string, object> fnv = new Dictionary<string, object>(Configuration.FileHeaderConfiguration.StringComparer);
            foreach (var name in _fieldNames)
            {
                if (fnv.ContainsKey(name))
                    throw new ChoParserException($"Duplicate '{name}' field found.");

                fnv.Add(name, null); // String.Empty);
            }
            return fnv;
        }

        private void ToFieldNameValues(Dictionary<string, object> fnv, string[] fieldValues)
        {
            if (_fieldNames == null)
                return;

            long index = 1;
            foreach (var name in _fieldNames)
            {
                if (index - 1 < fieldValues.Length)
                    fnv[name] = fieldValues[index - 1];
                //else
                //    fnv[name] = String.Empty;

                index++;
            }
        }

        private bool FillRecord(object rec, Tuple<long, string> pair)
        {
            long lineNo;
            string line;

            lineNo = pair.Item1;
            line = pair.Item2;

            string[] fieldValues = line.Split(Configuration.Delimiter, Configuration.StringSplitOptions, Configuration.QuoteChar, Configuration.QuoteEscapeChar);
            if (Configuration.ColumnCountStrict)
            {
                if (fieldValues.Length != Configuration.CSVRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of field values found at line [{2}]. Expected [{0}] field values. Found [{1}] field values.".FormatString(Configuration.CSVRecordFieldConfigurations.Count, fieldValues.Length, pair.Item1));
            }

            //if (_fieldNames != null) //Configuration.FileHeaderConfiguration.HasHeaderRecord && Configuration.ColumnOrderStrict)
            if (Configuration.FileHeaderConfiguration.HasHeaderRecord && !Configuration.FileHeaderConfiguration.IgnoreHeader)
            {
                if (this.fieldNameValues == null)
                    this.fieldNameValues = InitFieldNameValuesDict();
                ToFieldNameValues(fieldNameValues, fieldValues);
            }
            ValidateLine(pair.Item1, fieldValues);

            object fieldValue = null;
            ChoCSVRecordFieldConfiguration fieldConfig = null;
            PropertyInfo pi = null;
            object rootRec = rec;
            foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.FCArray)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                try
                {
                    if (fieldNameValues != null)
                    {
                        if (fieldConfig.ValueSelector == null)
                        {
                            if (fieldNameValues.ContainsKey(fieldConfig.FieldName))
                            {
                                fieldValue = fieldNameValues[fieldConfig.FieldName];
                            }

                            if (fieldValue == null && Configuration.ThrowAndStopOnMissingField)
                            {
                                throw new ChoMissingRecordFieldException("Missing '{0}' field in CSV file.".FormatString(fieldConfig.FieldName));

                                //if (Configuration.ColumnOrderStrict)
                                //    throw new ChoParserException("No matching '{0}' field header found.".FormatString(fieldConfig.FieldName));
                            }
                        }
                        else
                        {
                            fieldValue = fieldConfig.ValueSelector(new ChoDynamicObject(fieldNameValues));
                        }
                    }
                    else
                    {
                        if (fieldConfig.ValueSelector == null)
                        {
                            if (fieldConfig.FieldPosition - 1 < fieldValues.Length)
                                fieldValue = fieldValues[fieldConfig.FieldPosition - 1];
                            else if (Configuration.ThrowAndStopOnMissingField)
                                throw new ChoMissingRecordFieldException("Missing field value at [Position: {1}] in CSV file.".FormatString(fieldConfig.FieldName, fieldConfig.FieldPosition));
                        }
                        else
                        {
                            if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
                            {
                                if (fieldNameValuesEx == null)
                                    fieldNameValuesEx = InitFieldNameValuesDict();
                                ToFieldNameValues(fieldNameValuesEx, fieldValues);
                                fieldValue = fieldConfig.ValueSelector(new ChoDynamicObject(fieldNameValuesEx));
                            }
                            else
                                fieldValue = fieldConfig.ValueSelector(new ChoDynamicObject(fieldValues));
                        }
                    }

                    //if (Configuration.FileHeaderConfiguration.HasHeaderRecord && Configuration.ColumnOrderStrict)
                    //{
                    //    if (fieldNameValues.ContainsKey(fieldConfig.FieldName))
                    //        fieldValue = fieldNameValues[fieldConfig.FieldName];
                    //    else if (Configuration.ColumnCountStrict)
                    //        throw new ChoParserException("No matching '{0}' field header found.".FormatString(fieldConfig.FieldName));
                    //}
                    //else
                    //{
                    //    if (fieldConfig.FieldPosition - 1 < fieldValues.Length)
                    //        fieldValue = fieldValues[fieldConfig.FieldPosition - 1];
                    //    else if (Configuration.ColumnCountStrict)
                    //        throw new ChoParserException("Missing field value for '{0}' [Position: {1}] field.".FormatString(fieldConfig.FieldName, fieldConfig.FieldPosition));
                    //}

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        if (kvp.Value.FieldType == null)
                            kvp.Value.FieldType = Configuration.MaxScanRows == -1 ? DiscoverFieldType(fieldValue as string, Configuration) : typeof(string);
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

                    bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
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
                        if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
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
                                continue;
                            }
                            else
                            {
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
                                {
                                    if (ex is ValidationException)
                                        throw;

                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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

        private string CleanFieldValue(ChoCSVRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForRead(fieldType);

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

            if (fieldValue.Length >= 2)
            {
                startChar = fieldValue[0];
                endChar = fieldValue[fieldValue.Length - 1];

                if (config.QuoteField != null && config.QuoteField.Value && startChar == Configuration.QuoteChar && endChar == Configuration.QuoteChar)
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
                else if (startChar == Configuration.QuoteChar && endChar == Configuration.QuoteChar &&
                    (fieldValue.Contains(Configuration.Delimiter)
                    || fieldValue.Contains(Configuration.EOLDelimiter)))
                    fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
            }

            //quotes are quoted and doubled (excel) i.e. 15" -> field1,"15""",field3
            //if (fieldValue.Contains(Configuration.DoubleQuoteChar))
            //    fieldValue = fieldValue.Replace(Configuration.DoubleQuoteChar, Configuration.QuoteChar.ToString());
            //if (fieldValue.Contains(Configuration.BackslashQuote))
            //    fieldValue = fieldValue.Replace(Configuration.BackslashQuote, Configuration.QuoteChar.ToString());

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

        private void ValidateLine(long lineNo, string[] fieldValues)
        {
            int maxPos = Configuration.MaxFieldPosition;

            if (Configuration.ColumnCountStrict)
            {
                if (fieldValues.Length != maxPos)
                    throw new ChoReaderException("Mismatched number of fields found at {0} line. [Expected: {1}, Found: {2}].".FormatString(
                        lineNo, maxPos, fieldValues.Length));
            }

            //ChoCSVRecordFieldAttribute attr = null;
            //foreach (Tuple<MemberInfo, ChoOrderedAttribute> member in _members)
            //{
            //    if (attr.Position > fields.Length)
            //        throw new ApplicationException("Record Member '{0}' has incorrect Position specified.".FormatString(ChoType.GetMemberName(member.Item1)));
            //}
        }

        private bool LoadExcelSeperatorIfAny(Tuple<long, string> pair)
        {
            string line = pair.Item2.NTrim();
            if (!line.IsNullOrWhiteSpace() && line.StartsWith("sep=", true, Configuration.Culture))
            {
                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator specified at [{0}]...".FormatString(pair.Item1));
                string delimiter = line.Substring(4);
                if (!delimiter.IsNullOrWhiteSpace())
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator [{0}] found.".FormatString(delimiter));
                    Configuration.Delimiter = delimiter;
                }

                return true;
            }

            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator NOT found. Default separator [{0}] used.".FormatString(Configuration.Delimiter));
            return false;
        }

        private string[] GetHeaders(string line)
        {
            if (Configuration.FileHeaderConfiguration.HasHeaderRecord && !Configuration.FileHeaderConfiguration.IgnoreHeader)
            {
                string[] headers = null;
                headers = (from x in line.Split(Configuration.Delimiter, Configuration.StringSplitOptions, Configuration.QuoteChar, Configuration.QuoteEscapeChar)
                           select CleanHeaderValue(x)).ToArray();

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

                return headers;
            }
            else
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.MaxFieldPosition <= 0)
                    {
                        long index = 0;
                        return (from x in line.Split(Configuration.Delimiter, Configuration.StringSplitOptions, Configuration.QuoteChar, Configuration.QuoteEscapeChar)
                                select "Column{0}".FormatString(++index)).ToArray();
                    }
                    else
                    {
                        List<string> headers = new List<string>();
                        for (var counter = 1; counter <= Configuration.MaxFieldPosition; counter++)
                        {
                            headers.Add("Column{0}".FormatString(counter));
                        }
                        return headers.ToArray();
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private void LoadHeaderLine(Tuple<long, string> pair)
        {
            string line = pair.Item2;

            //Validate header
            _fieldNames = GetHeaders(line);
            if (_fieldNames == null)
                return;

            if (_fieldNames.Length == 0)
                throw new ChoParserException("No headers found.");

            //Check any header value empty
            if (_fieldNames.Where(i => i.IsNullOrWhiteSpace()).Any())
            {
                var c = _fieldNames.Select((t, i) => String.IsNullOrWhiteSpace(t) ? (int?)i + 1 : null).Where(t => t != null).ToArray();
                throw new ChoParserException("Atleast one of the field header is empty. Please check the field headers at [{0}].".FormatString(String.Join(",", c)));
            }

            if (Configuration.ColumnCountStrict)
            {
                if (_fieldNames.Length != Configuration.CSVRecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of field headers found. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.CSVRecordFieldConfigurations.Count, _fieldNames.Length));

                string[] foundList = Configuration.CSVRecordFieldConfigurations.Select(i => i.FieldName).Except(_fieldNames, Configuration.FileHeaderConfiguration.StringComparer).ToArray();
                if (foundList.Any())
                    throw new ChoParserException("Header name(s) [{0}] are not found in file header.".FormatString(String.Join(",", foundList)));
            }

            if (Configuration.ColumnOrderStrict)
            {
                int colIndex = 0;
                foreach (string fieldName in Configuration.CSVRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Select(i => i.FieldName))
                {
                    if (!Configuration.FileHeaderConfiguration.IsEqual(_fieldNames[colIndex], fieldName))
                        throw new ChoParserException("Incorrect CSV column order found. Expected [{0}] CSV column at '{1}' location.".FormatString(fieldName, colIndex + 1));

                    colIndex++;
                }
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

        private bool? RaiseSkipUntil(Tuple<long, string> pair)
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

        private bool? RaiseDoWhile(Tuple<long, string> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackRecord.DoWhile(index, state));

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseDoWhile(index, state));

                return retValue;
            }
            return null;
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, string> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, string>(index, state as string);

                return retValue;
            }
            else if (target is IChoNotifyRecordRead)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordRead)target).BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, string>(index, state as string);

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

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
            if (_callbackRecord != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            else if (target is IChoNotifyRecordRead)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordRead)target).AfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            else if (Reader != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            skip = sp;
            return ret;
        }

        private bool RaiseRecordLoadError(object target, Tuple<long, string> pair, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            else if (target is IChoNotifyRecordRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordRead)target).RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (_callbackFieldRecord != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

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
            if (_callbackFieldRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackFieldRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, value, ex), false);
            }
            return true;
        }

        private bool RaiseMapColumn(object target, int colPos, string colName, out string newColName)
        {
            newColName = null;
            if (_customColumnMappableRecord != null)
            {
                bool retVal = false;
                string lnewColName = null;
                retVal = ChoFuncEx.RunWithIgnoreError(() => _customColumnMappableRecord.MapColumn(colPos, colName, out lnewColName), false);
                if (retVal)
                    newColName = lnewColName;
                return retVal;
            }
            else if (Reader != null)
            {
                string lnewColName = null;
                bool retVal = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseMapColumn(colPos, colName, out lnewColName), false);
                if (retVal)
                    newColName = lnewColName;

                return retVal;
            }
            return false;
        }

        private bool RaiseReportEmptyLine(object target, long index)
        {
            if (_emptyLineReportableRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _emptyLineReportableRecord.EmptyLineFound(index), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseReportEmptyLine(index), true);
            }
            return true;
        }

        #endregion Event Raisers
    }
}
