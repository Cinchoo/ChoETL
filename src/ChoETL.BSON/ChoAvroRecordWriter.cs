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
        private List<dynamic> _records = new List<dynamic>();

        public ChoBSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoAvroRecordWriter(Type recordType, ChoBSONRecordConfiguration configuration) : base(recordType)
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

                return false;
            });
            //Configuration.Validate();
        }

        public void Dispose(StreamWriter sw)
        {
            if (sw != null)
            {
                RaiseEndWrite(sw);
            }
        }

        public void Dispose()
        {
            Dispose(_sw as StreamWriter);
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

        private bool _rowScanComplete = false;
        public override IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> WriteTo<T>(object writer, IEnumerable<object> records, Func<object, bool> predicate = null)
        {
            _sw = writer;
            IAvroWriter<T> sw1 = writer as IAvroWriter<T>;
            ChoGuard.ArgumentNotNull(sw1, "AvroWriter");

            using (SequentialWriter<T> sw = new SequentialWriter<T>(sw1, 24))
            {
                if (records == null) yield break;

                if (!BeginWrite.Value)
                    yield break;

                CultureInfo prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

                object recOutput = null;
                var recEnum = records.GetEnumerator();

                try
                {
                    object notNullRecord = GetFirstNotNullRecord(recEnum);
                    if (notNullRecord == null)
                        yield break;

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
                                else if (!(recOutput is T))
                                    continue;

                                try
                                {
                                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                                        record.DoObjectLevelValidation(Configuration, Configuration.AvroRecordFieldConfigurations);

                                    if (recOutput is T)
                                    {
                                        sw.Write((T)recOutput);

                                        if (!RaiseAfterRecordWrite(record, _index, recOutput))
                                            yield break;
                                    }
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
            }
        }

        StringBuilder msg = new StringBuilder(6400);
        object fieldValue = null;
        string fieldText = null;
        ChoBSONRecordFieldConfiguration fieldConfig = null;
        IDictionary<string, Object> dict = null;
        private bool ToText(long index, object rec, ref dynamic recText)
        {
            /*
            if (typeof(IChoScalarObject).IsAssignableFrom(Configuration.RecordType))
                rec = ChoActivator.CreateInstance(Configuration.RecordType, rec);

            msg.Clear();

            if (Configuration.ColumnCountStrict)
                CheckColumnCountStrict(rec);
            if (Configuration.ColumnOrderStrict)
                CheckColumnOrderStrict(rec);

            bool isInit = false;
            PropertyInfo pi = null;
            object rootRec = rec;
            foreach (KeyValuePair<string, ChoAvroRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict.OrderBy(kvp => kvp.Value.Priority))
            {
                //if (Configuration.IsDynamicObject)
                //{
                if (Configuration.IgnoredFields.Contains(kvp.Key))
                    continue;
                //}

                fieldConfig = kvp.Value;
                fieldValue = null;
                fieldText = String.Empty;
                if (Configuration.PIDict != null)
                {
                    // if FieldName is set
                    if (!string.IsNullOrEmpty(fieldConfig.FieldName))
                    {
                        // match using FieldName
                        Configuration.PIDict.TryGetValue(fieldConfig.FieldName, out pi);
                    }
                    else
                    {
                        // otherwise match usign the property name
                        Configuration.PIDict.TryGetValue(kvp.Key, out pi);
                    }
                }

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                if (!isInit)
                {
                    isInit = true;
                    if (Configuration.IsDynamicObject)
                        dict = rec.ToDynamicObject() as IDictionary<string, Object>;
                    if (Configuration.IsDynamicObject && Configuration.UseNestedKeyFormat)
                        dict = dict.Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToArray().ToDictionary();
                }

                if (Configuration.ThrowAndStopOnMissingField)
                {
                    if (Configuration.IsDynamicObject)
                    {
                        if (!dict.ContainsKey(kvp.Key))
                        {
                            if (!Configuration.IgnoreHeader)
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' CSV column.".FormatString(fieldConfig.FieldName));
                            if (fieldConfig.FieldPosition > dict.Count)
                                throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Avro column.".FormatString(fieldConfig.FieldName));
                        }
                    }
                    else
                    {
                        if (pi == null)
                            pi = Configuration.PIDict.Where(kvp1 => kvp.Value.FieldPosition == kvp.Value.FieldPosition).FirstOrDefault().Value;

                        if (pi == null)
                            throw new ChoMissingRecordFieldException("No matching property found in the object for '{0}' Avro column.".FormatString(fieldConfig.FieldName));
                    }
                }

                try
                {
                    if (Configuration.IsDynamicObject)
                    {
                        if (!Configuration.IgnoreHeader)
                            fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] :
                            fieldConfig.FieldPosition > 0 && fieldConfig.FieldPosition - 1 < dict.Keys.Count
                            && Configuration.RecordFieldConfigurationsDict.Count == dict.Keys.Count ? dict[dict.Keys.ElementAt(fieldConfig.FieldPosition - 1)] : null; // dict.GetValue(kvp.Key, Configuration.FileHeaderConfiguration.IgnoreCase, Configuration.Culture);
                        else
                            fieldValue = dict.ContainsKey(kvp.Key) ? dict[kvp.Key] : null;
                        if (kvp.Value.FieldType == null)
                        {
                            if (rec is ChoDynamicObject)
                            {
                                var dobj = rec as ChoDynamicObject;
                                kvp.Value.FieldType = dobj.GetMemberType(kvp.Key);
                            }
                            if (kvp.Value.FieldType == null)
                            {
                                if (fieldValue == null)
                                    kvp.Value.FieldType = typeof(string);
                                else
                                    kvp.Value.FieldType = fieldValue.GetType();
                            }
                        }
                    }
                    else
                    {
                        if (pi != null)
                        {
                            fieldValue = GetPropertyValue(rec, pi, fieldConfig);
                            if (kvp.Value.FieldType == null)
                                kvp.Value.FieldType = pi.PropertyType;
                        }
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    //Discover default value, use it if null
                    //if (fieldValue == null)
                    //{
                    //    if (fieldConfig.IsDefaultValueSpecified)
                    //        fieldValue = fieldConfig.DefaultValue;
                    //}
                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (!RaiseBeforeRecordFieldWrite(rec, index, kvp.Key, ref fieldValue))
                        return false;

                    if (fieldConfig.ValueSelector == null)
                    {
                        if (fieldConfig.ValueConverter != null)
                            fieldValue = fieldConfig.ValueConverter(fieldValue);
                        else
                            rec.GetNConvertMemberValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, true);
                    }
                    else
                    {
                        fieldValue = fieldConfig.ValueSelector(rec);
                    }

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.MemberLevel)
                        rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);

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
                    ChoETLFramework.HandleException(ref ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw;

                    try
                    {
                        if (Configuration.IsDynamicObject)
                        {
                            if (dict.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                            else if (dict.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                            else
                            {
                                var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                                fieldValue = null;
                                throw ex1;
                            }
                        }
                        else if (pi != null)
                        {
                            if (rec.GetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.GetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode, fieldValue);
                            else
                            {
                                var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                                fieldValue = null;
                                throw ex1;
                            }
                        }
                        else
                        {
                            var ex1 = new ChoWriterException($"Failed to write '{fieldValue}' value for '{fieldConfig.FieldName}' member.", ex);
                            fieldValue = null;
                            throw ex1;
                        }
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
                                if (!RaiseRecordFieldWriteError(rec, index, kvp.Key, ref fieldValue, ex))
                                    throw new ChoWriterException($"Failed to write '{fieldValue}' value of '{kvp.Key}' member.", ex);
                            }
                        }
                        else
                        {
                            throw new ChoWriterException("Failed to use '{0}' fallback value for '{1}' member.".FormatString(fieldValue, kvp.Key), innerEx);
                        }
                    }
                }

                recText.Add(kvp.Key, fieldValue);
            }
            */
            return true;
        }

        public object GetPropertyValue(object target, PropertyInfo propertyInfo, ChoBSONRecordFieldConfiguration fieldConfig)
        {
            if (typeof(IList).IsAssignableFrom(target.GetType()))
            {
                if (fieldConfig.ArrayIndex != null)
                {
                    var item = ((IList)target).OfType<object>().Skip(fieldConfig.ArrayIndex.Value).FirstOrDefault();
                    if (item != null)
                        return ChoType.GetPropertyValue(item, propertyInfo);
                }
                return null;
            }
            else
            {
                var item = ChoType.GetPropertyValue(target, propertyInfo);
                if (item != null && typeof(IList).IsAssignableFrom(item.GetType()))
                {
                    if (fieldConfig.ArrayIndex != null)
                    {
                        return ((IList)item).OfType<object>().Skip(fieldConfig.ArrayIndex.Value).FirstOrDefault();
                    }
                    return item;
                }
                else if (item != null && typeof(IDictionary<string, object>).IsAssignableFrom(item.GetType()))
                {
                    if (fieldConfig.DictKey != null && ((IDictionary)item).Contains(fieldConfig.DictKey))
                    {
                        return ((IDictionary)item)[fieldConfig.DictKey];
                    }
                    return item;
                }
                else
                    return item;
            }
        }

        private ChoFieldValueJustification GetFieldValueJustification(ChoFieldValueJustification? fieldValueJustification)
        {
            return fieldValueJustification == null ? ChoFieldValueJustification.None : fieldValueJustification.Value;
        }

        private char GetFillChar(char? fillChar)
        {
            return fillChar == null ? ' ' : fillChar.Value;
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
