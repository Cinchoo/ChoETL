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
    internal class ChoKVPRecordReader : ChoRecordReader
    {
        private IChoNotifyRecordRead _callbackRecord;
        private string[] _fieldNames = new string[] { };
        private bool _configCheckDone = false;

        public ChoKVPRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoKVPRecordReader(Type recordType, ChoKVPRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;
            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            //Configuration.Validate();
        }

        public override void LoadSchema(object source)
        {
            var e = AsEnumerable(source, ChoETLFramework.TraceSwitchOff).GetEnumerator();
            e.MoveNext();
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            return AsEnumerable(source, TraceSwitch, filterFunc);
        }

        private IEnumerable<object> AsEnumerable(object source, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            StreamReader sr = source as StreamReader;
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            sr.Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(sr))
                yield break;

            string[] commentTokens = Configuration.Comments;
            bool? skip = false;
            bool isRecordStartFound = false;
            bool isRecordEndFound = false;
            long seekOriginPos = sr.BaseStream.Position;
            List<string> headers = new List<string>();
            bool isHeaderFound = false;
            List<Tuple<int, string>> recLines = new List<Tuple<int, string>>();
            int recNo = 0;

            for (int i = 0; i < 2; i++)
            {
                if (i == 1)
                {
                    sr.Seek(seekOriginPos, SeekOrigin.Begin);
                    TraceSwitch = traceSwitch;
                }
                else
                    TraceSwitch = ChoETLFramework.TraceSwitchOff;

                using (ChoPeekEnumerator<Tuple<int, string>> e = new ChoPeekEnumerator<Tuple<int, string>>(
                    new ChoIndexedEnumerator<string>(sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData)).ToEnumerable(),
                    (pair) =>
                    {
                    //bool isStateAvail = IsStateAvail();
                    skip = false;

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
                                throw new ChoParserException("Empty line found at {0} location.".FormatString(pair.Item1));
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
                            _configCheckDone = true;
                        }

                        return false;
                    }))
                {
                    while (true)
                    {
                        Tuple<int, string> pair = e.Peek;
                        if (pair == null)
                        {
                            RaiseEndLoad(sr);
                            yield break;
                        }

                        if (!isRecordStartFound)
                        {
                            recLines.Clear();
                            isRecordStartFound = true;
                            if (!Configuration.RecordStart.IsNullOrWhiteSpace())
                            {
                                //Move to record start
                                while (String.Compare(e.Peek.Item2, Configuration.RecordStart, Configuration.FileHeaderConfiguration.IgnoreCase) == 0)
                                {
                                    e.MoveNext();
                                }
                            }

                            e.MoveNext();
                            continue;
                        }
                        else
                        {
                            if (!Configuration.RecordEnd.IsNullOrWhiteSpace())
                            {
                                //Move to record start
                                if (String.Compare(e.Peek.Item2, Configuration.RecordEnd, Configuration.FileHeaderConfiguration.IgnoreCase) == 0)
                                {
                                    isRecordEndFound = true;
                                    isRecordStartFound = false;
                                }
                            }

                            if (!isHeaderFound)
                            {
                                if (isRecordEndFound && headers.Count == 0)
                                    throw new ChoParserException("Unexpected EOF found.");
                                else if (!isRecordEndFound)
                                {
                                    string header = pair.Item2.Split(Configuration.Seperator).FirstOrDefault();
                                    if (!header.IsNullOrWhiteSpace())
                                        headers.Add(header);

                                    e.MoveNext();
                                }
                                else
                                {
                                    Configuration.Validate(headers.ToArray());
                                    isHeaderFound = true;
                                    isRecordEndFound = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (isRecordEndFound && recLines.Count == 0)
                                    throw new ChoParserException("Unexpected EOF found.");
                                else if (!isRecordEndFound)
                                {
                                    recLines.Add(pair);
                                    e.MoveNext();
                                }
                                else
                                {
                                    object rec = Activator.CreateInstance(RecordType);
                                    if (!LoadLines(new Tuple<int, List<Tuple<int, string>>>(++recNo, recLines), ref rec))
                                        yield break;

                                    //StoreState(e.Current, rec != null);

                                    e.MoveNext();

                                    if (rec == null)
                                        continue;

                                    yield return rec;

                                    if (Configuration.NotifyAfter > 0 && pair.Item1 % Configuration.NotifyAfter == 0)
                                    {
                                        if (RaisedRowsLoaded(pair.Item1))
                                        {
                                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                                            yield break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool LoadLines(Tuple<int, List<Tuple<int, string>>> pairs, ref object rec)
        {
            Tuple<int, string> pair = null;
            int recNo = pairs.Item1;

            foreach (var pair1 in pairs.Item2)
            {
                pair = pair1;
                try
                {
                    if (Configuration.ColumnCountStrict)
                    {
                        if (pairs.Item2.Count != Configuration.KVPRecordFieldConfigurations.Count)
                            throw new ChoParserException("Incorrect number of field values found at record [{2}]. Expected [{0}] field values. Found [{1}] field values.".FormatString(Configuration.KVPRecordFieldConfigurations.Count, pairs.Item2.Count, recNo));
                    }

                    if (!RaiseBeforeRecordLoad(rec, ref pair))
                        return false;

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

                    if (!RaiseAfterRecordLoad(rec, pair))
                        return false;
                }
                catch (ChoParserException)
                {
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ChoETLFramework.HandleException(ex);
                    if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                    {
                        rec = null;
                    }
                    else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                    {
                        if (!RaiseRecordLoadError(rec, pair, ex))
                            throw;
                    }
                    else
                        throw;

                    return true;
                }
            }

            return true;
        }

        private bool FillRecord(object rec, Tuple<int, string> pair)
        {
            int lineNo;
            string line;

            lineNo = pair.Item1;
            line = pair.Item2;

            //var tokens = pair.Item2.Split(Configuration.Seperator).Take(2).ToArray();
            //if (tokens.Length < 2)
            //    throw new ChoParserException("Invalid line found.");


            //ValidateLine(pair.Item1, fieldValues);

            object fieldValue = null;
            ChoKVPRecordFieldConfiguration fieldConfig = null;
            PropertyInfo pi = null;
            foreach (KeyValuePair<string, ChoKVPRecordFieldConfiguration> kvp in Configuration.FCArray)
            {
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                    if (Configuration.ColumnCountStrict)
                        throw new ChoParserException("Missing field value for '{0}' [Position: {1}] field.".FormatString(fieldConfig.FieldName));

                if (Configuration.IsDynamicObject)
                {
                    if (kvp.Value.FieldType == null)
                        kvp.Value.FieldType = typeof(string);
                }
                else
                {
                    if (pi != null)
                        kvp.Value.FieldType = pi.PropertyType;
                    else
                        kvp.Value.FieldType = typeof(string);
                }

                fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string);

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    return false;

                try
                {
                    bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
                        if (pi != null)
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else
                            throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(kvp.Key, ChoType.GetTypeName(rec)));

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }

                    if (!RaiseAfterRecordFieldLoad(rec, pair.Item1, kvp.Key, fieldValue))
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
                    ChoETLFramework.HandleException(ex);

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
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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

        private string CleanFieldValue(ChoKVPRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim;

            if (config.FieldValueTrimOption == null)
            {
                //if (fieldType == typeof(string))
                //    fieldValueTrimOption = ChoFieldValueTrimOption.None;
            }
            else
                fieldValueTrimOption = config.FieldValueTrimOption.Value;

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
                        fieldValue = fieldValue.Substring(0, config.Size.Value);
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
                    (fieldValue.Contains(Configuration.Seperator)
                    || fieldValue.Contains(Configuration.EOLDelimiter)))
                    return fieldValue.Substring(1, fieldValue.Length - 2);

            }
            return fieldValue;
        }

        private void ValidateLine(int lineNo, string[] fieldValues)
        {
            //int maxPos = Configuration.MaxFieldPosition;

            //if (Configuration.ColumnCountStrict)
            //{
            //    if (fieldValues.Length != maxPos)
            //        throw new ChoReaderException("Mismatched number of fields found at {0} line. [Expected: {1}, Found: {2}].".FormatString(
            //            lineNo, maxPos, fieldValues.Length));
            //}

            //ChoKVPRecordFieldAttribute attr = null;
            //foreach (Tuple<MemberInfo, ChoOrderedAttribute> member in _members)
            //{
            //    if (attr.Position > fields.Length)
            //        throw new ApplicationException("Record Member '{0}' has incorrect Position specified.".FormatString(ChoType.GetMemberName(member.Item1)));
            //}
        }

        private bool LoadExcelSeperatorIfAny(Tuple<int, string> pair)
        {
            string line = pair.Item2.NTrim();
            if (!line.IsNullOrWhiteSpace() && line.StartsWith("sep=", true, Configuration.Culture))
            {
                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator specified at [{0}]...".FormatString(pair.Item1));
                string delimiter = line.Substring(4);
                if (!delimiter.IsNullOrWhiteSpace())
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator [{0}] found.".FormatString(delimiter));
                    Configuration.Seperator = delimiter;
                }

                return true;
            }

            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator NOT found. Default separator [{0}] used.".FormatString(Configuration.Seperator));
            return false;
        }

        private string[] GetHeaders(string line)
        {
                if (RecordType == typeof(ExpandoObject))
                {
                    int index = 0;
                    return (from x in line.Split(Configuration.Seperator, Configuration.StringSplitOptions, Configuration.QuoteChar)
                            select "Column{0}".FormatString(++index)).ToArray();
                }
                else
                {
                    return null;
                }
        }

        private bool RaiseBeginLoad(object state)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(state), true);
        }

        private void RaiseEndLoad(object state)
        {
            if (_callbackRecord == null) return;
            ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndLoad(state));
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<int, string> pair)
        {
            if (_callbackRecord == null) return true;
            int index = pair.Item1;
            object state = pair.Item2;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordLoad(target, index, ref state), true);

            if (retValue)
                pair = new Tuple<int, string>(index, state as string);

            return retValue;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<int, string> pair)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2), true);
        }

        private bool RaiseRecordLoadError(object target, Tuple<int, string> pair, Exception ex)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
        }

        private bool RaiseBeforeRecordFieldLoad(object target, int index, string propName, ref object value)
        {
            if (_callbackRecord == null) return true;
            object state = value;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

            if (retValue)
                value = state;

            return retValue;
        }

        private bool RaiseAfterRecordFieldLoad(object target, int index, string propName, object value)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordFieldLoad(target, index, propName, value), true);
        }

        private bool RaiseRecordFieldLoadError(object target, int index, string propName, object value, Exception ex)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldLoadError(target, index, propName, value, ex), true);
        }
    }
}
