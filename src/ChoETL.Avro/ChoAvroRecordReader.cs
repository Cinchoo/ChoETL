using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoAvroRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        internal ChoReader Reader = null;
        private object _sr = null;
        private object _avroSerializer = null;
        private object _avroReader = null;

        public ChoAvroRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoAvroRecordReader(Type recordType, ChoAvroRecordConfiguration configuration) : base(recordType, false)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        private bool IsDynamicType
        {
            get
            {
                return RecordType.IsDynamicType();
            }
        }

        private object CreateAvroSerializer<T>()
        {
            if (_avroSerializer == null)
            {
                if (_avroReader != null && _avroReader is ChoAvroReader<T>)
                    _avroSerializer = ((ChoAvroReader<T>)_avroReader).AvroSerializer;
                
                if (_avroSerializer == null)
                    _avroSerializer = AvroSerializer.Create<T>(Configuration.AvroSerializerSettings);
            }

            return _avroSerializer as IAvroSerializer<T>;
        }

        private object CreateAvroReader<T>(StreamReader sr)
        {
            if (IsDynamicType)
            {
                if (Configuration.RecordSchema.IsNullOrWhiteSpace())
                    return AvroContainer.CreateGenericReader(sr.BaseStream, Configuration.LeaveOpen, Configuration.CodecFactory != null ? Configuration.CodecFactory : new CodecFactory());
                else
                    return AvroContainer.CreateGenericReader(Configuration.RecordSchema, sr.BaseStream, Configuration.LeaveOpen, new CodecFactory());
            }
            else
            {
                if (Configuration.CodecFactory != null)
                    return AvroContainer.CreateReader<T>(sr.BaseStream, Configuration.LeaveOpen, Configuration.AvroSerializerSettings, Configuration.CodecFactory);
                else
                    return AvroContainer.CreateReader<T>(sr.BaseStream, Configuration.LeaveOpen, Configuration.AvroSerializerSettings, new CodecFactory());
            }
        }

        public IEnumerable<object> AsEnumerable<T>(object source, Func<object, bool?> filterFunc = null)
        {
            Configuration.ResetStatesInternal();
            if (source == null)
                yield break;

            Configuration.Init();

            StreamReader sr = null;
            if (source is Lazy<StreamReader>)
            {
                var lsr = source as Lazy<StreamReader>;
                ChoGuard.ArgumentNotNull(lsr, "StreamReader");

                _sr = sr = lsr.Value;

                if (!Configuration.UseAvroSerializer)
                {
                    if (_avroReader == null)
                    {
                        _avroReader = CreateAvroReader<T>(sr);
                    }
                }
                else if (_avroSerializer == null)
                {
                    _avroSerializer = CreateAvroSerializer<T>();
                }

                InitializeRecordConfiguration(Configuration);

                if (!RaiseBeginLoad(sr))
                    yield break;

                if (_avroReader != null)
                {
                    foreach (var item in AsEnumerable(ReadObjects<T>(_avroReader).OfType<object>(), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }
                else
                {
                    foreach (var item in AsEnumerable(ReadObjects<T>(sr, _avroSerializer as IAvroSerializer<T>).OfType<object>(), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }

                RaiseEndLoad(sr);
            }
            else
            {
                _avroReader = source as IAvroReader<T>;
                if (_avroReader == null)
                    throw new ChoParserException("Missing valid reader object passed.");

                InitializeRecordConfiguration(Configuration);

                if (!RaiseBeginLoad(_avroReader))
                    yield break;

                foreach (var item in AsEnumerable(ReadObjects<T>(_avroReader as IAvroReader<T>).OfType<object>(), TraceSwitch, filterFunc))
                {
                    yield return item;
                }

                RaiseEndLoad(_avroReader);
            }
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<object> ReadObjects<T>(StreamReader sr, IAvroSerializer<T> avroSerializer, Func<object, bool?> filterFunc = null)
        {
            while (true)
            {
                object obj = null;
                try
                {
                    obj = avroSerializer.Deserialize(sr.BaseStream);

                    if (IsDynamicType)
                        obj  = new ChoDynamicObject(obj as Dictionary<string, object>);
                }
                catch (System.OverflowException)
                {
                    break;
                }
                catch (SerializationException sEx)
                {
                    if (sEx.Message.StartsWith("Invalid integer value in the input stream"))
                        break;
                    throw;
                }

                yield return obj;
            }
        }

        private IEnumerable<object> ReadObjects<T>(object avroReader, Func<object, bool?> filterFunc = null)
        {
            if (avroReader is IAvroReader<T>)
            {
                using (var streamReader = new SequentialReader<T>(avroReader as IAvroReader<T>))
                {
                    foreach (var item in streamReader.Objects)
                        yield return item;
                }
            }
            else if (avroReader is IAvroReader<object>)
            {
                using (var streamReader = new SequentialReader<object>(avroReader as IAvroReader<object>))
                {
                    foreach (var item in streamReader.Objects)
                        yield return ToDynamicObject(item);
                }
            }
        }

        private IEnumerable<object> AsEnumerable(IEnumerable<object> objs, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, object> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool? doWhile = true;
            bool abortRequested = false;
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;

            foreach (var obj in objs)
            {
                pair = new Tuple<long, object>(++counter, obj);
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
                if (skip == null)
                    break;
                if (skip.Value)
                    continue;

                if (!_configCheckDone)
                {
                    Configuration.ValidateInternal(pair);
                    _configCheckDone = true;
                }

                object rec = null;
                if (TraceSwitch.TraceVerbose)
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading node [{0}]...".FormatString(pair.Item1));

                if (!LoadNode(pair, ref rec))
                    yield break;

                if (rec == null)
                    continue;

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

                pair = null;
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1, true);
        }

        private bool LoadNode(Tuple<long, object> pair, ref object rec)
        {
            bool ignoreFieldValue = pair.Item2.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
            if (ignoreFieldValue)
                return false;
            else if (pair.Item2 == null && !Configuration.IsDynamicObjectInternal)
            {
                rec = RecordType.CreateInstanceAndDefaultToMembers(Configuration.RecordFieldConfigurationsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ChoRecordFieldConfiguration));
                return true;
            }

            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping...");
                    rec = null;
                    return true;
                }
                //if (Configuration.CustomNodeSelecter != null)
                //{
                //    pair = new Tuple<long, object>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                //}

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                rec = pair.Item2;
                if (rec is AvroRecord)
                {
                    rec = ToDynamicObject(rec as AvroRecord);
                }

                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                    rec.DoObjectLevelValidation(Configuration, Configuration.AvroRecordFieldConfigurations);


                bool skip = false;
                if (!RaiseAfterRecordLoad(rec, pair, ref skip))
                    return false;
                else if (skip)
                {
                    rec = null;
                    return true;
                }
            }
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

            return true;
        }

        private object ToDynamicObject(object rec)
        {
            if (!(rec is AvroRecord))
                return rec;

            var output = new ChoDynamicObject();

            var avroRec = ((AvroRecord)rec);
            var schema = avroRec.Schema;
            foreach (var f in schema.Fields)
                output.Add(f.Name, ToDynamicObject(avroRec[f.Name]));

            return output;
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

        private bool? RaiseSkipUntil(Tuple<long, object> pair)
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

        private bool? RaiseDoWhile(Tuple<long, object> pair)
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

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, object> pair)
        {
            if (Reader != null && Reader.HasBeforeRecordLoadSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, object>(index, state as IDictionary<string, object>);

                return retValue;
            }
            else if (_callbackRecordRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, object>(index, state as IDictionary<string, object>);

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, object> pair, ref bool skip)
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

        private bool RaiseRecordLoadError(object target, Tuple<long, object> pair, Exception ex)
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

        #endregion Event Raisers

        private bool RaiseRecordFieldDeserialize(object target, long index, string propName, ref object value)
        {
            if (Reader is IChoSerializableReader && ((IChoSerializableReader)Reader).HasRecordFieldDeserializeSubcribed)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoSerializableReader)Reader).RaiseRecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (target is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = target as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (_callbackRecordSeriablizable is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            return false;
        }
    }
}
