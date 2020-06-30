using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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
    internal class ChoJSONRecordReader : ChoRecordReader
    {
        private IChoNotifyFileRead _callbackFileRead;
        private IChoNotifyRecordRead _callbackRecordRead;
        private IChoNotifyRecordFieldRead _callbackRecordFieldRead;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        private Lazy<JsonSerializer> _se;
        internal ChoReader Reader = null;
        private Lazy<List<JObject>> _recBuffer = null;

        public override Type RecordType
        {
            get => Configuration != null ? Configuration.RecordType : base.RecordType;
            set
            {
                if (Configuration != null)
                    Configuration.RecordType = value;
            }
        }

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override ChoRecordConfiguration RecordConfiguration => Configuration;

        public ChoJSONRecordReader(Type recordType, ChoJSONRecordConfiguration configuration) : base(recordType, false)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecordFieldRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            _callbackFileRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(recordType);
            _callbackRecordRead = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;

            //Configuration.Validate();
            _recBuffer = new Lazy<List<JObject>>(() =>
            {
                if (Reader != null)
                {
                    var b = Reader.Context.ContainsKey("RecBuffer") ? Reader.Context.RecBuffer : null;
                    if (b == null)
                        Reader.Context.RecBuffer = new List<JObject>();

                    return Reader.Context.RecBuffer;
                }
                else
                    return new List<JObject>();
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

            JsonTextReader sr = source as JsonTextReader;
            ChoGuard.ArgumentNotNull(sr, "JsonTextReader");

            InitializeRecordConfiguration(Configuration);

            if (!RaiseBeginLoad(sr))
                yield break;

            foreach (var item in AsEnumerable(ReadJObjects(sr), TraceSwitch, filterFunc))
            {
                yield return item;
            }

            RaiseEndLoad(sr);
        }

        public IEnumerable<object> AsEnumerable(IEnumerable<JToken> jObjects, Func<object, bool?> filterFunc = null)
        {
            foreach (var item in AsEnumerable(ToJObjects(jObjects), TraceSwitch, filterFunc))
            {
                yield return item;
            }
        }

        private IEnumerable<JObject> ReadNodes(JsonTextReader sr)
        {
            while (sr.Read())
            {
                if (sr.TokenType == JsonToken.StartArray)
                {
                    while (sr.Read())
                    {
                        if (sr.TokenType == JsonToken.StartObject)
                        {
                            yield return JObject.Load(sr);
                        }
                        else if (sr.TokenType == JsonToken.StartArray)
                        {
                            var z = JArray.Load(sr).Children().ToArray();
                            dynamic x = new JObject(new JProperty("Value", z));
                            yield return x;
                        }
                        else if (sr.TokenType != JsonToken.EndArray)
                        {
                            dynamic x = null;
                            try
                            {
                                x = new JObject(new JProperty("Value", JToken.Load(sr)));
                            }
                            catch { }
                            if (x != null)
                                yield return x;
                        }
                    }
                }
                if (sr.TokenType == JsonToken.StartObject)
                    yield return (JObject)JToken.ReadFrom(sr);

                sr.Skip();
            }
        }

        public static IEnumerable<IDictionary<string, object>> ToCollections(object o)
        {
            var jo = o as JObject;
            if (jo != null)
            {
                var ret = jo.ToObject<IDictionary<string, object>>();
                ret.ToDictionary(k => k.Key, v => ToCollections(v.Value));

                yield return ret;
            }

            var ja = o as JArray;
            if (ja != null)
            {
                var list = ja.ToObject<List<object>>().Select(ToCollections).ToList();
                foreach (var e in list.OfType<IDictionary<string, object>>())
                {
                    yield return e;
                }
            }
        }

        private IEnumerable<JObject> ReadJObjects(JsonTextReader sr)
        {
            if (Configuration.JSONPath.IsNullOrWhiteSpace())
            {
                sr.SupportMultipleContent = Configuration.SupportMultipleContent == null ? true : Configuration.SupportMultipleContent.Value;
                foreach (var node in ReadNodes(sr))
                    yield return node;
            }
            else
            {
                bool dictKey = false;
                bool dictValue = false;
                string[] tokens = null;
                if (!Configuration.AllowComplexJSONPath && IsSimpleJSONPath(Configuration.JSONPath, out tokens, out dictKey, out dictValue))
                {
                    foreach (var jo in StreamElements(sr, tokens))
                    {
                        if (dictKey)
                        {
                            foreach (var dict in ToCollections(jo))
                            {
                                foreach (var kvp in dict)
                                {
                                    foreach (var j in ToJObjects(new JToken[] { new JValue(kvp.Key) }))
                                        yield return j;
                                }
                            }
                        }
                        else if (dictValue)
                        {
                            foreach (var dict in ToCollections(jo))
                            {
                                foreach (var kvp in dict)
                                {
                                    foreach (var j in ToJObjects(new JToken[] { kvp.Value is JToken ? (JToken)kvp.Value : new JValue(kvp.Value) }))
                                        yield return j;
                                }
                            }
                        }
                        else
                            yield return jo;
                    }
                }
                else
                {
                    if (!Configuration.AllowComplexJSONPath)
                        throw new JsonException("Complex JSON path not supported.");

                    foreach (var t in ToJObjects(JObject.Load(sr).SelectTokens(Configuration.JSONPath)))
                    {
                        yield return t;
                    }

                    //while (sr.Read())
                    //{
                    //    if (sr.TokenType == JsonToken.StartArray)
                    //    {
                    //        foreach (var t in ToJObjects(JArray.Load(sr).SelectTokens(Configuration.JSONPath)))
                    //        {
                    //            yield return t;
                    //        }
                    //    }
                    //    if (sr.TokenType == JsonToken.StartObject)
                    //    {
                    //        foreach (var t in ToJObjects(JObject.Load(sr).SelectTokens(Configuration.JSONPath)))
                    //        {
                    //            yield return t;
                    //        }
                    //    }
                    //}
                }
            }
        }

        private bool IsSimpleJSONPath(string jsonPath, out string[] tokens, out bool dictKey, out bool dictValue)
        {
            dictKey = false;
            dictValue = false;
            tokens = null;

            if (jsonPath.StartsWith("$"))
                jsonPath = jsonPath.Substring(1);
            while (jsonPath.StartsWith("."))
                jsonPath = jsonPath.Substring(1);
            if (jsonPath.Length == 0)
                return false;

            var tokens1 = jsonPath.SplitNTrim(".");
            if (String.Join("/", tokens1) == "~")
            {
                tokens = new string[] { "*" };
                dictKey = true;
                return true;
            }
            else if (String.Join("/", tokens1) == "^")
            {
                tokens = new string[] { "*" };
                dictValue = true;
                return true;
            }

            foreach (var token in tokens1)
            {
                if (token.IsNullOrWhiteSpace())
                    return false;
                if (!token.IsValidIdentifierEx())
                    return false;
            }

            tokens = tokens1;
            return true;
        }

        private bool ReadToFollowing(JsonTextReader sr, string elementName)
        {
            while (sr.Read())
            {
                if (sr.TokenType == JsonToken.StartObject)
                {
                    while (sr.Read())
                    {
                        if (sr.TokenType == JsonToken.PropertyName && (sr.Path == elementName || sr.Path.EndsWith($".{elementName}")))
                            return true;
                    }
                }
                break;
            }

            return false;
        }

        private IEnumerable<JObject> StreamElements(JsonTextReader sr, string[] elementNames)
        {
            if (elementNames.Length == 1)
            {
                string elementName = elementNames[0];
                if (elementName == "*")
                {
                    foreach (var node in ReadNodes(sr))
                        yield return node;
                }
                else
                {
                    if (ReadToFollowing(sr, elementName))
                    {
                        foreach (var node in ReadNodes(sr))
                            yield return node;
                    }
                }
            }
            else
            {
                bool match = true;
                foreach (var en in elementNames.Take(elementNames.Length - 1))
                {
                    if (!ReadToFollowing(sr, en))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    if (ReadToFollowing(sr, elementNames.Skip(elementNames.Length - 1).First()))
                    {
                        foreach (var node in ReadNodes(sr))
                            yield return node;
                    }

                }
            }
        }

        private bool IsKeyValuePairArray(JArray array)
        {
            try
            {
                if (array.Count == 2)
                {
                    object key = ((JArray)array).FirstOrDefault();
                    object value = ((JArray)array).Skip(1).FirstOrDefault();

                    if (key is JValue && value != null)
                        return true;
                }
            }
            catch { }

            return false;
        }

        private IEnumerable<JObject> ToJObjects(IEnumerable<JToken> tokens)
        {
            foreach (var t in tokens)
            {
                if (t is JArray)
                {
                    if (IsKeyValuePairArray(t as JArray))
                    {
                        int counter = 0;
                        JValue key = null;
                        JToken value = null;

                        foreach (JToken item in ((JArray)t))
                        {
                            counter++;

                            if (counter % 2 == 1)
                                key = item as JValue;
                            else
                            {
                                value = item as JToken;
                                dynamic obj = new JObject();
                                obj.Key = key;
                                obj.Value = value;

                                yield return obj;
                            }
                        }
                        if (counter % 2 == 1)
                        {
                            dynamic obj = new JObject();
                            obj.Key = key;
                            obj.Value = null;

                            yield return obj;
                        }
                    }
                    else
                    {
                        dynamic x1 = new JObject();
                        x1.Value = t;
                        yield return x1;

                        //foreach (JToken item in ((JArray)t))
                        //{
                        //    if (item is JObject)
                        //        yield return item.ToObject<JObject>();
                        //    else if (item is JValue)
                        //    {
                        //        dynamic x = new JObject();
                        //        x.Value = ((JValue)item).Value;
                        //        yield return x;
                        //    }
                        //    else if (item is JArray)
                        //    {
                        //        dynamic x = new JObject();
                        //        x.Value = item;
                        //        yield return x;
                        //    }
                        //}
                    }
                }
                else if (t is JObject)
                    yield return t.ToObject<JObject>();
                else if (t is JValue)
                    yield return new JObject(new JProperty("Value", ((JValue)t).Value));
            }

        }

        private IEnumerable<object> AsEnumerable(IEnumerable<JObject> jObjects, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            long counter = 0;
            Tuple<long, JObject> pair = null;
            bool? skip = false;
            bool? skipUntil = true;
            bool? doWhile = true;
            bool abortRequested = false;
            _se = new Lazy<JsonSerializer>(() => Configuration.JsonSerializerSettings != null ? JsonSerializer.Create(Configuration.JsonSerializerSettings) : null);
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;

            foreach (var obj in jObjects)
            {
                pair = new Tuple<long, JObject>(++counter, obj);
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
                    var dict = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
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
                            recFieldTypes = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                        RaiseRecordFieldTypeAssessment(recFieldTypes, (IDictionary<string, object>)rec, counter == Configuration.MaxScanRows);
                        if (counter == Configuration.MaxScanRows)
                        {
                            Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                            var dict = recFieldTypes = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
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
                    var dict = recFieldTypes = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);

                    foreach (object rec1 in buffer)
                        yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes));
                }
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1);
        }

        private bool LoadNode(Tuple<long, JObject> pair, ref object rec)
        {
            bool ignoreFieldValue = pair.Item2.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
            if (ignoreFieldValue)
                return false;
            else if (pair.Item2 == null && !Configuration.IsDynamicObject)
            {
                rec = RecordType.CreateInstanceAndDefaultToMembers(Configuration.RecordFieldConfigurationsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ChoRecordFieldConfiguration));
                return true;
            }

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
            else if (!Configuration.UseJSONSerialization || Configuration.IsDynamicObject)
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
                    pair = new Tuple<long, JObject>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                }

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                if (!Configuration.UseJSONSerialization
                    && !typeof(ICollection).IsAssignableFrom(Configuration.RecordType)
                    && !(Configuration.RecordType.IsGenericType && Configuration.RecordType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    )
                {
                    if (!FillRecord(ref rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);
                }
                else
                {
                    //rec = _se.Value != null ? pair.Item2.ToObject(RecordType, _se.Value) : pair.Item2.ToObject(RecordType);
                    //if (Configuration.IsDynamicObject)
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                    {
                        if (pair.Item2 is IDictionary)
                            rec = pair.Item2;
                        else
                            rec = JsonConvert.DeserializeObject<ExpandoObject>(pair.Item2.ToString(), Configuration.JsonSerializerSettings);
                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                            rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);
                    }
                    else
                    {
                        rec = Configuration.JsonSerializerSettings == null ? JsonConvert.DeserializeObject(pair.Item2.ToString(), RecordType) : JsonConvert.DeserializeObject(pair.Item2.ToString(), RecordType, Configuration.JsonSerializerSettings);
                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                            rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);

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

        private IDictionary<string, object> ToDictionary(JObject @object)
        {
            var result = @object.ToObject<Dictionary<string, object>>();

            var JObjectKeys = (from r in result
                               let key = r.Key
                               let value = r.Value
                               where value.GetType() == typeof(JObject)
                               select key).ToList();

            var JArrayKeys = (from r in result
                              let key = r.Key
                              let value = r.Value
                              where value.GetType() == typeof(JArray)
                              select key).ToList();

            JArrayKeys.ForEach(key => result[key] = ((JArray)result[key]).Values().Select(x => ((JValue)x).Value).ToArray());
            JObjectKeys.ForEach(key => result[key] = ToDictionary(result[key] as JObject));

            return result;
        }

        object fieldValue = null;
        ChoJSONRecordFieldConfiguration fieldConfig = null;
        PropertyInfo pi = null;

        private bool FillRecord(ref object rec, Tuple<long, JObject> pair)
        {
            long lineNo;
            JObject node;
            JToken jToken = null;
            JToken[] jTokens = null;

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
            foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                jToken = null;
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

                rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

                //fieldValue = dictValues[kvp.Key];
                if (!kvp.Value.JSONPath.IsNullOrWhiteSpace())
                {
                    jTokens = node.SelectTokens(kvp.Value.JSONPath).ToArray();
                    if (!fieldConfig.FieldType.IsCollection())
                        jToken = jTokens.FirstOrDefault();
                    if (jToken == null)
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                    }
                }
                else
                {
                    if (!node.TryGetValue(kvp.Value.FieldName, StringComparison.CurrentCultureIgnoreCase, out jToken))
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                        //else
                        //    jToken = node;
                    }
                }

                fieldValue = !jTokens.IsNullOrEmpty() ? (object)jTokens : jToken;
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

                    object v1 = !jTokens.IsNullOrEmpty() ? (object)jTokens : jToken == null ? node : jToken;
                    if (fieldConfig.CustomSerializer != null)
                        fieldValue = fieldConfig.CustomSerializer(v1);
                    else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref v1))
                        fieldValue = v1;
                    else
                    {
                        if (fieldConfig.FieldType == null)
                        {
                            if (!fieldConfig.IsArray && fieldValue is JToken[])
                            {
                                fieldValue = ((JToken[])fieldValue).FirstOrDefault();
                                if (fieldValue is JArray)
                                {
                                    fieldValue = ((JArray)fieldValue).FirstOrDefault();
                                }
                            }
                        }
                        else
                        {
                            if (!fieldConfig.FieldType.IsCollection() && fieldValue is JToken[])
                            {
                                fieldValue = ((JToken[])fieldValue).FirstOrDefault();
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
                            if (fieldValue is JToken)
                            {
                                fieldValue = DeserializeNode((JToken)fieldValue, null, fieldConfig);
                            }
                            else if (fieldValue is JToken[])
                            {
                                List<object> arr = new List<object>();
                                foreach (var ele in (JToken[])fieldValue)
                                {
                                    object fv = DeserializeNode(ele, null, fieldConfig);
                                    arr.Add(fv);
                                }

                                fieldValue = arr.ToArray();
                            }
                        }
                        else if (fieldConfig.FieldType == typeof(string) || fieldConfig.FieldType.IsSimple())
                        {
                            if (fieldValue is JToken[])
                                fieldValue = ((JToken[])fieldValue).FirstOrDefault();

                            if (fieldValue is JToken)
                            {
                                fieldValue = DeserializeNode((JToken)fieldValue, typeof(string) /*fieldConfig.FieldType*/, fieldConfig);
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
                            else if (fieldValue is JToken)
                            {
                                if (!typeof(JToken).IsAssignableFrom(itemType))
                                    fieldValue = DeserializeNode((JToken)fieldValue, itemType, fieldConfig);
                            }
                            else if (fieldValue is JArray)
                            {
                                if (typeof(JArray).IsAssignableFrom(itemType))
                                {

                                }
                                else if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                                {
                                    itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                                    foreach (var ele in (JArray)fieldValue)
                                    {
                                        object fv = DeserializeNode(ele, itemType, fieldConfig);
                                        list.Add(fv);
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    var fi = ((JArray)fieldValue).FirstOrDefault();
                                    fieldValue = DeserializeNode(fi, itemType, fieldConfig);
                                }
                            }
                            else if (fieldValue is JToken[])
                            {
                                itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                                if (typeof(JToken[]).IsAssignableFrom(itemType))
                                {

                                }
                                else if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                                {
                                    var isJArray = ((JToken[])fieldValue).Length == 1 && ((JToken[])fieldValue)[0] is JArray;
                                    var array = isJArray ? ((JArray)((JToken[])fieldValue)[0]).ToArray() : (JToken[])fieldValue;
                                    foreach (var ele in array)
                                    {
                                        object fv = DeserializeNode(ele, itemType, fieldConfig);
                                        list.Add(fv);
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    var fi = ((JToken[])fieldValue).FirstOrDefault();
                                    fieldValue = DeserializeNode(fi, itemType, fieldConfig);
                                }


                                //if (fi is JArray && !itemType.IsCollection())
                                //                     {
                                //                         fieldValue = ToObject(fi, itemType);
                                //	fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
                                //}
                                //else
                                //                     {
                                //                         foreach (var ele in (JToken[])fieldValue)
                                //                         {
                                //		object fv = ToObject(ele, itemType);
                                //		if (fieldConfig.ItemConverter != null)
                                //			fv = fieldConfig.ItemConverter(fv);

                                //		list.Add(fv);
                                //	}
                                //                         fieldValue = list.ToArray();
                                //                     }
                            }
                        }
                    }

                    if (!(fieldValue is ICollection))
                    {
                        if (fieldValue is string)
                            fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string);
                        else if (fieldValue is JValue)
                        {
                            if (((JValue)fieldValue).Value is string)
                                fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue.ToString());
                            else
                                fieldValue = ((JValue)fieldValue).Value;
                        }
                    }

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

            ////Find any object members and serialize them
            //if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            //{

            //}
            //else
            //{
            //    try
            //    {
            //        rec = SerializeObjectMembers(rec);
            //    }
            //    catch { }
            //    rec = AssignDefaultsToNullableMembers(rec);
            //}

            return true;
        }

        private object DeserializeNode(JToken jtoken, Type type, ChoJSONRecordFieldConfiguration config)
        {
            object value = null;
            type = type == null ? fieldConfig.FieldType : type;
            try
            {
                value = ToObject(jtoken, type, config.UseJSONSerialization, config);
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
            if (typeof(JToken).IsAssignableFrom(recordType))
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

            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
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
                            if (typeof(JToken).IsAssignableFrom(itemValue.GetType()))
                            {
                                ChoType.SetPropertyValue(target, pd.Name, ToObject(itemValue as JToken, typeof(ChoDynamicObject)));
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
            if (typeof(JToken).IsAssignableFrom(recordType))
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

            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
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
                            if (typeof(JToken).IsAssignableFrom(itemValue.GetType()))
                            {
                                ChoType.SetPropertyValue(target, pd.Name, ToObject(itemValue as JToken, typeof(ChoDynamicObject)));
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

        private object RaiseItemConverter(ChoJSONRecordFieldConfiguration fieldConfig, object fieldValue)
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

        private bool FillIfKeyValueObject(object rec, JToken jObject)
        {
            if (rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            {
                IDictionary<string, object> dict = ToDynamic(jObject) as IDictionary<string, object>;
                if (dict == null || dict.Count == 0)
                    return true;

                FillIfKeyValueObject(rec, dict.First());
            }
            return false;
        }


        private IList FillIfKeyValueObject(Type type, JToken jObject)
        {
            if (type.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(type))
            {
                IDictionary<string, object> dict = ToDynamic(jObject) as IDictionary<string, object>;
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

        private object ToObject(JToken jToken, Type type, bool? useJSONSerialization = null, ChoJSONRecordFieldConfiguration config = null)
        {
            if (type == null || type.IsDynamicType())
            {
                switch (jToken.Type)
                {
                    case JTokenType.Null:
                        return null;
                    case JTokenType.String:
                        return (string)jToken;
                    case JTokenType.Integer:
                        return (int)jToken;
                    case JTokenType.Float:
                        return (float)jToken;
                    case JTokenType.Date:
                        return (DateTime)jToken;
                    case JTokenType.TimeSpan:
                        return (TimeSpan)jToken;
                    case JTokenType.Guid:
                        return (Guid)jToken;
                    case JTokenType.Object:
                    case JTokenType.Undefined:
                    case JTokenType.Raw:
                        return ToDynamic(jToken);
                    case JTokenType.Uri:
                        return (Uri)jToken;
                    case JTokenType.Array:
                        return ToDynamic(jToken);
                    default:
                        return (string)jToken;
                }

            }
            else
            {
                if (type.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(type))
                {
                    return FillIfKeyValueObject(type, jToken);
                }

                bool lUseJSONSerialization = useJSONSerialization == null ? Configuration.UseJSONSerialization : useJSONSerialization.Value;
                if (true) //lUseJSONSerialization)
                {
                    return JTokenToObject(jToken, type, _se);
                }
                else
                {
                    if (type.GetUnderlyingType().IsSimple())
                    {
                        if (_se == null || _se.Value == null)
                            return jToken.ToObject(type);
                        else
                            return jToken.ToObject(type, _se.Value);
                    }
                    else if (typeof(IDictionary).IsAssignableFrom(type.GetUnderlyingType())
                        || typeof(IList).IsAssignableFrom(type.GetUnderlyingType())
                        )
                    {
                        if (_se == null || _se.Value == null)
                            return jToken.ToObject(type);
                        else
                            return jToken.ToObject(type, _se.Value);
                    }
                    else
                    {
                        try
                        {
                            return DeserializeToObject(type, jToken, config);
                        }
                        catch
                        {
                            if (_se == null || _se.Value == null)
                                return jToken.ToObject(type);
                            else
                                return jToken.ToObject(type, _se.Value);
                        }
                    }
                }
            }
        }

        public object JTokenToObject(JToken jToken, Type objectType, Lazy<JsonSerializer> jsonSerializer = null)
        {
            try
            {
                if (jsonSerializer == null || jsonSerializer.Value == null)
                    return jToken.ToObject(objectType);
                else
                {
                    if (objectType == typeof(ChoCurrency))
                    {
                        var value = jToken.ToObject(typeof(string)) as string;

                        ChoCurrency currency = null;
                        if (ChoCurrency.TryParse(value, out currency))
                            return currency;
                        else
                            throw new ChoParserException($"failed to parse `{value}` currency value.");
                    }
                    else if (objectType == typeof(Decimal))
                    {
                        ChoCurrency currency = null;
                        if (ChoCurrency.TryParse(jToken.ToObject(typeof(string), jsonSerializer.Value) as string, out currency))
                            return currency.Amount;
                        else
                            return jToken.ToObject(objectType, jsonSerializer.Value);
                    }
                    else
                        return jToken.ToObject(objectType, jsonSerializer.Value);
                }
            }
            catch
            {
                if (objectType.IsGenericList())
                {
                    IList list = ChoActivator.CreateInstance(objectType) as IList;

                    Type itemType = objectType.GetItemType();
                    if (jsonSerializer == null || jsonSerializer.Value == null)
                        list.Add(jToken.ToObject(itemType));
                    else
                        list.Add(jToken.ToObject(itemType, jsonSerializer.Value));

                    return list;
                }
                else
                    throw;
            }
        }

        private object DeserializeToObject(Type type, JToken token, ChoJSONRecordFieldConfiguration config = null)
        {
            if (token == null)
                return null;

            object obj = ChoActivator.CreateInstance(type);
            Dictionary<string, string> dict = null;

            dict = new Dictionary<string, string>(token.ToObject<IDictionary<string, object>>().ToDictionary(kvp => kvp.Key, kvp => kvp.Key), StringComparer.CurrentCultureIgnoreCase);

            //if (!Configuration.ContainsRecordConfigForType(type))
            //    Configuration.MapRecordFieldsForType(type);

            //string pn = null;
            //Type propertyType = null;
            //foreach (var cf in Configuration.GetRecordConfigForType(type))
            //{
            //    pn = cf.FieldName.IsNullOrWhiteSpace() ? cf.Name : cf.FieldName;
            //    if (!dict.ContainsKey(pn)) continue;

            //    propertyType = cf.PropertyDescriptor.PropertyType.GetUnderlyingType();

            //}

            //return obj

            string jsonPath = null;
            string jsonPropName = null;
            Type propertyType = null;
            bool? useJsonSerialization = null;
            IEnumerable<PropertyDescriptor> pds = null;

            if (ChoTypeDescriptor.GetProperties(type).Where(pd1 => pd1.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any())
                pds = ChoTypeDescriptor.GetProperties(type).Where(pd1 => pd1.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any());
            else if (ChoTypeDescriptor.GetProperties(type).Where(pd1 => pd1.Attributes.OfType<JsonPropertyAttribute>().Any()).Any())
                pds = ChoTypeDescriptor.GetProperties(type).Where(pd1 => pd1.Attributes.OfType<JsonPropertyAttribute>().Any());
            else
                pds = ChoTypeDescriptor.GetProperties(type);

            foreach (var pd in pds)
            {
                jsonPropName = pd.Name.StartsWith("_") ? pd.Name.Substring(1) : pd.Name;
                if (dict.ContainsKey(jsonPropName))
                    jsonPropName = dict[jsonPropName];

                propertyType = pd.PropertyType.GetUnderlyingType();
                useJsonSerialization = null;

                var fa = pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault();
                if (fa != null)
                {
                    if (!fa.FieldName.IsNullOrWhiteSpace())
                        jsonPropName = fa.FieldName;
                    if (!fa.JSONPath.IsNullOrWhiteSpace())
                        jsonPath = fa.JSONPath;
                    useJsonSerialization = fa.UseJSONSerializationInternal;
                }
                else
                {
                    var fa1 = pd.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                    if (fa1 != null)
                    {
                        if (!fa1.PropertyName.IsNullOrWhiteSpace())
                            jsonPropName = fa1.PropertyName;
                        var jp = pd.Attributes.OfType<ChoJSONPathAttribute>().FirstOrDefault();
                        if (jp != null)
                            jsonPath = jp.JSONPath;
                        var us = pd.Attributes.OfType<ChoUseJSONSerializationAttribute>().FirstOrDefault();
                        if (us != null)
                            useJsonSerialization = true;
                    }
                }
                if (useJsonSerialization == null)
                {
                    ChoUseJSONSerializationAttribute sAttr = pd.Attributes.OfType<ChoUseJSONSerializationAttribute>().FirstOrDefault();
                    if (sAttr != null)
                        useJsonSerialization = true;
                }
                if (propertyType.IsCollection())
                {
                    var nodes = jsonPath != null ? token.SelectTokens(jsonPath) : token[jsonPropName];
                    if (nodes == null)
                        continue;

                    List<object> list = new List<object>();
                    foreach (var node in nodes)
                    {
                        list.Add(ToObject(node, propertyType, useJsonSerialization));
                    }
                    obj.ConvertNSetValue(pd, list.ToArray(), Configuration.Culture);
                }
                else if (jsonPropName != null)
                {
                    var node = jsonPath != null ? token.SelectToken(jsonPath) : token[jsonPropName];
                    if (node == null)
                        continue;

                    obj.ConvertNSetValue(pd, ToObject(node, propertyType, useJsonSerialization), Configuration.Culture);
                }
            }

            return obj;
        }


        private dynamic ToDynamicArray(JArray jArray)
        {
            return jArray.Select(jToken => ToDynamic(jToken)).ToArray();
        }

        private dynamic ToDynamic(JToken jToken)
        {
            if (jToken.Type == JTokenType.Array)
            {
                return ToDynamicArray((JArray)jToken);
            }
            else
            {
                switch (jToken.Type)
                {
                    case JTokenType.Null:
                        return null;
                    case JTokenType.String:
                        return (string)jToken;
                    case JTokenType.Integer:
                        return (int)jToken;
                    case JTokenType.Float:
                        return (float)jToken;
                    case JTokenType.Date:
                        return (DateTime)jToken;
                    case JTokenType.TimeSpan:
                        return (TimeSpan)jToken;
                    case JTokenType.Guid:
                        return (Guid)jToken;
                    case JTokenType.Object:
                    case JTokenType.Undefined:
                    case JTokenType.Raw:
                        Dictionary<string, object> dict = Configuration.JsonSerializer == null ? jToken.ToObject(typeof(Dictionary<string, object>)) as Dictionary<string, object> :
                            jToken.ToObject(typeof(Dictionary<string, object>), Configuration.JsonSerializer) as Dictionary<string, object>;

                        dict = dict.Select(kvp =>
                        {
                            if (kvp.Value is JToken)
                            {
                                var dobj = ToDynamic((JToken)kvp.Value);
                                if (dobj is ChoDynamicObject)
                                    ((ChoDynamicObject)dobj).DynamicObjectName = kvp.Key;
                                return new KeyValuePair<string, object>(kvp.Key, dobj);
                            }
                            else
                                return kvp;
                        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);
                        return new ChoDynamicObject(dict);
                    case JTokenType.Uri:
                        return (Uri)jToken;
                    case JTokenType.Array:
                        return ToDynamic(jToken);
                    default:
                        return (string)jToken;
                }
            }
        }

        private void HandleCollection(JToken[] jTokens, KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp)
        {
            if (false) //typeof(ICollection).IsAssignableFrom(kvp.Value.FieldType) && !kvp.Value.FieldType.IsArray)
            {
                Type itemType = kvp.Value.FieldType.GetItemType();
                IList<object> list = new List<object>();
                foreach (var jt in jTokens)
                    list.Add(jt.ToObject(itemType));

                MethodInfo method = GetType().GetMethod("CloneListAs", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo genericMethod = method.MakeGenericMethod(itemType);
                fieldValue = genericMethod.Invoke(this, new[] { list });
            }
            else
            {
                List<object> list = new List<object>();
                foreach (var jt in jTokens)
                {
                    if (fieldConfig.CustomSerializer != null)
                        list.Add(fieldConfig.CustomSerializer(jt));
                    else
                    {
                        list.Add(ToObject(jt, kvp.Value.FieldType, kvp.Value.UseJSONSerialization));
                    }
                }
                fieldValue = list.ToArray();
            }
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

        private bool? RaiseSkipUntil(Tuple<long, JObject> pair)
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

        private bool? RaiseDoWhile(Tuple<long, JObject> pair)
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

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, JObject> pair)
        {
            if (Reader != null && Reader.HasBeforeRecordLoadSubscribed)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, JObject>(index, state as JObject);

                return retValue;
            }
            else if (_callbackRecordRead != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecordRead.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, JObject>(index, state as JObject);

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, JObject> pair, ref bool skip)
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

        private bool RaiseRecordLoadError(object target, Tuple<long, JObject> pair, Exception ex)
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
