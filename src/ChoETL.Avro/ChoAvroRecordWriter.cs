using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ChoETL
{
    internal class ChoAvroRecordWriter : ChoRecordWriter
    {
        private IChoNotifyFileHeaderArrange _callbackFileHeaderArrange;
        private IChoNotifyFileWrite _callbackFileWrite;
        private IChoNotifyRecordWrite _callbackRecordWrite;
        private IChoNotifyRecordFieldWrite _callbackRecordFieldWrite;
        private bool _configCheckDone = false;
        private long _index = 0;
        internal ChoWriter Writer = null;
        internal Type ElementType = null;
        private Lazy<List<object>> _recBuffer = null;
        private Lazy<bool> BeginWrite = null;
        private object _sw = null;
        private object _avroSerializer = null;
        private object _avroWriter = null;
        private List<dynamic> _records = new List<dynamic>();

        public ChoAvroRecordConfiguration Configuration
        {
            get;
            private set;
        }
        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoAvroRecordWriter(Type recordType, ChoAvroRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackFileHeaderArrange = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileHeaderArrange>(recordType);
            _callbackRecordWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(recordType);
            _callbackFileWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileWrite>(recordType);
            _callbackRecordFieldWrite = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldWrite>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            _recBuffer = new Lazy<List<object>>(() =>
            {
                if (Writer != null)
                {
                    var b = Writer.Context.ContainsKey("RecBuffer") ? Writer.Context.RecBuffer : null;
                    if (b == null)
                        Writer.Context.RecBuffer = new List<object>();

                    return Writer.Context.RecBuffer;
                }
                else
                    return new List<object>();
            }, true);

            BeginWrite = new Lazy<bool>(() =>
            {
                if (_sw != null)
                    return RaiseBeginWrite(_sw);
                if (_avroWriter != null)
                    return RaiseBeginWrite(_avroWriter);

                return false;
            });
            //Configuration.Validate();
        }

        internal void Dispose<T>()
        {
            if (_sw != null)
                RaiseEndWrite(_sw);
            else if (_avroWriter != null)
                RaiseEndWrite(_avroWriter);

            if (Configuration.UseAvroSerializer)
            {
                if (IsDynamicType)
                {
                    IAvroSerializer<Dictionary<string, object>> avroSerializer = _avroSerializer as IAvroSerializer<Dictionary<string, object>>;
                }
                else
                {
                    IAvroSerializer<T> avroSerializer = _avroSerializer as IAvroSerializer<T>;
                }
            }
            else
            {
                IDisposable avroWriter = _avroWriter as IDisposable;
                if (avroWriter != null)
                    avroWriter.Dispose();
            }
        }

        private bool IsDynamicType
        {
            get
            {
                return RecordType.IsDynamicType();
            }
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<object> GetRecords(IEnumerator<object> records)
        {
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return rec;


            while (records.MoveNext())
                yield return records.Current;
        }

        private object GetFirstNotNullRecord(IEnumerator<object> recEnum)
        {
            if (Writer != null && !Object.ReferenceEquals(Writer.Context.FirstNotNullRecord, null))
                return Writer.Context.FirstNotNullRecord;

            while (recEnum.MoveNext())
            {
                _recBuffer.Value.Add(recEnum.Current);
                if (recEnum.Current != null)
                {
                    if (Writer != null)
                    {
                        Writer.Context.FirstNotNullRecord = recEnum.Current;
                        return Writer.Context.FirstNotNullRecord;
                    }
                    else
                        return recEnum.Current;
                }
            }
            return null;
        }

        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            throw new NotImplementedException();
        }

        private void DiscoverKnownTypes(object rec)
        {
            if (rec == null || !(rec is IDictionary<string, object>))
                return;

            IDictionary<string, object> dict = rec as IDictionary<string, object>;

            if (!Configuration.KnownTypes.Contains(typeof(string)))
                Configuration.KnownTypes.Add(typeof(string));

            foreach (var value in dict.Values)
            {
                if (value == null) continue;

                if (Configuration.KnownTypes.Contains(value.GetType()))
                    continue;

                Configuration.KnownTypes.Add(value.GetType());
            }

        }

        private object CreateAvroSerializer<T>()
        {
            if (_avroSerializer == null)
            {
                if (_avroWriter != null && _avroWriter is ChoAvroWriter<T>)
                    _avroSerializer = ((ChoAvroWriter<T>)_avroWriter).AvroSerializer;

                if (_avroSerializer == null)
                    _avroSerializer = AvroSerializer.Create<T>(Configuration.AvroSerializerSettings);
            }

            return _avroSerializer;
        }

        private IAvroWriter<T> CreateAvroWriter<T>(StreamWriter sw)
        {
            if (Configuration.Codec != null)
                return AvroContainer.CreateWriter<T>(sw.BaseStream, Configuration.LeaveOpen, Configuration.AvroSerializerSettings, Configuration.Codec);
            else
                return AvroContainer.CreateWriter<T>(sw.BaseStream, Configuration.LeaveOpen, Configuration.AvroSerializerSettings, Codec.Null);
        }

        public IEnumerable<T> WriteTo<T>(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            Configuration.ResetStatesInternal();
            Configuration.Init();

            if (records == null) yield break;

            var recEnum = records.GetEnumerator();

            object notNullRecord = GetFirstNotNullRecord(recEnum);
            if (notNullRecord == null)
                yield break;
            DiscoverKnownTypes(notNullRecord);

            StreamWriter sw = null;
            if (writer is Lazy<StreamWriter>)
            {
                var lsw = writer as Lazy<StreamWriter>;
                ChoGuard.ArgumentNotNull(lsw, "StreamWriter");

                _sw = sw = lsw.Value;
            }
            else if (writer is StreamWriter)
            {
                _sw = sw = writer as StreamWriter;
            }

            if (_sw != null)
            {
                if (!Configuration.UseAvroSerializer)
                {
                    if (_avroWriter == null)
                    {
                        _avroWriter = new SequentialWriter<T>(CreateAvroWriter<T>(sw), Configuration.SyncNumberOfObjects);
                    }
                }
                else if (_avroSerializer == null)
                {
                    _avroSerializer = CreateAvroSerializer<T>();
                }
            }
            else
            {
                _avroWriter = writer as IAvroWriter<T>;
                if (_avroWriter == null)
                    throw new ChoParserException("Missing valid writer object passed.");
            }

            if (!BeginWrite.Value)
                yield break;

            CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            object recOutput = null;
            bool abortRequested = false;
            try
            {
                foreach (object record in GetRecords(recEnum))
                {
                    _index++;

                    if (TraceSwitch.TraceVerbose)
                    {
                        if (record is IChoETLNameableObject)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(((IChoETLNameableObject)record).Name));
                        else
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Writing [{0}] object...".FormatString(_index));
                    }
                    recOutput = record;
                    if (record != null)
                    {
                        if (predicate == null || predicate(record))
                        {
                            if (!RaiseBeforeRecordWrite(record, _index, ref recOutput))
                                yield break;

                            if (recOutput == null)
                                continue;
                            else if (recOutput is IDictionary<string, object>)
                            {

                            }
                            else if (!(recOutput is T))
                                continue;

                            try
                            {
                                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                    record.DoObjectLevelValidation(Configuration, Configuration.AvroRecordFieldConfigurations);

                                if (recOutput is IDictionary<string, object>)
                                    recOutput = new Dictionary<string, object>(recOutput as IDictionary<string, object>);

                                if (_sw != null)
                                {
                                    if (Configuration.UseAvroSerializer)
                                    {
                                        IAvroSerializer<T> avroSerializer = _avroSerializer as IAvroSerializer<T>;
                                        avroSerializer.Serialize(sw.BaseStream, (T)(recOutput as object));
                                    }
                                    else
                                    {
                                        SequentialWriter<T> avroWriter = _avroWriter as SequentialWriter<T>;
                                        avroWriter.Write((T)(recOutput as object));
                                    }
                                }
                                else
                                {
                                    SequentialWriter<T> avroWriter = _avroWriter as SequentialWriter<T>;
                                    avroWriter.Write((T)(recOutput as object));
                                }

                                if (!RaiseAfterRecordWrite(record, _index, recOutput))
                                    yield break;
                            }
                            catch (Exception ex)
                            {
                                ChoETLFramework.HandleException(ref ex);
                                if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                                {
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                }
                                else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                                {
                                    if (!RaiseRecordWriteError(record, _index, recOutput, ex))
                                        throw;
                                    else
                                    {
                                        //ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                                    }
                                }
                                else
                                    throw;
                            }
                        }
                    }

                    yield return (T)recOutput;
                    recOutput = null;

                    if (Configuration.NotifyAfter > 0 && _index % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsWritten(_index))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                            abortRequested = true;
                            yield break;
                        }
                    }
                }

                if (!abortRequested && recOutput != null)
                    RaisedRowsWritten(_index, true);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = prevCultureInfo;
            }
        }

        #region Event Raisers

        private bool RaiseBeginWrite(object state)
        {
            if (Writer != null && Writer.HasBeginWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeginWrite(state), true);
            }
            else if (_callbackFileWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFileWrite.BeginWrite(state), true);
            }
            return true;
        }

        private void RaiseEndWrite(object state)
        {
            if (Writer != null && Writer.HasEndWriteSubscribed)
            {
                ChoActionEx.RunWithIgnoreError(() => Writer.RaiseEndWrite(state));
            }
            else if (_callbackFileWrite != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackFileWrite.EndWrite(state));
            }
        }

        private bool RaiseBeforeRecordWrite(object target, long index, ref object state)
        {
            if (Writer != null && Writer.HasBeforeRecordWriteSubscribed)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            else if (_callbackRecordWrite != null)
            {
                object inState = state;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.BeforeRecordWrite(target, index, ref inState), true);
                if (retValue)
                    state = inState == null ? null : inState.ToString();
                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordWrite(object target, long index, object state)
        {
            if (Writer != null && Writer.HasAfterRecordWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordWrite(target, index, state), true);
            }
            else if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.AfterRecordWrite(target, index, state), true);
            }
            return true;
        }

        private bool RaiseRecordWriteError(object target, long index, object state, Exception ex)
        {
            if (Writer != null && Writer.HasRecordWriteErrorSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordWriteError(target, index, state, ex), false);
            }
            else if (_callbackRecordWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordWrite.RecordWriteError(target, index, state, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            if (Writer != null && Writer.HasBeforeRecordFieldWriteSubscribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseBeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            else if (_callbackRecordFieldWrite != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.BeforeRecordFieldWrite(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            if (Writer != null && Writer.HasAfterRecordFieldWriteSubscribed)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseAfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).AfterRecordFieldWrite(target, index, propName, value), true);
            }
            else if (_callbackRecordFieldWrite != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.AfterRecordFieldWrite(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
        {
            bool retValue = true;
            object state = value;

            if (Writer != null && Writer.HasRecordFieldWriteErrorSubscribed)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => Writer.RaiseRecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (target is IChoNotifyRecordFieldWrite)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldWrite)target).RecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            else if (_callbackRecordFieldWrite != null)
            {
                retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordFieldWrite.RecordFieldWriteError(target, index, propName, ref state, ex), true);

                if (retValue)
                    value = state;
            }
            return retValue;
        }

        #endregion Event Raisers
    }
}
