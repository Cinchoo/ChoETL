using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoManifoldRecordWriter : ChoRecordWriter
    {
        private bool _configCheckDone = false;
        private int _index = 0;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldRecordWriter(Type recordType, ChoManifoldRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;
        }

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            StreamWriter sw = writer as StreamWriter;
            ChoGuard.ArgumentNotNull(sw, "StreamWriter");

            if (records == null) yield break;

            if (!RaiseBeginWrite(sw))
                yield break;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            string recText = String.Empty;
            Type recType;

            try
            {
                int index = 0;
                foreach (object record in records)
                {
                    recType = record.GetType();
                    if (record is IChoETLNameableObject)
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(((IChoETLNameableObject)record).Name));
                    else
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(++index));

                    recText = String.Empty;
                    _index++;
                    if (record != null)
                    {
                        if (predicate == null || predicate(record))
                        {
                            //Discover and load Manifold columns from first record
                            if (!_configCheckDone)
                            {
                                //string[] fieldNames = null;

                                //if (record is ExpandoObject)
                                //{
                                //    var dict = record as IDictionary<string, Object>;
                                //    fieldNames = dict.Keys.ToArray();
                                //}
                                //else
                                //{
                                //    fieldNames = ChoTypeDescriptor.GetProperties<ChoManifoldRecordFieldAttribute>(record.GetType()).Select(pd => pd.Name).ToArray();
                                //    if (fieldNames.Length == 0)
                                //    {
                                //        fieldNames = ChoType.GetProperties(record.GetType()).Select(p => p.Name).ToArray();
                                //    }
                                //}

                                //Configuration.Validate(fieldNames);

                                //WriteHeaderLine(sw);

                                _configCheckDone = true;
                            }

                            if (!RaiseBeforeRecordWrite(record, _index, ref recText))
                                yield break;

                            if (recText == null)
                                continue;
                            else if (recText.Length > 0)
                            {
                                sw.Write("{1}{0}", recText, Configuration.FileHeaderConfiguration.HasHeaderRecord ? Configuration.EOLDelimiter : "");
                                continue;
                            }

                            try
                            {
                                var config = GetConfiguration(record.GetType());
                                if (config == null)
                                    throw new ChoParserException("No writer found to write record.");

                                if (config.GetType() == typeof(ChoCSVRecordConfiguration))
                                {
                                    recText = ChoCSVWriter.ToText(record, config as ChoCSVRecordConfiguration, Configuration.Encoding, Configuration.BufferSize);
                                }
                                else if (config.GetType() == typeof(ChoFixedLengthRecordConfiguration))
                                {
                                    recText = ChoFixedLengthWriter.ToText(record, config as ChoFixedLengthRecordConfiguration, Configuration.Encoding, Configuration.BufferSize);
                                }
                                else
                                    throw new ChoParserException("Unsupported record found to write.");

                                if (recText != null)
                                {
                                    if (_index == 1)
                                    {
                                        sw.Write("{1}{0}", recText, Configuration.FileHeaderConfiguration.HasHeaderRecord ? Configuration.EOLDelimiter : "");
                                    }
                                    else
                                        sw.Write("{1}{0}", recText, Configuration.EOLDelimiter);

                                    if (!RaiseAfterRecordWrite(record, _index, recText))
                                        yield break;
                                }
                            }
                            catch (ChoParserException pEx)
                            {
                                throw new ChoParserException($"Failed to write line for '{recType}' object.", pEx);
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
                                        throw new ChoParserException($"Failed to write line for '{recType}' object.", ex);
                                }
                                else
                                    throw new ChoParserException($"Failed to write line for '{recType}' object.", ex);
                            }
                        }
                    }

                    yield return record;
                }
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
            }

            RaiseEndWrite(sw);
        }

        private ChoFileRecordConfiguration GetConfiguration(Type recordType)
        {
            ChoFileRecordConfiguration config = Configuration[recordType];
            if (config == null)
            {
                ChoFileRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType);
                if (recObjAttr == null || recObjAttr is ChoCSVRecordObjectAttribute)
                    Configuration[recordType] = new ChoCSVRecordConfiguration(recordType);
                else if (recObjAttr is ChoFixedLengthRecordObjectAttribute)
                    Configuration[recordType] = new ChoFixedLengthRecordConfiguration(recordType);
            }

            return Configuration[recordType];
        }

        private bool RaiseBeginWrite(object state)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.BeginWrite(state), true);
        }

        private void RaiseEndWrite(object state)
        {
            if (Configuration.NotifyRecordWriteObject == null) return;
            ChoActionEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.EndWrite(state));
        }

        private bool RaiseBeforeRecordWrite(object target, int index, ref string state)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            object inState = state;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.BeforeRecordWrite(target, index, ref inState), true);
            if (retValue)
                state = inState == null ? null : inState.ToString();

            return retValue;
        }

        private bool RaiseAfterRecordWrite(object target, int index, string state)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.AfterRecordWrite(target, index, state), true);
        }

        private bool RaiseRecordWriteError(object target, int index, string state, Exception ex)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.RecordWriteError(target, index, state, ex), false);
        }

        private bool RaiseBeforeRecordFieldWrite(object target, int index, string propName, ref object value)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            object state = value;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.BeforeRecordFieldWrite(target, index, propName, ref state), true);

            if (retValue)
                value = state;

            return retValue;
        }

        private bool RaiseAfterRecordFieldWrite(object target, int index, string propName, object value)
        {
            if (Configuration.NotifyRecordWriteObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.AfterRecordFieldWrite(target, index, propName, value), true);
        }

        private bool RaiseRecordFieldWriteError(object target, int index, string propName, object value, Exception ex)
        {
            if (Configuration.NotifyRecordWriteObject == null) return false;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.RecordFieldWriteError(target, index, propName, value, ex), false);
        }
    }
}
