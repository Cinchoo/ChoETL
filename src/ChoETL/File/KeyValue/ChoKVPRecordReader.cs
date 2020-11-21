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
    public interface IChoNotifyKVPRecordRead
    {
        KeyValuePair<string, string>? ToKVP(string recText);
    }

    internal class ChoKVPRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoNotifyKVPRecordRead _customKVPRecord;
        private IChoCustomColumnMappable _customColumnMappableRecord;
        private IChoEmptyLineReportable _emptyLineReportableRecord;
        private string[] _fieldNames = new string[] { };
        private bool _configCheckDone = false;
        private Dictionary<string, bool> _propInit = new Dictionary<string, bool>();
        internal ChoBaseKVPReader Reader = null;

        public ChoKVPRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoKVPRecordReader(Type recordType, ChoKVPRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _customColumnMappableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoCustomColumnMappable>(recordType);
            _emptyLineReportableRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoEmptyLineReportable>(recordType);

            _customKVPRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyKVPRecordRead>(recordType);
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
            ChoGuard.ArgumentNotNull(sr, "TextReader");

            if (sr is StreamReader)
                ((StreamReader)sr).Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(sr))
                yield break;

            string[] commentTokens = Configuration.Comments;
            bool? skip = false;
            bool isRecordStartFound = false;
            bool isRecordEndFound = false;
            long seekOriginPos = sr is StreamReader ? ((StreamReader)sr).BaseStream.Position : 0;
            List<string> headers = new List<string>();
            Tuple<long, string> lastLine = null;
            List<Tuple<long, string>> recLines = new List<Tuple<long, string>>();
            long recNo = 0;
            int loopCount = Configuration.AutoDiscoverColumns && Configuration.KVPRecordFieldConfigurations.Count == 0 ? 2 : 1;
            bool isHeaderFound = loopCount == 1;
            bool IsHeaderLoaded = false;
            Tuple<long, string> pairIn;
            bool abortRequested = false;
            bool? skipUntil = true;
            bool? doWhile = true;

            for (int i = 0; i < loopCount; i++)
            {
                if (i == 1)
                {
                    if (sr is StreamReader)
                        ((StreamReader)sr).Seek(seekOriginPos, SeekOrigin.Begin);
                    TraceSwitch = traceSwitch;
                }
                else
                    TraceSwitch = ChoETLFramework.TraceSwitchOff;
                lastLine = null;
                recLines.Clear();
                isRecordEndFound = false;
                isRecordStartFound = false;

                using (ChoPeekEnumerator<Tuple<long, string>> e = new ChoPeekEnumerator<Tuple<long, string>>(
                    new ChoIndexedEnumerator<string>(sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData, Configuration.MaxLineSize)).ToEnumerable(),
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
                            return skip;

                    //if (!(sr.BaseStream is MemoryStream))
                    //    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, ChoETLFramework.Switch.TraceVerbose, "Loading line [{0}]...".FormatString(item.Item1));

                    //if (Task != null)
                    //    return !IsStateNOTExistsOrNOTMatch(item);

                    if (pair.Item2.IsNullOrWhiteSpace())
                        {
                            if (!Configuration.IgnoreEmptyLine)
                                throw new ChoParserException("Empty line found at [{0}] location.".FormatString(pair.Item1));
                            else
                            {
                                if (TraceSwitch.TraceVerbose)
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring empty line found at [{0}].".FormatString(pair.Item1));
                                return true;
                            }
                        }

                        if (commentTokens != null && commentTokens.Length > 0)
                        {
                            foreach (string comment in commentTokens)
                            {
                                if (!pair.Item2.IsNull() && pair.Item2.StartsWith(comment, StringComparison.Ordinal)) //, true, Configuration.Culture))
                            {
                                    if (TraceSwitch.TraceVerbose)
                                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Comment line found at [{0}]...".FormatString(pair.Item1));
                                    return true;
                                }
                            }
                        }

                        if (!_configCheckDone)
                        {
                            Configuration.Validate(null);
                            var dict = Configuration.KVPRecordFieldConfigurations.ToDictionary(i1 => i1.Name, i1 => i1.FieldType == null ? null : i1.FieldType);
                            RaiseMembersDiscovered(dict);
                            Configuration.UpdateFieldTypesIfAny(dict);
                            _configCheckDone = true;
                        }

                        return false;
                    }))
                {
                    while (true)
                    {
                        pairIn = e.Peek;

                        if (!isRecordStartFound)
                        {
                            if (pairIn == null)
                                break;

                            lastLine = null;
                            recLines.Clear();
                            isRecordEndFound = false;
                            isRecordStartFound = true;
                            if (!Configuration.RecordStart.IsNullOrWhiteSpace())
                            {
                                //Move to record start
                                while (!(Configuration.IsRecordStartMatch(e.Peek.Item2)))
                                {
                                    e.MoveNext();
                                    if (e.Peek == null)
                                        break;
                                }
                            }
                            if (e.Peek != null)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Record start found at [{0}] line...".FormatString(e.Peek.Item1));

                            e.MoveNext();
                            continue;
                        }
                        else
                        {
                            string recordEnd = !Configuration.RecordEnd.IsNullOrWhiteSpace() ? Configuration.RecordEnd : Configuration.RecordStart;
                            if (!recordEnd.IsNullOrWhiteSpace())
                            {
                                if (e.Peek == null)
                                {
                                    if (Configuration.RecordEnd.IsNullOrWhiteSpace())
                                        isRecordEndFound = true;
                                    else
                                        break;
                                }
                                else
                                {
                                    //Move to record start
                                    if (Configuration.IsRecordEndMatch(e.Peek.Item2))
                                    {
                                        isRecordEndFound = true;
                                        isRecordStartFound = false;
                                        if (e.Peek != null)
                                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Record end found at [{0}] line...".FormatString(e.Peek.Item1));
                                    }
                                }
                            }
                            else if (e.Peek == null)
                                isRecordEndFound = true;

                            if (!isHeaderFound)
                            {
                                //if (isRecordEndFound && headers.Count == 0)
                                //{
                                //    //throw new ChoParserException("Unexpected EOF found.");
                                //}
                                if (!isRecordEndFound)
                                {
                                    e.MoveNext();
                                    if (e.Peek != null)
                                    {
                                        //If line empty or line continuation, skip
                                        if (pairIn.Item2.IsNullOrWhiteSpace() || IsLineContinuationCharFound(pairIn.Item2)) //.Item2[0] == ' ' || pairIn.Item2[0] == '\t')
                                        {

                                        }
                                        else
                                        {
                                            string header = ToKVP(pairIn.Item1, pairIn.Item2).Key;
                                            if (!header.IsNullOrWhiteSpace())
                                                headers.Add(header);
                                        }
                                    }
                                }
                                else
                                {
                                    Configuration.Validate(headers.ToArray());
                                    isHeaderFound = true;
                                    isRecordEndFound = false;
                                    IsHeaderLoaded = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (!IsHeaderLoaded)
                                {
                                    Configuration.Validate(new string[] { });
                                    IsHeaderLoaded = true;
                                }

                                if (isRecordEndFound && recLines.Count == 0)
                                {
                                    //throw new ChoParserException("Unexpected EOF found.");
                                }
                                else if (!isRecordEndFound)
                                {
                                    e.MoveNext();
                                    if (e.Peek != null)
                                    {
                                        //If line empty or line continuation, skip
                                        if (pairIn.Item2.IsNullOrWhiteSpace())
                                        {
                                            if (!Configuration.IgnoreEmptyLine)
                                                throw new ChoParserException("Empty line found at [{0}] location.".FormatString(pairIn.Item1));
                                            else
                                            {
                                                Tuple<long, string> t = new Tuple<long, string>(lastLine.Item1, lastLine.Item2 + Configuration.EOLDelimiter);
                                                recLines.RemoveAt(recLines.Count - 1);
                                                recLines.Add(t);
                                            }
                                        }
                                        else if (IsLineContinuationCharFound(pairIn.Item2)) //pairIn.Item2[0] == ' ' || pairIn.Item2[0] == '\t')
                                        {
                                            if (lastLine == null)
                                                throw new ChoParserException("Unexpected line continuation found at {0} location.".FormatString(pairIn.Item1));
                                            else
                                            {
                                                Tuple<long, string> t = new Tuple<long, string>(lastLine.Item1, lastLine.Item2 + Configuration.EOLDelimiter + pairIn.Item2);
                                                recLines.RemoveAt(recLines.Count - 1);
                                                recLines.Add(t);
                                            }
                                        }
                                        else
                                        {
                                            lastLine = pairIn;
                                            recLines.Add(pairIn);
                                        }
                                    }
                                }
                                else
                                {
                                    object rec = Configuration.IsDynamicObject ? new ChoDynamicObject(new Dictionary<string, object>(Configuration.FileHeaderConfiguration.StringComparer)) { ThrowExceptionIfPropNotExists = true,
                                        AlternativeKeys = Configuration.AlternativeKeys
                                    } : ChoActivator.CreateInstance(RecordType);
                                    if (!LoadLines(new Tuple<long, List<Tuple<long, string>>>(++recNo, recLines), ref rec))
                                        yield break;

                                   isRecordStartFound = false;
                                    //StoreState(e.Current, rec != null);

                                    if (!Configuration.RecordEnd.IsNullOrWhiteSpace())
                                        e.MoveNext();

                                    if (rec == null)
                                        continue;

                                    yield return rec;

                                    if (Configuration.NotifyAfter > 0 && recNo % Configuration.NotifyAfter == 0)
                                    {
                                        if (RaisedRowsLoaded(recNo))
                                        {
                                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                                            abortRequested = true;
                                            yield break;
                                        }
                                    }

                                    if (e.Peek == null)
                                        break;
                                }
                            }
                        }

                        if (doWhile != null)
                        {
                            doWhile = RaiseDoWhile(pairIn);
                            if (doWhile != null && doWhile.Value)
                                break;
                        }
                    }
                }
            }

            if (!abortRequested)
                RaisedRowsLoaded(recNo, true);
            RaiseEndLoad(sr);
        }

        private bool IsLineContinuationCharFound(string line)
        {
            if (Configuration.LineContinuationChars.IsNullOrEmpty())
                return false;

            return Configuration.LineContinuationChars.Contains(line[0]);
        }

        private bool LoadLines(Tuple<long, List<Tuple<long, string>>> pairs, ref object rec)
        {
            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading [{0}] record...".FormatString(pairs.Item1));

            Tuple<long, string> pair = null;
            long recNo = pairs.Item1;

            if (!Configuration.AutoDiscoveredColumns)
            {
                if (Configuration.ColumnCountStrict)
                {
                    if (pairs.Item2.Count != Configuration.KVPRecordFieldConfigurations.Count)
                        throw new ChoParserException("Incorrect number of field values found at record [{2}]. Expected [{0}] field values. Found [{1}] field values.".FormatString(Configuration.KVPRecordFieldConfigurations.Count, pairs.Item2.Count, recNo));
                    List<string> keys = (from p in pairs.Item2
                                         select ToKVP(p.Item1, p.Item2).Key).ToList();

                    if (!Enumerable.SequenceEqual(keys.OrderBy(t => t), Configuration.FCArray.Select(a => a.Value.FieldName).OrderBy(t => t), Configuration.FileHeaderConfiguration.StringComparer))
                    {
                        throw new ChoParserException("Column count mismatch detected at [{0}] record.".FormatString(recNo));
                    }
                }
                if (Configuration.ColumnOrderStrict)
                {
                    List<string> keys = (from p in pairs.Item2
                                         select ToKVP(p.Item1, p.Item2).Key).ToList();
                    int runnngIndex = -1;
                    foreach (var k in keys)
                    {
                        runnngIndex++;
                        if (runnngIndex < Configuration.FCArray.Length)
                        {
                            if (Configuration.FileHeaderConfiguration.IsEqual(Configuration.FCArray[runnngIndex].Value.FieldName, k))
                                continue;
                        }
                        throw new ChoParserException("Found incorrect order on '{1}' column at [{0}] record.".FormatString(recNo, k));
                    }
                }
            }

            object fieldValue = String.Empty;
            PropertyInfo pi = null;
            object rootRec = rec;
            //Set default values
            foreach (KeyValuePair<string, ChoKVPRecordFieldConfiguration> kvp in Configuration.FCArray)
            {
                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);
                if (Configuration.PIDict != null)
                {
                    // if FieldName is set
                    if (!string.IsNullOrEmpty(kvp.Value.FieldName))
                    {
                        // match using FieldName
                        Configuration.PIDict.TryGetValue(kvp.Value.FieldName, out pi);
                    }
                    else
                    {
                        // otherwise match usign the property name
                        Configuration.PIDict.TryGetValue(kvp.Key, out pi);
                    }
                }

                try
                {
                    if (kvp.Value.IsDefaultValueSpecified)
                        fieldValue = kvp.Value.DefaultValue;

                    if (Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;
                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                    }
                    else
                    {
                        if (pi != null)
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                    }
                }
                catch
                {

                }
            }

            rec = rootRec;

            _propInit.Clear();
            foreach (var pair1 in pairs.Item2)
            {
                pair = pair1;
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
                            rec.DoObjectLevelValidation(Configuration, Configuration.KVPRecordFieldConfigurations);
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
            }

            return true;
        }

        private KeyValuePair<string, string> ToKVP(long lineNo, string line)
        {
            var kvp = RaiseCustomKVPReader(line);
            if (kvp != null)
                return kvp.Value;

            if (Configuration.Separator.Length == 0)
            {
                throw new ChoParserException("Missing separator.");
            }
            else if (Configuration.Separator.Length == 1)
            {
                int pos = line.IndexOf(Configuration.Separator[0]);
                if (pos <= 0)
                    return new KeyValuePair<string, string>(CleanKeyValue(line), String.Empty);
                //throw new ChoMissingRecordFieldException("Missing key at '{0}' line.".FormatString(lineNo));
                return new KeyValuePair<string, string>(CleanKeyValue(line.Substring(0, pos)), line.Substring(pos + 1));
            }
            else
            {
                int pos = line.IndexOf(Configuration.Separator);
                if (pos <= 0)
                    return new KeyValuePair<string, string>(CleanKeyValue(line), String.Empty);
                //throw new ChoMissingRecordFieldException("Missing key at '{0}' line.".FormatString(lineNo));
                return new KeyValuePair<string, string>(CleanKeyValue(line.Substring(0, pos)), line.Substring(pos + Configuration.Separator.Length));
            }
        }

        private string CleanFieldValue(ChoKVPRecordFieldConfiguration config, Type fieldType, string fieldValue)
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

            char startChar;
            char endChar;
            char quoteChar = Configuration.QuoteChar == '\0' ? '"' : Configuration.QuoteChar;

            if (fieldValue.Length >= 2)
            {
                startChar = fieldValue[0];
                endChar = fieldValue[fieldValue.Length - 1];

                if (config.QuoteField != null && config.QuoteField.Value && startChar == quoteChar && endChar == quoteChar)
                    return fieldValue.Substring(1, fieldValue.Length - 2);
                else if (startChar == quoteChar && endChar == quoteChar &&
                    (fieldValue.Contains(Configuration.Separator)
                    || fieldValue.Contains(Configuration.EOLDelimiter)))
                    return fieldValue.Substring(1, fieldValue.Length - 2);

            }
            return fieldValue;
        }

        private string CleanKeyValue(string key)
        {
            if (key.IsNull()) return key;

            ChoFileHeaderConfiguration config = Configuration.FileHeaderConfiguration;
            if (key != null)
            {
                switch (config.TrimOption)
                {
                    case ChoFieldValueTrimOption.Trim:
                        key = key.Trim();
                        break;
                    case ChoFieldValueTrimOption.TrimStart:
                        key = key.TrimStart();
                        break;
                    case ChoFieldValueTrimOption.TrimEnd:
                        key = key.TrimEnd();
                        break;
                }
            }

            if (Configuration.QuoteAllFields != null && Configuration.QuoteAllFields.Value && key.StartsWith(@"""") && key.EndsWith(@""""))
                return key.Substring(1, key.Length - 2);
            else
                return key;
        }

        private bool FillRecord(object rec, Tuple<long, string> pair)
        {
            long lineNo;
            string line;

            lineNo = pair.Item1;
            line = pair.Item2;

            var tokens = ToKVP(pair.Item1, pair.Item2);

            //ValidateLine(pair.Item1, fieldValues);

            object fieldValue = tokens.Value;
            string key = tokens.Key;
            if (!Configuration.RecordFieldConfigurationsDict2.ContainsKey(key))
                return true;
            key = Configuration.RecordFieldConfigurationsDict2[key].Name;
            ChoKVPRecordFieldConfiguration fieldConfig = Configuration.RecordFieldConfigurationsDict[key];
            PropertyInfo pi = null;

            if (Configuration.IsDynamicObject)
            {
                if (Configuration.IgnoredFields.Contains(key))
                    return true;
            }

            try
            {
                if (_propInit.ContainsKey(key))
                    return true;
                _propInit.Add(key, true);

                fieldValue = CleanFieldValue(fieldConfig, fieldConfig.FieldType, fieldValue as string);

                if (Configuration.IsDynamicObject)
                {
                    if (fieldConfig.FieldType == null)
                        fieldConfig.FieldType = typeof(string);
                }
                else
                {
                    if (Configuration.PIDict != null)
                        Configuration.PIDict.TryGetValue(key, out pi);

                    if (pi != null)
                        fieldConfig.FieldType = pi.PropertyType;
                    else
                        fieldConfig.FieldType = typeof(string);
                }

                fieldValue = CleanFieldValue(fieldConfig, fieldConfig.FieldType, fieldValue as string);

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, key, ref fieldValue))
                    return true;

                bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
                if (ignoreFieldValue)
                    fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                if (Configuration.IsDynamicObject)
                {
                    var dict = rec as IDictionary<string, Object>;

                    dict.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                        dict.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
                }
                else
                {
                    if (pi != null)
                        rec.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);
                    else
                        throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(key, ChoType.GetTypeName(rec)));

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                        rec.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
                }

                if (!RaiseAfterRecordFieldLoad(rec, pair.Item1, key, fieldValue))
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

                        if (dict.SetFallbackValue(key, fieldConfig, Configuration.Culture, ref fieldValue))
                            dict.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
                        else if (dict.SetDefaultValue(key, fieldConfig, Configuration.Culture))
                            dict.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
                        else if (ex is ValidationException)
                            throw;
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                    }
                    else if (pi != null)
                    {
                        if (rec.SetFallbackValue(key, fieldConfig, Configuration.Culture))
                            rec.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
                        else if (rec.SetDefaultValue(key, fieldConfig, Configuration.Culture))
                            rec.DoMemberLevelValidation(key, fieldConfig, Configuration.ObjectValidationMode);
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
                        }
                        else
                        {
                            if (!RaiseRecordFieldLoadError(rec, pair.Item1, key, ref fieldValue, ex))
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

                                        dict.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);
                                    }
                                    else
                                    {
                                        if (pi != null)
                                            rec.ConvertNSetMemberValue(key, fieldConfig, ref fieldValue, Configuration.Culture);
                                        else
                                            throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(key, ChoType.GetTypeName(rec)));
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

            return true;
        }

        private KeyValuePair<string, string>? RaiseCustomKVPReader(string recText)
        {
            KeyValuePair<string, string>? kvp = null;
            if (Reader is IChoCustomKVPReader && ((IChoCustomKVPReader)Reader).HasCustomKVPSubscribed)
            {
                kvp = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseToKVP(recText));
            }
            else if (_customKVPRecord != null)
            {
                kvp = ChoFuncEx.RunWithIgnoreError(() => _customKVPRecord.ToKVP(recText));
            }
            return kvp;
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
