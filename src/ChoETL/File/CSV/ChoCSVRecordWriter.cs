using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoCSVRecordWriter : ChoRecordWriter
    {
        private IChoWriterRecord _record;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVRecordWriter(Type recordType, ChoCSVRecordConfiguration configuration = null) : base(recordType)
        {
            Configuration = configuration;
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(recordType);

            if (typeof(IChoWriterRecord).IsAssignableFrom(recordType))
                _record = Activator.CreateInstance(recordType) as IChoWriterRecord;

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
                        if (!RaiseBeforeRecordWrite(record))
                            yield break;

                        try
                        {
                            if (!Write(sw))
                                yield break;

                            if (!RaiseAfterRecordWrite(record))
                                yield break;
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

        private bool Write(StreamWriter sw)
        {
            sw.Write("{1}{0}", ToText(), sw.BaseStream.Position == 0 ? "" : Configuration.EOLDelimiter);
            return true;
        }

        private string ToText()
        {
            StringBuilder msg = new StringBuilder();
            string fieldValue = null;
            bool firstColumn = true;
            foreach (var member in Configuration.RecordFieldConfigurations)
            {
                //fieldValue = null;
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

        private void WriteHeaderLine(StreamWriter sw)
        {
            if (Configuration.CSVFileHeaderConfiguration.HasHeaderRecord && sw.BaseStream.Position == 0)
            {
                string header = ToHeaderText();
                if (header.IsNullOrWhiteSpace())
                    return;

                sw.Write("{1}{0}", header, sw.BaseStream.Position == 0 ? "" : Configuration.EOLDelimiter);
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
            if (_record == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _record.BeginLoad(null), true);
        }

        private bool RaiseFormattedFieldValue(string fieldName, ref string fieldValue)
        {
            if (_record == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _record.BeginLoad(null), true);
        }

        private bool RaiseBeginWrite(object state)
        {
            if (_record == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => _record.BeginLoad(state), true);
        }

        private void RaiseEndWrite(object state)
        {
            if (_record == null) return;
            ChoActionEx.RunWithIgnoreError(() => _record.EndLoad(state));
        }

        private bool RaiseBeforeRecordWrite(object record)
        {
            if (_record == null) return true;
            bool retValue = false; // ChoFuncEx.RunWithIgnoreError(() => _record.BeforeRecordWrite(index, ref state), true);

            return retValue;
        }

        private bool RaiseAfterRecordWrite(object record)
        {
            if (_record == null) return true;
            return false; // ChoFuncEx.RunWithIgnoreError(() => _record.AfterRecordWrite(pair.Item1, pair.Item2), true);
        }

        private bool RaiseRecordWriteError(object record, Exception ex)
        {
            if (_record == null) return true;
            return false; // ChoFuncEx.RunWithIgnoreError(() => _record.RecordWriteError(pair.Item1, pair.Item2, ex), false);
        }

    }
}
