using GotDotNet.XPath;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChoETL
{
    internal class ChoXmlRecordReader : ChoRecordReader
    {
        private IChoNotifyRecordRead _callbackRecord;
        private bool _headerFound = false;
        private string[] _fieldNames = new string[] { };
        private bool _configCheckDone = false;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlRecordReader(Type recordType, ChoXmlRecordConfiguration configuration) : base(recordType)
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

            XmlReader sr = source as XmlReader;
            ChoGuard.ArgumentNotNull(sr, "XmlReader");

            //sr.Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(sr))
                yield break;

            XPathCollection xc = new XPathCollection();
            int childQuery = xc.Add(Configuration.XPath);
            XPathReader xpr = new XPathReader(sr, xc);
            int counter = 0;
            Tuple<int, XPathReader> pair = null;

            while (xpr.ReadUntilMatch())
            {
                if (xpr.Match(childQuery))
                {
                    pair = new Tuple<int, XPathReader>(++counter, xpr);

                    if (!_configCheckDone)
                    {
                        Configuration.Validate(pair);
                        _configCheckDone = true;
                    }

                    object rec = ChoActivator.CreateInstance(RecordType);
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading node [{0}]...".FormatString(pair.Item1));
                    if (!LoadNode(pair, ref rec))
                        yield break;
                    
                    if (rec == null)
                        continue;

                    yield return rec;
                }
            }

            RaiseEndLoad(sr);
        }

        private bool LoadNode(Tuple<int, XPathReader> pair, ref object rec)
        {
            return true;
        }

        //private bool LoadLine(Tuple<int, string> pair, ref object rec)
        //{
        //    try
        //    {
        //        if (!RaiseBeforeRecordLoad(rec, ref pair))
        //            return false;

        //        if (pair.Item2 == null)
        //        {
        //            rec = null;
        //            return true;
        //        }
        //        else if (pair.Item2 == String.Empty)
        //            return true;

        //        if (!pair.Item2.IsNullOrWhiteSpace())
        //        {
        //            if (!FillRecord(rec, pair))
        //                return false;

        //            rec.DoObjectLevelValidation(Configuration, Configuration.XmlRecordFieldConfigurations.ToArray());
        //        }

        //        if (!RaiseAfterRecordLoad(rec, pair))
        //            return false;
        //    }
        //    catch (ChoParserException)
        //    {
        //        throw;
        //    }
        //    catch (ChoMissingRecordFieldException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        ChoETLFramework.HandleException(ex);
        //        if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
        //        {
        //            rec = null;
        //        }
        //        else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
        //        {
        //            if (!RaiseRecordLoadError(rec, pair, ex))
        //                throw;
        //        }
        //        else
        //            throw;

        //        return true;
        //    }

        //    return true;
        //}

        //private Dictionary<string, string> ToFieldNameValues(string[] fieldValues)
        //{
        //    int index = 1;
        //    Dictionary<string, string> fnv = new Dictionary<string, string>(Configuration.FileHeaderConfiguration.StringComparer);
        //    if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
        //    {
        //        foreach (var name in _fieldNames)
        //        {
        //            if (index - 1 < fieldValues.Length)
        //                fnv.Add(name, fieldValues[index - 1]);
        //            else
        //                fnv.Add(name, String.Empty);

        //            index++;
        //        }
        //    }
        //    else
        //    {
        //        foreach (var fn in Configuration.RecordFieldConfigurationsDict.Keys)
        //        {
        //            if (index - 1 < fieldValues.Length)
        //                fnv.Add(fn, fieldValues[index - 1]);
        //            else
        //                fnv.Add(fn, String.Empty);

        //            index++;
        //        }
        //    }
        //    return fnv;
        //}

        //private bool FillRecord(object rec, Tuple<int, string> pair)
        //{
        //    int lineNo;
        //    string line;

        //    lineNo = pair.Item1;
        //    line = pair.Item2;


        //    string[] fieldValues = (from x in line.Split(Configuration.XPath, Configuration.StringSplitOptions, Configuration.QuoteChar)
        //                       select x).ToArray();
        //    if (Configuration.ColumnCountStrict)
        //    {
        //        if (fieldValues.Length != Configuration.XmlRecordFieldConfigurations.Count)
        //            throw new ChoParserException("Incorrect number of field values found at line [{2}]. Expected [{0}] field values. Found [{1}] field values.".FormatString(Configuration.XmlRecordFieldConfigurations.Count, fieldValues.Length, pair.Item1));
        //    }

        //    Dictionary<string, string> fieldNameValues = ToFieldNameValues(fieldValues);

        //    ValidateLine(pair.Item1, fieldValues);

        //    object fieldValue = null;
        //    ChoCSVRecordFieldConfiguration fieldConfig = null;
        //    foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
        //    {
        //        fieldValue = null;
        //        fieldConfig = kvp.Value;

        //        if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
        //        {
        //            if (fieldNameValues.ContainsKey(fieldConfig.FieldName))
        //                fieldValue = fieldNameValues[fieldConfig.FieldName];
        //            else if (Configuration.ColumnCountStrict)
        //                throw new ChoParserException("No matching '{0}' field header found.".FormatString(fieldConfig.FieldName));
        //        }
        //        else
        //        {
        //            if (fieldConfig.FieldPosition - 1 < fieldValues.Length)
        //                fieldValue = fieldValues[fieldConfig.FieldPosition - 1];
        //            else if (Configuration.ColumnCountStrict)
        //                throw new ChoParserException("Missing field value for {0} [Position: {1}] field.".FormatString(fieldConfig.FieldName, fieldConfig.FieldPosition));
        //        }

        //        if (rec is ExpandoObject)
        //        {
        //            if (kvp.Value.FieldType == null)
        //                kvp.Value.FieldType = typeof(string);
        //        }
        //        else
        //        {
        //            if (ChoType.HasProperty(rec.GetType(), kvp.Key))
        //            {
        //                kvp.Value.FieldType = ChoType.GetMemberType(rec.GetType(), kvp.Key);
        //            }
        //            else
        //                kvp.Value.FieldType = typeof(string);
        //        }

        //        fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string);

        //        if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
        //            return false;

        //        try
        //        {
        //            bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
        //            if (ignoreFieldValue)
        //                fieldValue = null;

        //            if (rec is ExpandoObject)
        //            {
        //                var dict = rec as IDictionary<string, Object>;

        //                dict.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture);

        //                if (ignoreFieldValue)
        //                    dict.AddOrUpdate(kvp.Key, fieldValue);
        //                else
        //                    dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);

        //                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
        //            }
        //            else
        //            {
        //                if (ChoType.HasProperty(rec.GetType(), kvp.Key))
        //                {
        //                    rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture);

        //                    if (!ignoreFieldValue)
        //                        rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
        //                }
        //                else
        //                    throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(kvp.Key, ChoType.GetTypeName(rec)));

        //                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
        //            }

        //            if (!RaiseAfterRecordFieldLoad(rec, pair.Item1, kvp.Key, fieldValue))
        //                return false;
        //        }
        //        catch (ChoParserException)
        //        {
        //            throw;
        //        }
        //        catch (ChoMissingRecordFieldException)
        //        {
        //            if (Configuration.ThrowAndStopOnMissingField)
        //                throw;
        //        }
        //        catch (Exception ex)
        //        {
        //            ChoETLFramework.HandleException(ex);

        //            if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
        //                throw;

        //            try
        //            {
        //                if (rec is ExpandoObject)
        //                {
        //                    var dict = rec as IDictionary<string, Object>;

        //                    if (dict.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
        //                    {
        //                        dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
        //                    }
        //                    else
        //                        throw new ChoParserException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
        //                }
        //                else if (ChoType.HasProperty(rec.GetType(), kvp.Key) && rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
        //                {
        //                    rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
        //                }
        //                else
        //                    throw new ChoParserException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
        //            }
        //            catch (Exception innerEx)
        //            {
        //                if (ex == innerEx.InnerException)
        //                {
        //                    if (fieldConfig.ErrorMode == ChoErrorMode.IgnoreAndContinue)
        //                    {
        //                        continue;
        //                    }
        //                    else
        //                    {
        //                        if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
        //                            throw new ChoParserException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
        //                    }
        //                }
        //                else
        //                {
        //                    throw new ChoParserException("Failed to assign '{0}' fallback value to '{1}' field.".FormatString(fieldValue, fieldConfig.FieldName), innerEx);
        //                }
        //            }
        //        }
        //    }

        //    return true;
        //}

        private string CleanFieldValue(ChoCSVRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue.IsNull()) return fieldValue;

            if (fieldValue != null)
            {
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

            if (config.QuoteField != null && config.QuoteField.Value && fieldValue.StartsWith(@"""") && fieldValue.EndsWith(@""""))
                return fieldValue.Substring(1, fieldValue.Length - 2);
            else if ((fieldValue.Contains(Configuration.XPath)
                || fieldValue.Contains(Configuration.EOLDelimiter)) && fieldValue.StartsWith(@"""") && fieldValue.EndsWith(@""""))
                return fieldValue.Substring(1, fieldValue.Length - 2);
            else
                return fieldValue;
        }

        //private void ValidateLine(int lineNo, string[] fieldValues)
        //{
        //    int maxPos = Configuration.MaxFieldPosition;

        //    if (Configuration.ColumnCountStrict)
        //    {
        //        if (fieldValues.Length != maxPos)
        //            throw new ApplicationException("Mismatched number of fields found at {0} line. [Expected: {1}, Found: {2}].".FormatString(
        //                lineNo, maxPos, fieldValues.Length));
        //    }

        //    //ChoCSVRecordFieldAttribute attr = null;
        //    //foreach (Tuple<MemberInfo, ChoOrderedAttribute> member in _members)
        //    //{
        //    //    if (attr.Position > fields.Length)
        //    //        throw new ApplicationException("Record Member '{0}' has incorrect Position specified.".FormatString(ChoType.GetMemberName(member.Item1)));
        //    //}
        //}

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
                    Configuration.XPath = delimiter;
                }

                return true;
            }

            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Excel separator NOT found. Default separator [{0}] used.".FormatString(Configuration.XPath));
            return false;
        }

        //private string[] GetHeaders(string line)
        //{
        //    if (Configuration.FileHeaderConfiguration.HasHeaderRecord)
        //        return (from x in line.Split(Configuration.XPath, Configuration.StringSplitOptions, Configuration.QuoteChar)
        //                select CleanHeaderValue(x)).ToArray();
        //    else
        //    {
        //        if (RecordType == typeof(ExpandoObject))
        //        {
        //            int index = 0;
        //            return (from x in line.Split(Configuration.XPath, Configuration.StringSplitOptions, Configuration.QuoteChar)
        //                    select "Column{0}".FormatString(++index)).ToArray();
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //}

        //private void LoadHeaderLine(Tuple<int, string> pair)
        //{
        //    string line = pair.Item2;

        //    //Validate header
        //    _fieldNames = GetHeaders(line);
            
        //    if (_fieldNames.Length == 0)
        //        throw new ChoParserException("No headers found.");

        //    //Check any header value empty
        //    if (_fieldNames.Where(i => i.IsNullOrWhiteSpace()).Any())
        //        throw new ChoParserException("At least one of the field header is empty.");

        //    if (Configuration.ColumnCountStrict)
        //    {
        //        if (_fieldNames.Length != Configuration.XmlRecordFieldConfigurations.Count)
        //            throw new ChoParserException("Incorrect number of field headers found. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.XmlRecordFieldConfigurations.Count, _fieldNames.Length));

        //        string[] foundList = Configuration.XmlRecordFieldConfigurations.Select(i => i.FieldName).Except(_fieldNames, Configuration.FileHeaderConfiguration.StringComparer).ToArray();
        //        if (foundList.Any())
        //            throw new ChoParserException("Header name(s) [{0}] are not found in file header.".FormatString(String.Join(",", foundList)));

        //        if (Configuration.ColumnOrderStrict)
        //        {
        //            int colIndex = 0;
        //            foreach (string fieldName in Configuration.XmlRecordFieldConfigurations.OrderBy(i => i.FieldPosition).Select(i => i.Name))
        //            {
        //                if (String.Compare(_fieldNames[colIndex], fieldName, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture) != 0)
        //                    throw new ChoParserException("Incorrect CSV column order found. Expected [{0}] CSV column at '{1}' location.".FormatString(fieldName, colIndex + 1));

        //                colIndex++;

        //            }
        //        }
        //    }
        //}

        //private string CleanHeaderValue(string headerValue)
        //{
        //    if (headerValue.IsNull()) return headerValue;

        //    ChoFileHeaderConfiguration config = Configuration.FileHeaderConfiguration;
        //    if (headerValue != null)
        //    {
        //        switch (config.TrimOption)
        //        {
        //            case ChoFieldValueTrimOption.Trim:
        //                headerValue = headerValue.Trim();
        //                break;
        //            case ChoFieldValueTrimOption.TrimStart:
        //                headerValue = headerValue.TrimStart();
        //                break;
        //            case ChoFieldValueTrimOption.TrimEnd:
        //                headerValue = headerValue.TrimEnd();
        //                break;
        //        }
        //    }

        //    if (Configuration.QuoteAllFields && headerValue.StartsWith(@"""") && headerValue.EndsWith(@""""))
        //        return headerValue.Substring(1, headerValue.Length - 2);
        //    else
        //        return headerValue;
        //}

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
            if (_callbackRecord == null) return false;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
        }
    }
}
