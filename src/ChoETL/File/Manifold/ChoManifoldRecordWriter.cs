using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
        private long _index = 0;
        internal ChoWriter Writer = null;
        private TraceSwitch _offSwitch = new System.Diagnostics.TraceSwitch("t", "t", "Off");

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldRecordWriter(ChoManifoldRecordConfiguration configuration) : base(typeof(object))
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;
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
            Type recType;

            try
            {
                foreach (object record in records)
                {
                    _index++;
                    recType = record.GetType();
                    if (record is IChoETLNameableObject)
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(((IChoETLNameableObject)record).Name));
                    else
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(_index));

                    recText = String.Empty;
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
                                    recText = ChoCSVWriter.ToText(record, config as ChoCSVRecordConfiguration, Configuration.Encoding, Configuration.BufferSize, _offSwitch);
                                }
                                else if (config.GetType() == typeof(ChoFixedLengthRecordConfiguration))
                                {
                                    recText = ChoFixedLengthWriter.ToText(record, config as ChoFixedLengthRecordConfiguration, Configuration.Encoding, Configuration.BufferSize, _offSwitch);
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
                            //catch (ChoParserException pEx)
                            //{
                            //    throw new ChoParserException($"Failed to write line for '{recType}' object.", pEx);
                            //}
                            catch (Exception ex)
                            {
                                ChoETLFramework.HandleException(ref ex);
                                if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                                {
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                }
                                else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                                {
                                    if (!RaiseRecordWriteError(record, _index, recText, ex))
                                        throw new ChoWriterException($"Failed to write line for '{recType}' object.", ex);
                                    else
                                    {
                                        //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                    }
                                }
                                else
                                    throw new ChoWriterException($"Failed to write line for '{recType}' object.", ex);
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

        private ChoFileRecordConfiguration GetConfiguration(Type recordType)
        {
            ChoFileRecordConfiguration config = Configuration[recordType];
            if (config == null)
            {
                ChoFileRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType);
                if (recObjAttr == null || recObjAttr is ChoCSVRecordObjectAttribute)
                {
                    Configuration[recordType] = new ChoCSVRecordConfiguration(recordType);
                    RaiseAfterRecordConfigurationConstruct(recordType, Configuration[recordType]);
                }
                else if (recObjAttr is ChoFixedLengthRecordObjectAttribute)
                {
                    Configuration[recordType] = new ChoFixedLengthRecordConfiguration(recordType);
                    RaiseAfterRecordConfigurationConstruct(recordType, Configuration[recordType]);
                }
            }

            return Configuration[recordType];
        }

        public void RaiseAfterRecordConfigurationConstruct(Type recordType, ChoRecordConfiguration config)
        {
            if (Writer is IChoManifoldWriter)
                ((IChoManifoldWriter)Writer).RaiseAfterRecordConfigurationConstruct(recordType, config);
        }

        private bool RaiseBeginWrite(object state)
        {
            if (Configuration.NotifyFileWriteObject != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyFileWriteObject.BeginWrite(state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeginWrite(state), true);
            }
            return true;
        }

        private void RaiseEndWrite(object state)
        {
            if (Configuration.NotifyFileWriteObject != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Configuration.NotifyFileWriteObject.EndWrite(state));
            }
            else if (Writer != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseEndWrite(state));
            }
        }

        private bool RaiseBeforeRecordWrite(object target, long index, ref string state)
        {
            if (Configuration.NotifyRecordWriteObject != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.BeforeRecordWrite(target, index, ref inState), true);
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
            if (Configuration.NotifyRecordWriteObject != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.AfterRecordWrite(target, index, state), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordWrite(target, index, state), true);
            }
            return true;
        }

        private bool RaiseRecordWriteError(object target, long index, string state, Exception ex)
        {
            if (Configuration.NotifyRecordWriteObject != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordWriteObject.RecordWriteError(target, index, state, ex), false);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordWriteError(target, index, state, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (Configuration.NotifyRecordWriteObject != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordFieldWriteObject.BeforeRecordFieldWrite(target, index, propName, ref state), true);

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
            if (Configuration.NotifyRecordWriteObject != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordFieldWriteObject.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (Writer != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = true;
            object state = value;
            if (target is IChoNotifyRecordFieldWrite)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).RecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (Writer != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (Configuration.NotifyRecordWriteObject != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordFieldWriteObject.RecordFieldWriteError(target, index, propName, ref state, ex), false);

                if (retValue)
                    value = state;
            }
            return retValue;
        }
    }
}
