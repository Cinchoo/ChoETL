using Newtonsoft.Json;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    internal class ChoParquetRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        internal ChoReader Reader = null;
        internal bool InterceptRowGroup = false;
        public event EventHandler<ChoRowGroupEventArgs> BeforeRowGroupLoad;
        public event EventHandler<ChoRowGroupEventArgs> AfterRowGroupLoaded;

        public ChoParquetRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoParquetRecordReader(Type recordType, ChoParquetRecordConfiguration configuration) : base(recordType, false)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            Configuration.ResetStatesInternal();
            if (source == null)
                yield break;

            ParquetReader sr = source as ParquetReader;
            ChoGuard.ArgumentNotNull(sr, "ParquetReader");

            InitializeRecordConfiguration(Configuration);

            if (!RaiseBeginLoad(sr))
                yield break;

            if (InterceptRowGroup)
            {
                foreach (var item in AsEnumerable(ReadObjectsByRowGroup(sr).SelectMany(i => i.Select(i1 => i1)), TraceSwitch, filterFunc))
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in AsEnumerable(ReadAllObjects(sr), TraceSwitch, filterFunc))
                {
                    yield return item;
                }
            }

            RaiseEndLoad(sr);
        }
        private IEnumerable<List<DataColumn[]>> ReadObjectsByRowGroup(ParquetReader sr, Func<object, bool?> filterFunc = null)
        {
            DataField[] dataFields = sr.Schema.GetDataFields();

            for (int i = 0; i < sr.RowGroupCount; i++)
            {
                if (RaiseBeforeRowGroupLoad(i, null))
                    continue;

                List<DataColumn[]> rowGroup = new List<DataColumn[]>();
                using (ParquetRowGroupReader groupReader = sr.OpenRowGroupReader(i))
                {
                    List<DataColumn> columns = new List<DataColumn>();
                    //var dc = dataFields.Select(groupReader.ReadColumnAsync).ToArray();
                    foreach (var df in dataFields)
                        columns.Add(ChoAsyncHelper.RunSync<DataColumn>(() => groupReader.ReadColumnAsync(df)));

                    rowGroup.Add(columns.ToArray());
                }
                if (!RaiseAfterRowGroupLoaded(i, rowGroup))
                    yield return rowGroup;
            }
        }

        private IEnumerable<DataColumn[]> ReadAllObjects(ParquetReader sr, Func<object, bool?> filterFunc = null)
        {
            DataField[] dataFields1 = sr.Schema.Fields.ToArray().OfType<DataField>().ToArray(); //sr.Schema.GetDataFields();
            DataField[] dataFields = dataFields1.Where(f => !Configuration.IgnoredFields.Any(ig => ig == f.Name)).ToArray();

            //dataFields.ForEach(f => Console.WriteLine(f.Name)).Loop();

            for (int i = 0; i < sr.RowGroupCount; i++)
            {
                using (ParquetRowGroupReader groupReader = sr.OpenRowGroupReader(i))
                {
                    var dc = dataFields.Select(f =>
                    {
                        try
                        {
                            return ChoAsyncHelper.RunSync<DataColumn>(() => groupReader.ReadColumnAsync(f));
                        }
                        catch (Exception ex)
                        {
                            if (Configuration.ErrorMode == ChoErrorMode.ThrowAndStop)
                                throw;

                            ChoETLFramework.WriteLog(TraceSwitch.TraceError, $"Failed to read `{f.Name}` field. {ex.Message}");
                            return null;
                        }
                    }).Where(f => f != null).ToArray();
                    yield return dc;
                }
            }
        }

        private long GetDataLength(DataColumn[] dc)
        {
            long _dataLength = 0;
            for (int j = 0; j < dc.Length; j++)
            {
                string cn = dc[j].Field.Name;
                Array arr = dc[j].Data;

                if (arr != null && _dataLength < arr.Length)
                    _dataLength = arr.Length;
            }

            return _dataLength;
        }

        private IEnumerable<IDictionary<string, object>> Unpack(DataColumn[] dc)
        {
            long index = 0;
            Dictionary<string, object> data = new Dictionary<string, object>();
            long dataLength = GetDataLength(dc);

            while (index < dataLength)
            {
                data.Clear();
                for (int j = 0; j < dc.Length; j++)
                {
                    string cn = dc[j].Field.Name;
                    Array arr = dc[j].Data;

                    if (arr == null)
                        data.Add(cn, null);

                    if (index < arr.Length)
                        data.Add(cn, arr.GetValue(index));
                    else
                        data.Add(cn, null);
                }
                yield return ConvertToNestedObjectIfApplicable(data);

                index++;
            }
        }

        private IDictionary<string, object> ConvertToNestedObjectIfApplicable(IDictionary<string, object> value)
        {
            return Configuration.UseNestedKeyFormat ? value.ConvertToNestedObject(Configuration.NestedKeySeparator != null ? Configuration.NestedKeySeparator.Value : '/',
                Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, true) :
                value;
        }

        private IEnumerable<object> AsEnumerable(IEnumerable<DataColumn[]> dataColumns, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, IDictionary<string, object>> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool? doWhile = true;
            bool abortRequested = false;
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;

            foreach (var dc in dataColumns)
            {
                foreach (var obj in Unpack(dc))
                {
                    pair = new Tuple<long, IDictionary<string, object>>(++counter, obj);
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
                        if (Configuration.SupportsMultiRecordTypes && Configuration.RecordTypeSelector != null && !Configuration.RecordTypeMappedInternal)
                        {
                        }
                        else
                            Configuration.ValidateInternal(pair);
                        var dict = Configuration.ParquetRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                        //if (Configuration.MaxScanRows == 0)
                        RaiseMembersDiscovered(dict);
                        Configuration.UpdateFieldTypesIfAny(dict);
                        _configCheckDone = true;
                    }

                    object rec = null;
                    if (TraceSwitch.TraceVerbose)
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading node [{0}]...".FormatString(pair.Item1));

                    rec = Configuration.IsDynamicObjectInternal ? new ChoDynamicObject()
                    {
                        ThrowExceptionIfPropNotExists = Configuration.ThrowExceptionIfDynamicPropNotExists == null ? ChoDynamicObjectSettings.ThrowExceptionIfPropNotExists : Configuration.ThrowExceptionIfDynamicPropNotExists.Value,
                    } : ChoActivator.CreateInstance(RecordType);

                    if (!LoadNode(pair, ref rec))
                        yield break;

                    if (rec == null)
                        continue;

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                        if (Configuration.AreAllFieldTypesNull && Configuration.AutoDiscoverFieldTypes && Configuration.MaxScanRows > 0 && counter <= Configuration.MaxScanRows)
                        {
                            buffer.Add(rec);
                            if (recFieldTypes == null)
                                recFieldTypes = Configuration.ParquetRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                            RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, counter == Configuration.MaxScanRows);
                            if (counter == Configuration.MaxScanRows)
                            {
                                Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                                var dict = recFieldTypes = Configuration.ParquetRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                                RaiseMembersDiscovered(dict);

                                foreach (object rec1 in buffer)
                                    yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes, Configuration.TypeConverterFormatSpec));

                                buffer.Clear();
                            }
                        }
                        else
                        {
                            yield return rec;
                        }
                    }
                    else
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
            }

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
            {
                if (buffer.Count > 0)
                {
                    Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                    var dict = recFieldTypes = Configuration.ParquetRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);

                    foreach (object rec1 in buffer)
                        yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes, Configuration.TypeConverterFormatSpec));
                }
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1, true);
        }

        private bool LoadNode(Tuple<long, IDictionary<string, object>> pair, ref object rec)
        {
            bool ignoreFieldValue = pair.Item2.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
            if (ignoreFieldValue)
                return false;
            else if (pair.Item2 == null && !Configuration.IsDynamicObjectInternal)
            {
                rec = RecordType.CreateInstanceAndDefaultToMembers(Configuration.RecordFieldConfigurationsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ChoRecordFieldConfiguration));
                return true;
            }

            if (Configuration.SupportsMultiRecordTypes && Configuration.RecordTypeSelector != null)
            {
                Type recType = Configuration.RecordTypeSelector(pair);
                if (recType == null)
                {
                    if (Configuration.IgnoreIfNoRecordTypeFound)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, $"No record type found for [{pair.Item1}] line to parse.");
                        return true;
                    }
                    else
                        throw new ChoParserException($"No record type found for [{pair.Item1}] line to parse.");
                }

                if (!Configuration.RecordTypeMappedInternal)
                {
                    Configuration.MapRecordFields(recType);
                    Configuration.ValidateInternal(null);
                }

                rec = recType.IsDynamicType() ? new ChoDynamicObject()
                {
                    ThrowExceptionIfPropNotExists = Configuration.ThrowExceptionIfDynamicPropNotExists == null ? ChoDynamicObjectSettings.ThrowExceptionIfPropNotExists : Configuration.ThrowExceptionIfDynamicPropNotExists.Value,
                } : ChoActivator.CreateInstance(recType);
                RecordType = recType;
            }
            else if (Configuration.IsDynamicObjectInternal)
                rec = Configuration.IsDynamicObjectInternal ? new ChoDynamicObject()
                {
                    ThrowExceptionIfPropNotExists = Configuration.ThrowExceptionIfDynamicPropNotExists == null ? ChoDynamicObjectSettings.ThrowExceptionIfPropNotExists : Configuration.ThrowExceptionIfDynamicPropNotExists.Value,
                } : ChoActivator.CreateInstance(RecordType);

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
                //    pair = new Tuple<long, IDictionary<string, object>>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                //}

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                if (!FillRecord(ref rec, pair))
                    return false;

                if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                    rec.DoObjectLevelValidation(Configuration, Configuration.ParquetRecordFieldConfigurations);


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

        object fieldValue = null;
        ChoParquetRecordFieldConfiguration fieldConfig = null;
        PropertyInfo pi = null;

        private bool IsInNeedOfCustomFormatter(Type type)
        {
            if (type == null)
                return false;

            Func<Type, Type> mapParquetType = Configuration.MapParquetType;
            if (mapParquetType != null)
            {
                type = mapParquetType(type);
            }

            var underlytingType = type.GetUnderlyingType();
            if (underlytingType == null)
                return false;

            if (underlytingType == typeof(DateTime))
                return false;
            else if (underlytingType == typeof(TimeSpan))
                return false;
            else if (underlytingType == typeof(ChoCurrency))
                return false;
            else if (underlytingType == typeof(Guid))
                return false;
            else if (underlytingType.IsEnum)
                return false;
            else if (type == typeof(byte[]))
                return false;
            else if (type == typeof(DateTimeOffset))
                return false;
            else if (underlytingType.IsSimpleSpecial())
                return false;
            else if (underlytingType == typeof(ChoDynamicObject))
                return false;
            else
                return true;
        }

        private object CustomDeserialize(string value, Type type)
        {
            if (value == null)
                return null;

            if (Configuration.CustomDeserializer == null)
                return JsonConvert.DeserializeObject(value, type, Configuration.JsonSerializerSettings);
            else
                return Configuration.CustomDeserializer(value, type);
        }

        private bool FillRecord(ref object rec, Tuple<long, IDictionary<string, object>> pair)
        {
            long lineNo;

            lineNo = pair.Item1;
            var node = pair.Item2;

            //if (Configuration.LiteParsing && Configuration.IsDynamicObjectInternal && rec is ChoDynamicObject)
            //{
            //    ((ChoDynamicObject)rec).SetDictionary(node as IDictionary<string, object>);
            //    return true;
            //}

            fieldValue = null;
            fieldConfig = null;
            pi = null;

            object rootRec = rec;
            foreach (KeyValuePair<string, ChoParquetRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDictInternal != null)
                {
                    // if FieldName is set
                    if (!string.IsNullOrEmpty(fieldConfig.FieldName))
                    {
                        // match using FieldName
                        Configuration.PIDictInternal.TryGetValue(fieldConfig.FieldName, out pi);
                    }
                    if (pi == null)
                    {
                        // otherwise match usign the property name
                        Configuration.PIDictInternal.TryGetValue(kvp.Key, out pi);
                    }
                }

                rec = GetDeclaringRecord(kvp.Value.DeclaringMemberInternal, rootRec);

                if (!node.ContainsKey(kvp.Value.FieldName))
                {
                    if (Configuration.ColumnCountStrict)
                        throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                }
                else
                    fieldValue = node[kvp.Value.FieldName];

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    continue;
                try
                {
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                    }
                    else
                    {
                        if (pi != null)
                            kvp.Value.FieldType = pi.PropertyType;
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    object v1 = node;

                    if (Configuration.IsDynamicObjectInternal && fieldConfig.FieldType != null && IsInNeedOfCustomFormatter(fieldConfig.FieldType))
                    {
                        fieldValue = CustomDeserialize(fieldValue.ToNString(), fieldConfig.FieldType);
                    }
                    else if (fieldConfig.CustomSerializer != null)
                        fieldValue = fieldConfig.CustomSerializer(fieldValue);
                    else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref fieldValue))
                        fieldValue = v1;
                    else if (fieldConfig.PropCustomSerializer != null)
                        fieldValue = ChoCustomSerializer.Deserialize(fieldValue, fieldConfig.FieldType, fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name);
                    else
                    {
                        if (!node.ContainsKey(kvp.Value.FieldName))
                        {
                            if (Configuration.ColumnCountStrict)
                                throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                        }
                        else
                            fieldValue = node[kvp.Value.FieldName];
                    }

                    if (fieldConfig.IgnoreFieldValueMode == null)
                    {
                        if (fieldValue.IsObjectNullOrEmpty() && fieldConfig.IsDefaultValueSpecified)
                            fieldValue = fieldConfig.DefaultValue;
                    }
                    else
                    {
                        bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                        if (ignoreFieldValue && fieldConfig.IsDefaultValueSpecified)
                            fieldValue = fieldConfig.DefaultValue;
                        ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                        if (ignoreFieldValue)
                            continue;
                    }

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture, config: Configuration);

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
                        if (Configuration.SupportsMultiRecordTypes)
                        {
                            ChoType.TryGetProperty(rec.GetType(), kvp.Key, out pi);
                            //*** TODO
                            //fieldConfig.PI = pi;
                            //fieldConfig.PropConverters = ChoTypeDescriptor.GetTypeConverters(fieldConfig.PI);
                            //fieldConfig.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fieldConfig.PI);
                        }

                        if (pi != null)
                        {
                            if (false) //Configuration.LiteParsing)
                            {
                                ChoType.SetPropertyValue(rec, fieldConfig.PIInternal,
                                    fieldConfig.FieldType == null || fieldConfig.FieldType == typeof(string) ? fieldValue : Convert.ChangeType(fieldValue, fieldConfig.FieldType, Configuration.Culture));
                            }
                            else
                                rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture, config: Configuration);
                        }
                        else if (RecordType.IsSimple())
                            rec = ChoConvert.ConvertTo(fieldValue, RecordType, Configuration.Culture, config: Configuration);
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
                    Reader.IsValid = false;
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    Reader.IsValid = false;
                    if (Configuration.ThrowAndStopOnMissingField)
                        throw;
                }
                catch (Exception ex)
                {
                    Reader.IsValid = false;
                    ChoETLFramework.HandleException(ref ex);

                    if (fieldConfig.ErrorMode == ChoErrorMode.ThrowAndStop)
                        throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);

                    try
                    {
                        if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                        {
                            var dict = rec as IDictionary<string, Object>;

                            if (dict.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue, Configuration))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (dict.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture, Configuration))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                    }
                    catch (Exception innerEx)
                    {
                        if (ex == innerEx.InnerException || ex is ValidationException)
                        {
                            if (fieldConfig.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                            {
                                ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring field...".FormatString(ex.Message));
                                continue;
                            }
                            else
                            {
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, ref fieldValue, ex))
                                {
                                    if (ex is ValidationException)
                                        throw;

                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                                }
                                else
                                {
                                    try
                                    {
                                        if (Configuration.IsDynamicObjectInternal)
                                        {
                                            var dict = rec as IDictionary<string, Object>;

                                            dict.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture, config: Configuration);
                                        }
                                        else
                                        {
                                            if (pi != null)
                                                rec.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture, config: Configuration);
                                            else
                                                throw new ChoMissingRecordFieldException("Missing '{0}' property in {1} type.".FormatString(kvp.Key, ChoType.GetTypeName(rec)));
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            throw new ChoReaderException("Failed to assign '{0}' fallback value to '{1}' field.".FormatString(fieldValue, fieldConfig.FieldName), innerEx);
                        }
                    }
                }
            }

            rec = rootRec;
            return true;
        }

        private string CleanFieldValue(ChoParquetRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForReadInternal(fieldType, Configuration.FieldValueTrimOption);

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
                    {
                        if (fieldValueTrimOption == ChoFieldValueTrimOption.TrimStart)
                            fieldValue = fieldValue.Right(config.Size.Value);
                        else
                            fieldValue = fieldValue.Substring(0, config.Size.Value);
                    }
                }
            }
            if (fieldValue.StartsWith(@"""") && fieldValue.EndsWith(@""""))
            {
                fieldValue = fieldValue.Substring(1, fieldValue.Length - 2);
            }

            return System.Net.WebUtility.HtmlDecode(fieldValue);
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

        private bool? RaiseSkipUntil(Tuple<long, IDictionary<string, object>> pair)
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

        private bool? RaiseDoWhile(Tuple<long, IDictionary<string, object>> pair)
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

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, IDictionary<string, object>> pair)
        {
            if (Reader != null && Reader.HasBeforeRecordLoadSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, IDictionary<string, object>>(index, state as IDictionary<string, object>);

                return retValue;
            }
            else if (_callbackRecordRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, IDictionary<string, object>>(index, state as IDictionary<string, object>);

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, IDictionary<string, object>> pair, ref bool skip)
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

        private bool RaiseRecordLoadError(object target, Tuple<long, IDictionary<string, object>> pair, Exception ex)
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

        private bool RaiseBeforeRowGroupLoad(int index, List<DataColumn[]> records)
        {
            ChoRowGroupEventArgs rowGroupEventArg = new ChoRowGroupEventArgs(index, records);
            EventHandler<ChoRowGroupEventArgs> rowGroupEvent = BeforeRowGroupLoad;
            if (rowGroupEvent != null)
                rowGroupEvent(this, rowGroupEventArg);

            return rowGroupEventArg.Skip;
        }

        private bool RaiseAfterRowGroupLoaded(int index, List<DataColumn[]> records)
        {
            ChoRowGroupEventArgs rowGroupEventArg = new ChoRowGroupEventArgs(index, records);
            EventHandler<ChoRowGroupEventArgs> rowGroupEvent = AfterRowGroupLoaded;
            if (rowGroupEvent != null)
                rowGroupEvent(this, rowGroupEventArg);

            return rowGroupEventArg.Skip;
        }
    }
}
