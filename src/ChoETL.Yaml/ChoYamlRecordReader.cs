using Newtonsoft.Json;
using SharpYaml;
using SharpYaml.Events;
using SharpYaml.Serialization;
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
    internal class ChoYamlRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        private Lazy<Serializer> _se;
        internal ChoReader Reader = null;
        private Lazy<List<YamlNode>> _recBuffer = null;

        public ChoYamlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoYamlRecordReader(Type recordType, ChoYamlRecordConfiguration configuration) : base(recordType, false)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            //Configuration.Validate();
            _recBuffer = new Lazy<List<YamlNode>>(() =>
            {
                if (Reader != null)
                {
                    var b = Reader.Context.ContainsKey("RecBuffer") ? Reader.Context.RecBuffer : null;
                    if (b == null)
                        Reader.Context.RecBuffer = new List<YamlNode>();

                    return Reader.Context.RecBuffer;
                }
                else
                    return new List<YamlNode>();
            });
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            if (source == null)
                yield break;

            if (Configuration.JsonSerializerSettings.ContractResolver is ChoPropertyRenameAndIgnoreSerializerContractResolver)
            {
                ChoPropertyRenameAndIgnoreSerializerContractResolver cr = Configuration.JsonSerializerSettings.ContractResolver as ChoPropertyRenameAndIgnoreSerializerContractResolver;
                cr.CallbackRecordFieldRead = _callbackRecordFieldRead;
                cr.Reader = Reader;
            }

            InitializeRecordConfiguration(Configuration);

            if (!RaiseBeginLoad(source))
                yield break;

            if (source is YamlStream)
            {
                foreach (var item in AsEnumerable(ReadYamlNodes(source as YamlStream), TraceSwitch, filterFunc))
                {
                    yield return item;
                }
            }
            else if (source is EventReader)
            {
                foreach (var item in AsEnumerable(ReadYamlNodes(source as EventReader), TraceSwitch, filterFunc))
                {
                    yield return item;
                }
            }
            else if (source.GetType().IsGenericType && source.GetType().GetGenericTypeDefinition() == typeof(IList<>))
            {
                if (source.GetType().GetGenericArguments()[0] == typeof(YamlNode))
                {
                    foreach (var item in AsEnumerable(ReadYamlNodes(((IList)source).OfType<YamlNode>()), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }
                else if (source.GetType().GetGenericArguments()[0] == typeof(YamlDocument))
                {
                    foreach (var item in AsEnumerable(ReadYamlNodes(((IList)source).OfType<YamlDocument>()), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }
            }
            else if (source is Array && source.GetType().HasElementType)
            {
                if (source.GetType().GetElementType() == typeof(YamlNode))
                {
                    foreach (var item in AsEnumerable(ReadYamlNodes(((IList)source).OfType<YamlNode>()), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }
                else if (source.GetType().GetElementType() == typeof(YamlDocument))
                {
                    foreach (var item in AsEnumerable(ReadYamlNodes(((IList)source).OfType<YamlDocument>()), TraceSwitch, filterFunc))
                    {
                        yield return item;
                    }
                }
            }
            else
                throw new ArgumentException("Invalid Yaml stream object passed.");

            RaiseEndLoad(source);
        }

        private IEnumerable<IDictionary<string, object>> ReadYamlNodes(EventReader sr)
        {
            if (Configuration.YamlPath.IsNullOrWhiteSpace())
            {
                sr.Expect<StreamStart>();

                do
                {
                    yield return new SharpYaml.Serialization.Serializer().Deserialize<IDictionary<string, object>>(sr)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, Configuration.StringComparer);
                }
                while (!sr.Accept<StreamEnd>());
            }
            else
            {
                sr.Expect<StreamStart>();

                do
                {
                    bool iterateAllItems = false;
                    object value = null;
                    new SharpYaml.Serialization.Serializer().Deserialize<IDictionary<string, object>>(sr).TrySelectValue(Configuration.StringComparer, Configuration.YamlPath, out value, out iterateAllItems);
                    bool ignoreFieldValue = value.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        continue;

                    if (value is IDictionary<object, object>)
                        value = ((IDictionary<object, object>)value).ToDictionary(kvp1 => kvp1.Key.ToNString(), kvp1 => kvp1.Value, Configuration.StringComparer);

                    if (value is IDictionary<string, object>)
                        yield return value as IDictionary<string, object>;
                    else
                    {
                        if (iterateAllItems && value is IList)
                        {
                            object item1 = null;
                            foreach (var item in (IList)value)
                            {
                                if (item is IDictionary<object, object>)
                                    item1 = ((IDictionary<object, object>)item).ToDictionary(kvp1 => kvp1.Key.ToNString(), kvp1 => kvp1.Value, Configuration.StringComparer);
                                else
                                    item1 = item;

                                if (item1 is IDictionary<string, object>)
                                    yield return item1 as IDictionary<string, object>;
                                else
                                {
                                    dynamic v = new ChoDynamicObject();
                                    v.Value = item1;

                                    yield return v;

                                }
                            }
                        }
                        else
                        {
                            dynamic v = new ChoDynamicObject();
                            v.Value = value;

                            yield return v;
                        }
                    }
                }
                while (!sr.Accept<StreamEnd>());
            }
        }

        private IEnumerable<IDictionary<string, object>> ReadYamlNodes(YamlStream sr)
        {
            if (Configuration.YamlPath.IsNullOrWhiteSpace())
            {
                foreach (var doc in sr.Documents)
                {
                    if (doc.RootNode == null)
                        continue;

                    yield return doc.RootNode.ToExpando(Configuration.StringComparer);
                }
            }
            else
            {
                foreach (var doc in sr.Documents)
                {
                    if (doc.RootNode == null)
                        continue;

                    bool iterAllItems = false;
                    object value = null;
                    doc.RootNode.ToExpando(Configuration.StringComparer).TrySelectValue(Configuration.StringComparer, Configuration.YamlPath, out value, out iterAllItems);
                    if (value is IDictionary<string, object>)
                        yield return value as IDictionary<string, object>;
                    else
                    {
                        dynamic v = new ChoDynamicObject();
                        v.Value = value;
                    }
                }
            }
        }

        private IEnumerable<IDictionary<string, object>> ReadYamlNodes(IEnumerable<YamlNode> sr)
        {
            if (Configuration.YamlPath.IsNullOrWhiteSpace())
            {
                foreach (var node in sr)
                {
                    yield return node.ToExpando(Configuration.StringComparer);
                }
            }
            else
            {
                foreach (var node in sr)
                {
                    bool iterAllItems = false;
                    object value = null;
                    node.ToExpando(Configuration.StringComparer).TrySelectValue(Configuration.StringComparer, Configuration.YamlPath, out value, out iterAllItems);
                    if (value is IDictionary<string, object>)
                        yield return value as IDictionary<string, object>;
                    else
                    {
                        dynamic v = new ChoDynamicObject();
                        v.Value = value;
                    }
                }
            }
        }

        private IEnumerable<IDictionary<string, object>> ReadYamlNodes(IEnumerable<YamlDocument> sr)
        {
            if (Configuration.YamlPath.IsNullOrWhiteSpace())
            {
                foreach (var doc in sr)
                {
                    if (doc.RootNode == null)
                        continue;

                    yield return doc.RootNode.ToExpando(Configuration.StringComparer);
                }
            }
            else
            {
                foreach (var doc in sr)
                {
                    if (doc.RootNode == null)
                        continue;

                    bool iterAllItems = false;
                    object value = null;
                    doc.RootNode.ToExpando(Configuration.StringComparer).TrySelectValue(Configuration.StringComparer, Configuration.YamlPath, out value, out iterAllItems);
                    if (value is IDictionary<string, object>)
                        yield return value as IDictionary<string, object>;
                    else
                    {
                        dynamic v = new ChoDynamicObject();
                        v.Value = value;
                    }
                }
            }
        }

        private IEnumerable<object> AsEnumerable(IEnumerable<IDictionary<string, object>> yamlObjects, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, IDictionary<string, object>> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool? doWhile = true;
            bool abortRequested = false;
            _se = new Lazy<Serializer>(() => new Serializer());
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;

            foreach (var obj in yamlObjects)
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
                    if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null && !Configuration.RecordTypeMapped)
                    {
                    }
                    else
                        Configuration.Validate(pair);
                    var dict = Configuration.YamlRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                    if (Configuration.MaxScanRows == 0)
                        RaiseMembersDiscovered(dict);
                    Configuration.UpdateFieldTypesIfAny(dict);
                    _configCheckDone = true;
                }

                object rec = null;
                if (TraceSwitch.TraceVerbose)
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading node [{0}]...".FormatString(pair.Item1));

                if (!LoadNode(pair, ref rec))
                    yield break;

                if (rec == null)
                    continue;

                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.AreAllFieldTypesNull && Configuration.AutoDiscoverFieldTypes && Configuration.MaxScanRows > 0 && counter <= Configuration.MaxScanRows)
                    {
                        buffer.Add(rec);
                        if (recFieldTypes == null)
                            recFieldTypes = Configuration.YamlRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                        RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, counter == Configuration.MaxScanRows);
                        if (counter == Configuration.MaxScanRows)
                        {
                            Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                            var dict = recFieldTypes = Configuration.YamlRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                            RaiseMembersDiscovered(dict);

                            foreach (object rec1 in buffer)
                                yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes));

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
            }

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {
                if (buffer.Count > 0)
                {
                    Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                    var dict = recFieldTypes = Configuration.YamlRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);

                    foreach (object rec1 in buffer)
                        yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes));
                }
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1);
        }

        private bool LoadNode(Tuple<long, IDictionary<string, object>> pair, ref object rec)
        {
            if (Configuration.SupportsMultiRecordTypes && Configuration.RecordSelector != null)
            {
                Type recType = Configuration.RecordSelector(pair);
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

                if (!Configuration.RecordTypeMapped)
                {
                    Configuration.MapRecordFields(recType);
                    Configuration.Validate(null);
                }

                rec = recType.IsDynamicType() ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : ChoActivator.CreateInstance(recType);
            }
            else /*if (!Configuration.UseYamlSerialization || Configuration.IsDynamicObject)*/
                rec = Configuration.IsDynamicObject ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : ChoActivator.CreateInstance(RecordType);

            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                {
                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping...");
                    rec = null;
                    return true;
                }
                if (Configuration.CustomNodeSelecter != null)
                {
                    pair = new Tuple<long, IDictionary<string, object>>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                }

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                if (/*!Configuration.UseYamlSerialization &&*/ !typeof(ICollection).IsAssignableFrom(Configuration.RecordType)
                    && !(Configuration.RecordType.IsGenericType && Configuration.RecordType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    )
                {
                    if (!FillRecord(ref rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.YamlRecordFieldConfigurations);
                }
                else
                {
                    //rec = _se.Value != null ? pair.Item2.ToObject(RecordType, _se.Value) : pair.Item2.ToObject(RecordType);
                    //if (Configuration.IsDynamicObject)
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        rec = _se.Value.Deserialize<ExpandoObject>(pair.Item2.ToString());
                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                            rec.DoObjectLevelValidation(Configuration, Configuration.YamlRecordFieldConfigurations);
                    }
                    else
                    {
                        rec = _se.Value.Deserialize(pair.Item2.ToString(), RecordType);
                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                            rec.DoObjectLevelValidation(Configuration, Configuration.YamlRecordFieldConfigurations);

                    }
                }


                bool skip = false;
                if (!RaiseAfterRecordLoad(rec, pair, ref skip))
                    return false;
                else if (skip)
                {
                    rec = null;
                    return true;
                }
            }
            //catch (ChoParserException)
            //{
            //    throw;
            //}
            //catch (ChoMissingRecordFieldException)
            //{
            //    throw;
            //}
            catch (Exception ex)
            {
                Reader.IsValid = false;
                if (ex is ChoMissingRecordFieldException && Configuration.ThrowAndStopOnMissingField)
                {
                    if (!RaiseRecordLoadError(rec, pair, ex))
                        throw;
                    else
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        rec = null;
                    }
                }
                else
                {
                    ChoETLFramework.HandleException(ref ex);
                    if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                        rec = null;
                    }
                    else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                    {
                        if (!RaiseRecordLoadError(rec, pair, ex))
                            throw;
                        else
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Error [{0}] found. Ignoring record...".FormatString(ex.Message));
                            rec = null;
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
        ChoYamlRecordFieldConfiguration fieldConfig = null;
        PropertyInfo pi = null;

        private bool FillRecord(ref object rec, Tuple<long, IDictionary<string, object>> pair)
        {
            long lineNo;
            IDictionary<string, object> node;
            IDictionary<string, object> yamlToken = null;
            IDictionary<string, object>[] yamlTokens = null;

            lineNo = pair.Item1;
            node = pair.Item2;

            fieldValue = null;
            fieldConfig = null;
            pi = null;
            //IDictionary<string, object> dictValues = ToDictionary(node);

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {

            }
            else
            {
                if (rec.FillIfCustomSerialization(pair.Item2))
                    return true;

                if (FillIfKeyValueObject(rec, pair.Item2))
                    return true;
            }

            object rootRec = rec;
            foreach (KeyValuePair<string, ChoYamlRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                yamlToken = null;
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                //fieldValue = dictValues[kvp.Key];
                if (!kvp.Value.YamlPath.IsNullOrWhiteSpace())
                {
                    bool iterAllItems = false;
                    if (!node.TrySelectValue(Configuration.StringComparer, kvp.Value.YamlPath, out fieldValue, out iterAllItems))
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                    }
                }
                else
                {
                    if (!node.ContainsKey(kvp.Value.FieldName)) //, StringComparison.CurrentCultureIgnoreCase, out yamlToken))
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                        //else
                        //    jToken = node;
                    }
                    else
                        fieldValue = node[kvp.Value.FieldName];
                }

                if (fieldValue is IList)
                    fieldValue = ((IList)fieldValue).OfType<IDictionary>().ToArray();

                //fieldValue = !yamlTokens.IsNullOrEmpty() ? (object)yamlTokens : yamlToken;
                //if (!fieldConfig.FieldType.IsCollection())
                //    yamlToken = yamlTokens.FirstOrDefault();
                Reader.ContractResolverState = new ChoContractResolverState
                {
                    Name = kvp.Key,
                    Index = pair.Item1,
                    Record = rec,
                    FieldConfig = kvp.Value
                };

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    continue;
                try
                {
                    //if (Configuration.IsDynamicObject) //rec is ExpandoObject)
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                    }
                    else
                    {
                        if (pi != null)
                        {
                            if (kvp.Value.FieldTypeSelector != null)
                            {
                                Type rt = kvp.Value.FieldTypeSelector(pair.Item2);
                                kvp.Value.FieldType = rt == null ? pi.PropertyType : rt;
                            }
                            else
                                kvp.Value.FieldType = pi.PropertyType;
                        }
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    object v1 = !yamlTokens.IsNullOrEmpty() ? (object)yamlTokens : yamlToken == null ? node : yamlToken;
                    if (fieldConfig.CustomSerializer != null)
                        fieldValue = fieldConfig.CustomSerializer(v1);
                    else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref v1))
                        fieldValue = v1;
                    else
                    {
                        if (fieldConfig.FieldType == null)
                        {
                            if (!fieldConfig.IsArray && fieldValue is IDictionary[])
                            {
                                fieldValue = ((IDictionary[])fieldValue).FirstOrDefault();
                                //if (fieldValue is JArray)
                                //{
                                //    fieldValue = ((JArray)fieldValue).FirstOrDefault();
                                //}
                            }
                        }
                        else
                        {
                            if (!fieldConfig.FieldType.IsCollection() && fieldValue is IDictionary[])
                            {
                                fieldValue = ((IDictionary[])fieldValue).FirstOrDefault();
                                //if (fieldValue is JArray)
                                //{
                                //    fieldValue = ((JArray)fieldValue).FirstOrDefault();
                                //}
                            }
                        }

                        if (fieldConfig.FieldType == null
                            || fieldConfig.FieldType == typeof(object)
                            || fieldConfig.FieldType.GetItemType() == typeof(object))
                        {
                            if (fieldValue is IDictionary)
                            {
                                fieldValue = DeserializeNode((IDictionary)fieldValue, null, fieldConfig);
                            }
                            else if (fieldValue is IDictionary[])
                            {
                                List<object> arr = new List<object>();
                                foreach (var ele in (IDictionary[])fieldValue)
                                {
                                    object fv = DeserializeNode(ele, null, fieldConfig);
                                    arr.Add(fv);
                                }

                                fieldValue = arr.ToArray();
                            }
                        }
                        else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                        {
                            if (fieldValue is IDictionary[])
                                fieldValue = ((IDictionary[])fieldValue).FirstOrDefault();

                            if (fieldValue is IDictionary)
                            {
                                fieldValue = DeserializeNode((IDictionary)fieldValue, typeof(string) /*fieldConfig.FieldType*/, fieldConfig);
                            }
                        }
                        else
                        {
                            List<object> list = new List<object>();
                            Type itemType = fieldConfig.FieldType.GetUnderlyingType();

                            //if (itemType.IsCollectionType())
                            //    itemType = itemType.GetItemType().GetUnderlyingType();

                            if (fieldValue != null && fieldValue.GetType().IsAssignableFrom(fieldConfig.FieldType))
                            {

                            }
                            else if (fieldValue is IDictionary)
                            {
                                if (!typeof(IDictionary).IsAssignableFrom(itemType))
                                    fieldValue = DeserializeNode((IDictionary)fieldValue, itemType, fieldConfig);
                            }
                            //else if (fieldValue is JArray)
                            //{
                            //    if (typeof(JArray).IsAssignableFrom(itemType))
                            //    {

                            //    }
                            //    else if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                            //    {
                            //        itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                            //        foreach (var ele in (JArray)fieldValue)
                            //        {
                            //            object fv = DeserializeNode(ele, itemType, fieldConfig);
                            //            list.Add(fv);
                            //        }
                            //        fieldValue = list.ToArray();
                            //    }
                            //    else
                            //    {
                            //        var fi = ((JArray)fieldValue).FirstOrDefault();
                            //        fieldValue = DeserializeNode(fi, itemType, fieldConfig);
                            //    }
                            //}
                            else if (fieldValue is IDictionary[])
                            {
                                itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                                if (typeof(IDictionary[]).IsAssignableFrom(itemType))
                                {

                                }
                                else if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                                {
                                    //var isJArray = ((YamlNode[])fieldValue).Length == 1 && ((YamlNode[])fieldValue)[0] is JArray;
                                    //var array = isJArray ? ((JArray)((YamlNode[])fieldValue)[0]).ToArray() : (YamlNode[])fieldValue;
                                    foreach (var ele in (IDictionary[])fieldValue)
                                    {
                                        object fv = DeserializeNode(ele, itemType, fieldConfig);
                                        list.Add(fv);
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    var fi = ((IDictionary[])fieldValue).FirstOrDefault();
                                    fieldValue = DeserializeNode(fi, itemType, fieldConfig);
                                }
                            }
                        }
                    }

                    //if (!(fieldValue is ICollection))
                    //{
                    //    if (fieldValue is string)
                    //        fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string);
                    //    //else if (fieldValue is JValue)
                    //    //{
                    //    //    if (((JValue)fieldValue).Value is string)
                    //    //        fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue.ToString());
                    //    //    else
                    //    //        fieldValue = ((JValue)fieldValue).Value;
                    //    //}
                    //}

                    bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
                        if (Configuration.SupportsMultiRecordTypes)
                        {
                            ChoType.TryGetProperty(rec.GetType(), kvp.Key, out pi);
                            fieldConfig.PI = pi;
                            fieldConfig.PropConverters = ChoTypeDescriptor.GetTypeConverters(fieldConfig.PI);
                            fieldConfig.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fieldConfig.PI);
                        }

                        if (pi != null)
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                        else if (RecordType.IsSimple())
                            rec = ChoConvert.ConvertTo(fieldValue, RecordType, Configuration.Culture);
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
                        throw;

                    try
                    {
                        if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                        {
                            var dict = rec as IDictionary<string, Object>;

                            if (dict.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture, ref fieldValue))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (dict.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (ex is ValidationException)
                                throw;
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
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
                                        if (Configuration.IsDynamicObject)
                                        {
                                            var dict = rec as IDictionary<string, Object>;

                                            dict.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture);
                                        }
                                        else
                                        {
                                            if (pi != null)
                                                rec.ConvertNSetMemberValue(kvp.Key, fieldConfig, ref fieldValue, Configuration.Culture);
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

            return true;
        }

        private object ToObject(IDictionary yamlNode, Type type, bool? useYamlSerialization = null, ChoYamlRecordFieldConfiguration config = null)
        {
            if (type == null)
                return yamlNode;
            else
            {
                var json = JsonConvert.SerializeObject(yamlNode, Newtonsoft.Json.Formatting.None);
                return JsonConvert.DeserializeObject(json, type, Configuration.JsonSerializerSettings);
            }
        }

        private object DeserializeNode(IDictionary yamlNode, Type type, ChoYamlRecordFieldConfiguration config)
        {
            object value = null;
            type = type == null ? fieldConfig.FieldType : type;
            try
            {
                value = ToObject(yamlNode, type, config.UseYamlSerialization, config);
            }
            catch
            {
                if (fieldConfig.ItemConverter != null)
                    value = RaiseItemConverter(config, value);
                else
                    throw;
            }

            return value;
        }

        private object AssignDefaultsToNullableMembers(object target, bool isTop = true)
        {
            if (target == null)
                return target;

            Type recordType = target.GetType();
            if (typeof(YamlNode).IsAssignableFrom(recordType))
                return target;
            if (recordType.IsSimple())
                return target;
            if (typeof(IList).IsAssignableFrom(recordType))
            {
                return ((IList)target).Cast((t) =>
                {
                    return AssignDefaultsToNullableMembers(t, false);
                });
            }
            if (typeof(IDictionary).IsAssignableFrom(recordType))
            {
                return ((IDictionary)target).Cast((t) =>
                {
                    var key = t.Key;
                    var value = t.Value;

                    key = AssignDefaultsToNullableMembers(key, false);
                    value = AssignDefaultsToNullableMembers(value, false);
                    return new KeyValuePair<object, object>(key, value);
                });
            }

            if (typeof(IEnumerable).IsAssignableFrom(recordType))
                return target;

            foreach (System.ComponentModel.PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
            {
                if (pd.PropertyType == typeof(object))
                {
                    var pi = ChoType.GetProperty(recordType, pd.Name);
                    var propConverters = ChoTypeDescriptor.GetTypeConverters(pi);
                    var propConverterParams = ChoTypeDescriptor.GetTypeConverterParams(pi);

                    var itemValue = ChoType.GetPropertyValue(target, pd.Name);

                    if (propConverters.IsNullOrEmpty())
                    {
                        if (itemValue != null)
                        {
                            if (typeof(IDictionary).IsAssignableFrom(itemValue.GetType()))
                            {
                                ChoType.SetPropertyValue(target, pd.Name, ToObject(itemValue as IDictionary, typeof(ChoDynamicObject)));
                            }
                        }
                    }
                    else
                    {
                        var fv = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, propConverters, propConverterParams, Configuration.Culture);
                        ChoType.SetPropertyValue(target, pd.Name, fv);
                    }
                }
                else
                {
                    ChoType.SetPropertyValue(target, pd.Name, AssignDefaultsToNullableMembers(ChoType.GetPropertyValue(target, pd.Name), false));
                }
            }

            return target;
        }

        private object SerializeObjectMembers(object target, bool isTop = true)
        {
            if (target == null)
                return target;

            Type recordType = target.GetType();
            if (typeof(YamlNode).IsAssignableFrom(recordType))
                return target;

            if (recordType.IsSimple())
                return target;
            if (typeof(IList).IsAssignableFrom(recordType))
            {
                return ((IList)target).Cast((t) =>
                {
                    return SerializeObjectMembers(t, false);
                });
            }
            if (typeof(IDictionary).IsAssignableFrom(recordType))
            {
                return ((IDictionary)target).Cast((t) =>
                {
                    var key = t.Key;
                    var value = t.Value;

                    key = SerializeObjectMembers(key, false);
                    value = SerializeObjectMembers(value, false);
                    return new KeyValuePair<object, object>(key, value);
                });
            }

            if (typeof(IEnumerable).IsAssignableFrom(recordType))
                return target;

            foreach (System.ComponentModel.PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
            {
                if (pd.PropertyType == typeof(object))
                {
                    var pi = ChoType.GetProperty(recordType, pd.Name);
                    var propConverters = ChoTypeDescriptor.GetTypeConverters(pi);
                    var propConverterParams = ChoTypeDescriptor.GetTypeConverterParams(pi);

                    var itemValue = ChoType.GetPropertyValue(target, pd.Name);

                    if (propConverters.IsNullOrEmpty())
                    {
                        if (itemValue != null)
                        {
                            if (typeof(IDictionary).IsAssignableFrom(itemValue.GetType()))
                            {
                                ChoType.SetPropertyValue(target, pd.Name, ToObject(itemValue as IDictionary, typeof(ChoDynamicObject)));
                            }
                        }
                    }
                    else
                    {
                        var fv = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, propConverters, propConverterParams, Configuration.Culture);
                        ChoType.SetPropertyValue(target, pd.Name, fv);
                    }
                }
                else
                {
                    ChoType.SetPropertyValue(target, pd.Name, SerializeObjectMembers(ChoType.GetPropertyValue(target, pd.Name), false));
                }
            }
            return target;
        }

        private object RaiseItemConverter(ChoYamlRecordFieldConfiguration fieldConfig, object fieldValue)
        {
            if (fieldConfig.ItemConverter != null)
            {
                if (fieldValue is IList)
                {
                    fieldValue = ((IList)fieldValue).Cast(fieldConfig.ItemConverter);
                }
                else
                    fieldValue = fieldConfig.ItemConverter(fieldValue);
            }
            else
            {

            }

            return fieldValue;
        }

        private bool FillIfKeyValueObject(object rec, IDictionary<string, object> yamlNode)
        {
            if (rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            {
                IDictionary<string, object> dict = yamlNode as IDictionary<string, object>;
                if (dict == null || dict.Count == 0)
                    return true;

                FillIfKeyValueObject(rec, dict.First());
            }
            return false;
        }

        private IDictionary<string, object> ToDynamic(YamlNode yamlNode)
        {
            return yamlNode.ToExpando(Configuration.StringComparer);
        }

        private IList FillIfKeyValueObject(Type type, IDictionary<string, object> yamlNode)
        {
            if (type.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(type))
            {
                IDictionary<string, object> dict = yamlNode as IDictionary<string, object>;
                if (dict == null || dict.Count == 0)
                    return null;

                IList recs = type.CreateGenericList();
                foreach (var kvp in dict)
                {
                    var rec = ChoActivator.CreateInstance(type);
                    FillIfKeyValueObject(rec, kvp);
                    recs.Add(rec);
                }
                return recs;
            }
            return null;
        }

        private bool FillIfKeyValueObject(object rec, KeyValuePair<string, object> kvp)
        {
            var isKVPAttrDefined = rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null;

            if (isKVPAttrDefined)
            {
                var kP = rec.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoKeyAttribute>() != null).FirstOrDefault();
                var vP = rec.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoValueAttribute>() != null).FirstOrDefault();

                if (kP != null && vP != null)
                {
                    kP.SetValue(rec, kvp.Key);
                    vP.SetValue(rec, kvp.Value);
                    return true;
                }
            }
            if (typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            {
                IChoKeyValueType kvp1 = rec as IChoKeyValueType;

                kvp1.Key = kvp.Key;
                kvp1.Value = kvp.Value;
                return true;
            }

            return false;
        }

        private List<T> CloneListAs<T>(IList<object> source)
        {
            // Here we can do anything we want with T
            // T == source[0].GetType()
            return source.Cast<T>().ToList();
        }
        private string CleanFieldValue(ChoFileRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForRead(fieldType, Configuration.FieldValueTrimOption);

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
            object state = null;
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
