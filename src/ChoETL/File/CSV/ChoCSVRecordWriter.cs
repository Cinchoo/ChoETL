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

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordWriter(Type recordType, ChoCSVRecordConfiguration configuration = null) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoSurrogateObjectCache.CreateSurrogateObject<IChoWriterRecord>(recordType);

            Configuration.Validate();
        }

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            StreamWriter sw = writer as StreamWriter;
            ChoGuard.ArgumentNotNull(sw, "StreamWriter");

            if (records == null) yield break;

            if (!RaiseBeginWrite(sw))
                yield break;

            WriteHeaderLine(sw);

            foreach (object record in records)
            {
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
                            }

                            Configuration.Validate(fieldNames);
                            _configCheckDone = true;
                        }

                        if (!RaiseBeforeRecordWrite(record))
                            yield break;

                        try
                        {
                            sw.Write("{1}{0}", ToText(record), Configuration.EOLDelimiter);

                            if (!RaiseAfterRecordWrite(record))
                                yield break;
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
                                if (!RaiseRecordWriteError(record, ex))
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

        private string ToText(object rec)
        {
            StringBuilder msg = new StringBuilder();
            string fieldValue = null;
            ChoCSVRecordFieldConfiguration fieldConfig = null;

            //if (Configuration.ColumnCountStrict)
            //    CheckColumnsStrict(rec);

            foreach (KeyValuePair<string, ChoCSVRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                fieldConfig = kvp.Value;
                fieldValue = null;

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

                //if (rec is ExpandoObject)
                //{
                //    var x = rec as IDictionary<string, Object>;
                //    string key = x.Keys.Where(k => Configuration.CSVFileHeaderConfiguration.StringComparer.Compare(fieldConfig.FieldName, k) == 0).FirstOrDefault();

                //    if (!x.Keys.Select(.Contains(fieldConfig.FieldName, Configuration.CSVFileHeaderConfiguration.StringComparer))
                //        throw new ChoParserException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                //}
                //else
                //{
                //    if (!ChoType.HasProperty(rec.GetType(), kvp.Key))
                //        throw new ChoParserException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                //}

                //fieldValue = CleanFieldValue(fieldConfig, fieldValue as string);

                //if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                //    continue;

                //object value = ERPSType.GetMemberValue(RecordObject, member.Item1);

                //if (value != null)
                //    value = ERPSConvert.ConvertTo(value, member.Item1, typeof(string), null);

                //if (value == null)
                //    value = String.Empty;

                //fieldValue = HasFieldsEnclosedInQuotes ? "\"{0}\"".FormatString(value.ToString()) : value.ToString();

                //if (firstColumn)
                //{
                //    msg.Append(NormalizeFieldValue(member.Item1.FullName(), member.Item2 as ERPSFileRecordFieldAttribute, fieldValue, Delimiter));
                //    firstColumn = false;
                //}
                //else
                //    msg.AppendFormat("{0}{1}", Delimiter, NormalizeFieldValue(member.Item1.FullName(), member.Item2 as ERPSFileRecordFieldAttribute, fieldValue, Delimiter));
            }

            return msg.ToString();
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
            if (Configuration.CSVFileHeaderConfiguration.HasHeaderRecord /* && sw.BaseStream.Position == 0*/)
            {
                string header = ToHeaderText();
                if (header.IsNullOrWhiteSpace())
                    return;

                sw.Write("{1}{0}", header, Configuration.EOLDelimiter);
                //sw.Write("{1}{0}", header, sw.BaseStream.Position == 0 ? "" : Configuration.EOLDelimiter);
            }
        }

        private string ToHeaderText()
        {
            string delimiter = Configuration.Delimiter;
            StringBuilder msg = new StringBuilder();
            string value;
            foreach (var member in Configuration.RecordFieldConfigurations)
            {
                value = NormalizeFieldValue(member.Name, member.FieldName, member.Size, member.Truncate,
                        member.QuoteField.Value, Configuration.CSVFileHeaderConfiguration.Justification,
                        Configuration.CSVFileHeaderConfiguration.FillChar, true);

                if (msg.Length == 0)
                    msg.Append(value);
                else
                    msg.AppendFormat("{0}{1}", delimiter, value);
            }

            return msg.ToString();
        }

        private string NormalizeFieldValue(string fieldName, string fieldValue, int? size, bool truncate, bool quoteField,
            ChoFieldValueJustification fieldValueJustification, char fillChar, bool isHeader = false)
        {
            string lFieldValue = fieldValue;
            bool retValue = false;
            if (isHeader)
                retValue = RaiseFormattedHeaderValue(fieldName, ref lFieldValue);
            else
                retValue = RaiseFormattedFieldValue(fieldName, ref lFieldValue);

            if (retValue)
                return lFieldValue;

            if (fieldValue.IsNull())
                fieldValue = String.Empty;

            if (!quoteField)
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
                    if (fieldValueJustification == ChoFieldValueJustification.Right)
                        fieldValue = fieldValue.PadLeft(size.Value, fillChar);
                    else if (fieldValueJustification == ChoFieldValueJustification.Left)
                        fieldValue = fieldValue.PadRight(size.Value, fillChar);
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

            //if (fieldValue.Contains(Environment.NewLine))
            //    fieldValue = fieldValue.Replace(Environment.NewLine, "");

            return fieldValue;
        }

        private bool RaiseFormattedHeaderValue(string fieldName, ref string fieldValue)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(null), true);
        }

        private bool RaiseFormattedFieldValue(string fieldName, ref string fieldValue)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(null), true);
        }

        private bool RaiseBeginWrite(object state)
        {
            if (_callbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(state), true);
        }

        private void RaiseEndWrite(object state)
        {
            if (_callbackRecord == null) return;
            ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndLoad(state));
        }

        private bool RaiseBeforeRecordWrite(object target)
        {
            if (_callbackRecord == null) return true;
            bool retValue = false; // ChoFuncEx.RunWithIgnoreError(() => _record.BeforeRecordWrite(index, ref state), true);

            return retValue;
        }

        private bool RaiseAfterRecordWrite(object target)
        {
            if (_callbackRecord == null) return true;
            return false; // ChoFuncEx.RunWithIgnoreError(() => _record.AfterRecordWrite(pair.Item1, pair.Item2), true);
        }

        private bool RaiseRecordWriteError(object target, Exception ex)
        {
            if (_callbackRecord == null) return true;
            return false; // ChoFuncEx.RunWithIgnoreError(() => _record.RecordWriteError(pair.Item1, pair.Item2, ex), false);
        }

    }
}
