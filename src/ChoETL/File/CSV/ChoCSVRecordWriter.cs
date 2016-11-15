using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoCSVRecordWriter : ChoRecordWriter
    {
        private IChoWriterRecord _callbackRecord;
        private bool _configCheckDone = false;
        private int _index = 0;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordWriter(Type recordType, ChoCSVRecordConfiguration configuration = null) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoWriterRecord>(recordType);

            Configuration.Validate();
        }

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            StreamWriter sw = writer as StreamWriter;
            ChoGuard.ArgumentNotNull(sw, "StreamWriter");

            if (records == null) yield break;

            if (!RaiseBeginWrite(sw))
                yield break;

            string recText = String.Empty;
            foreach (object record in records)
            {
                recText = String.Empty;
                _index++;
                if (record != null)
                {
                    if (predicate == null || predicate(record))
                    {
                        //Discover and load CSV columns from first record
                        if (!_configCheckDone)
                        {
                            string[] fieldNames = null;

                            if (record is ExpandoObject)
                            {
                                var x = record as IDictionary<string, Object>;
                                fieldNames = x.Keys.ToArray();
                            }
                            else
                            {
                                fieldNames = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(record.GetType()).Select(pd => pd.Name).ToArray();
                                if (fieldNames.Length == 0)
                                {
                                    fieldNames = ChoType.GetProperties(record.GetType()).Select(p => p.Name).ToArray();
                                }
                            }

                            Configuration.Validate(fieldNames);

                            WriteHeaderLine(sw);

                            _configCheckDone = true;
                        }

                        if (!RaiseBeforeRecordWrite(record, _index, ref recText))
                            yield break;

                        if (recText == null)
                            continue;
                        else if (recText.Length > 0)
                        {
                            sw.Write("{1}{0}", recText, Configuration.CSVFileHeaderConfiguration.HasHeaderRecord || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                            continue;
                        }

                        try
                        {
                            if (!(record is ExpandoObject)
                                && (Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                ChoValidator.Validate(record);

                            if (ToText(_index, record, out recText))
                            {
                                if (_index == 1)
                                    sw.Write("{1}{0}", recText, Configuration.CSVFileHeaderConfiguration.HasHeaderRecord || HasExcelSeparator ? Configuration.EOLDelimiter : "");
                                else
                                    sw.Write("{1}{0}", recText, Configuration.EOLDelimiter);

                                if (!RaiseAfterRecordWrite(record, _index, recText))
                                    yield break;
                            }
                        }
                        catch (ChoParserException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            ChoETLFramework.HandleException(ex);
                            if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                            {

                            }
                            else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                            {
                                if (!RaiseRecordWriteError(record, _index, recText, ex))
                                    throw;
                            }
                            else
                                throw;
                        }
                    }
                }

                yield return record;
            }

            RaiseEndWrite(sw);
        }

        private bool ToText(int index, object rec, out string recText)
        {
            recText = null;
            StringBuilder msg = new StringBuilder();
            object fieldValue = null;
            string fieldText = null;
            ChoCSVRecordFieldConfiguration fieldConfig = null;

            if (Configuration.ColumnCountStrict)
                CheckColumnsStrict(rec);

            bool firstColumn = true;
            foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                fieldConfig = kvp.Value;
                fieldValue = null;
                fieldText = String.Empty;
                if (Configuration.ThrowAndStopOnMissingField)
                {
                    if (rec is ExpandoObject)
                    {
                        var x = rec as IDictionary<string, Object>;
                        if (!x.Keys.Contains(fieldConfig.FieldName, Configuration.CSVFileHeaderConfiguration.StringComparer))
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                    }
                    else
                    {
                        if (!ChoType.HasProperty(rec.GetType(), kvp.Key))
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                    }
                }

                try
                {
                    if (rec is ExpandoObject)
                    {
                        var x = rec as IDictionary<string, Object>;
                        string getCultureSpecificKeyName = x.Keys.Where(i => String.Compare(i, kvp.Key, Configuration.CSVFileHeaderConfiguration.IgnoreCase, Configuration.Culture) == 0).FirstOrDefault();
                        if (!getCultureSpecificKeyName.IsNullOrWhiteSpace())
                            fieldValue = x[getCultureSpecificKeyName];
                    }
                    else
                    {
                        if (ChoType.HasProperty(rec.GetType(), kvp.Key))
                        {
                            if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                                ChoValidator.ValididateFor(rec, kvp.Key);

                            fieldValue = ChoType.GetPropertyValue(rec, kvp.Key);
                        }
                    }

                    if (!RaiseBeforeRecordFieldWrite(rec, index, kvp.Key, ref fieldValue))
                        return false;

                    if (rec is ExpandoObject)
                        fieldValue = ChoConvert.ConvertTo(fieldValue, typeof(string), Configuration.Culture);
                    else if (ChoType.HasProperty(rec.GetType(), kvp.Key))
                    {
                        if (fieldValue == null)
                        {
                            DefaultValueAttribute da = ChoTypeDescriptor.GetPropetyAttribute<DefaultValueAttribute>(rec.GetType(), kvp.Key);
                            if (da != null)
                            {
                                try
                                {
                                    fieldValue = ChoConvert.ConvertTo(da.Value, ChoType.GetMemberInfo(rec.GetType(), kvp.Key), typeof(string), rec, Configuration.Culture);
                                }
                                catch { }
                            }
                        }

                        fieldValue = ChoConvert.ConvertTo(fieldValue, ChoType.GetMemberInfo(rec.GetType(), kvp.Key), typeof(string), rec, Configuration.Culture);
                    }

                    if (fieldValue == null)
                        fieldText = String.Empty;
                    else
                        fieldText = fieldValue.ToString();

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
                    ChoETLFramework.HandleException(ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw;
                    try
                    {
                        ChoFallbackValueAttribute fbAttr = ChoTypeDescriptor.GetPropetyAttribute<ChoFallbackValueAttribute>(rec.GetType(), kvp.Key);
                        if (fbAttr != null)
                        {
                            if (rec is ExpandoObject)
                                fieldValue = ChoConvert.ConvertTo(fbAttr.Value, typeof(string), Configuration.Culture);
                            else
                                fieldValue = ChoConvert.ConvertTo(fbAttr.Value, ChoType.GetMemberInfo(rec.GetType(), kvp.Key), typeof(string), rec, Configuration.Culture);

                            if (fieldValue == null)
                                fieldText = String.Empty;
                            else
                                fieldText = fieldValue.ToString();
                        }
                        else
                            throw;
                    }
                    catch
                    {
                        if (fieldConfig.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                        {
                            continue;
                        }
                        else if (fieldConfig.ErrorMode == ChoErrorMode.ReportAndContinue)
                        {
                            if (!RaiseRecordFieldWriteError(rec, index, kvp.Key, fieldText, ex))
                                throw;
                        }
                        else
                            throw;
                    }
                }

                if (firstColumn)
                {
                    msg.Append(NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, kvp.Value.QuoteField, kvp.Value.FieldValueJustification, kvp.Value.FillChar, false));
                    firstColumn = false;
                }
                else
                    msg.AppendFormat("{0}{1}", Configuration.Delimiter, NormalizeFieldValue(kvp.Key, fieldText, kvp.Value.Size, kvp.Value.Truncate, kvp.Value.QuoteField, kvp.Value.FieldValueJustification, kvp.Value.FillChar, false));
            }

            recText = msg.ToString();
            return true;
        }

        private void CheckColumnsStrict(object rec)
        {
            if (rec is ExpandoObject)
            {
                var eoDict = rec as IDictionary<string, Object>;

                if (eoDict.Count != Configuration.RecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.RecordFieldConfigurations.Count, eoDict.Count));

                string[] missingColumns = Configuration.RecordFieldConfigurations.Select(v => v.FieldName).Except(eoDict.Keys, Configuration.CSVFileHeaderConfiguration.StringComparer).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
            else
            {
                PropertyDescriptor[] pds = ChoTypeDescriptor.GetProperties<ChoCSVRecordFieldAttribute>(rec.GetType()).ToArray();

                if (pds.Length != Configuration.RecordFieldConfigurations.Count)
                    throw new ChoParserException("Incorrect number of fields found in record object. Expected [{0}] fields. Found [{1}] fields.".FormatString(Configuration.RecordFieldConfigurations.Count, pds.Length));

                string[] missingColumns = Configuration.RecordFieldConfigurations.Select(v => v.FieldName).Except(pds.Select(pd => pd.Name), Configuration.CSVFileHeaderConfiguration.StringComparer).ToArray();
                if (missingColumns.Length > 0)
                    throw new ChoParserException("[{0}] fields are not found in record object.".FormatString(String.Join(",", missingColumns)));
            }
        }

        private void WriteHeaderLine(StreamWriter sw)
        {
            if (HasExcelSeparator)
                sw.Write("sep={0}".FormatString(Configuration.Delimiter));

            if (Configuration.CSVFileHeaderConfiguration.HasHeaderRecord)
            {
                string header = ToHeaderText();
                if (header.IsNullOrWhiteSpace())
                    return;

                sw.Write("{1}{0}", header, HasExcelSeparator ? Configuration.EOLDelimiter : "");
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
            foreach (var member in Configuration.RecordFieldConfigurations)
            {
                value = NormalizeFieldValue(member.Name, member.FieldName, member.Size, Configuration.CSVFileHeaderConfiguration.Truncate,
                        member.QuoteField, Configuration.CSVFileHeaderConfiguration.Justification,
                        Configuration.CSVFileHeaderConfiguration.FillChar, true);

                if (msg.Length == 0)
                    msg.Append(value);
                else
                    msg.AppendFormat("{0}{1}", delimiter, value);
            }

            return msg.ToString();
        }

        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool? quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false)
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
                    if (fieldValue.Contains(Configuration.Delimiter))
                    {
                        if (isHeader)
                            throw new ChoParserException("Field header {0} value contains delimiter character.".FormatString(fieldName));
                        else
                            throw new ChoParserException("Field {0} value contains delimiter character.".FormatString(fieldName));
                    }

                    if (fieldValue.Contains(Configuration.EOLDelimiter))
                    {
                        if (isHeader)
                            throw new ChoParserException("Field header {0} value contains EOL delimiter character.".FormatString(fieldName));
                        else
                            throw new ChoParserException("Field {0} value contains EOL delimiter character.".FormatString(fieldName));
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
                        fieldValue = fieldValue.Substring(0, size.Value);
                    else
                    {
                        if (isHeader)
                            throw new ApplicationException("Field header value length overflowed for '{0}' member [Expected: {1}, Actual: {2}].".FormatString(fieldName, size, fieldValue.Length));
                        else
                            throw new ApplicationException("Field value length overflowed for '{0}' member [Expected: {1}, Actual: {2}].".FormatString(fieldName, size, fieldValue.Length));
                    }
                }
            }

            return fieldValue;
        }

        private bool RaiseBeginWrite(object state)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginWrite(state), true);
        }

        private void RaiseEndWrite(object state)
        {
            if (_callbackRecord == null) return;
            ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndWrite(state));
        }

        private bool RaiseBeforeRecordWrite(object target, int index, ref string state)
        {
            if (_callbackRecord == null) return true;
            object inState = state;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordWrite(target, index, ref inState), true);
            if (retValue)
                state = inState == null ? null : inState.ToString();

            return retValue;
        }

        private bool RaiseAfterRecordWrite(object target, int index, string state)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordWrite(target, index, state), true);
        }

        private bool RaiseRecordWriteError(object target, int index, string state, Exception ex)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordWriteError(target, index, state, ex), false);
        }

        private bool RaiseBeforeRecordFieldWrite(object target, int index, string propName, ref object value)
        {
            if (_callbackRecord == null) return true;
            object state = value;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordFieldWrite(target, index, propName, ref state), true);

            if (retValue)
                value = state;

            return retValue;
        }

        private bool RaiseAfterRecordFieldWrite(object target, int index, string propName, object value)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordFieldWrite(target, index, propName, value), true);
        }

        private bool RaiseRecordFieldWriteError(object target, int index, string propName, object value, Exception ex)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldWriteError(target, index, propName, value, ex), false);
        }
    }
}
