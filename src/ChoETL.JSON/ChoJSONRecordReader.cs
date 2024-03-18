using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
using System.Text.RegularExpressions;
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
            get => Configuration != null ? Configuration.RecordTypeInternal : base.RecordType;
            set
            {
                if (Configuration != null)
                    Configuration.RecordTypeInternal = value;
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
            Configuration.ResetStatesInternal();
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
            bool proceed = true;
            if (sr.TokenType != JsonToken.StartArray && sr.TokenType != JsonToken.StartObject)
                proceed = sr.Read();

            if (proceed) //sr.Read())
            {
                if (sr.TokenType == JsonToken.StartArray)
                {
                    while (sr.Read())
                    {
                        if (sr.TokenType == JsonToken.StartObject)
                        {
                            yield return ToJObject(ChoJObjectLoader.InvokeJObjectLoader(sr, Configuration.JsonLoadSettings, Configuration.JObjectLoadOptions,
                                Configuration.CustomJObjectLoader));
                        }
                        else if (sr.TokenType == JsonToken.StartArray)
                        {
                            var z = ChoJObjectLoader.InvokeJObjectLoader(sr, Configuration.JsonLoadSettings, Configuration.JObjectLoadOptions,
                                Configuration.CustomJObjectLoader).Children().ToArray();
                            dynamic x = new JObject(new JProperty("Value", z));
                            yield return x;
                        }
                        else if (sr.TokenType != JsonToken.EndArray)
                        {
                            if (sr.TokenType == JsonToken.EndObject
                                || sr.TokenType == JsonToken.EndConstructor)
                                continue;
                            else if (sr.TokenType == JsonToken.EndArray)
                                break;

                            dynamic x = null;
                            try
                            {
                                var jt = JToken.Load(sr);
                                if (jt is JProperty)
                                    x = new JObject(jt);
                                else
                                    x = new JObject(new JProperty("Value", jt));
                            }
                            catch { }
                            if (x != null)
                                yield return x;
                        }
                        else
                            break;
                    }
                }
                else if (sr.TokenType == JsonToken.StartObject)
                    yield return ToJObject(ChoJObjectLoader.InvokeJObjectLoader(sr, Configuration.JsonLoadSettings, Configuration.JObjectLoadOptions,
                                Configuration.CustomJObjectLoader));

                sr.Skip();
            }
        }
        private JObject ToJObject(JToken value)
        {
            return ChoJObjectLoader.ToJObject(value);
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
                bool dict1 = false;
                string[] tokens = null;
                if (!Configuration.AllowComplexJSONPath && IsSimpleJSONPath(Configuration.JSONPath, out tokens, out dictKey, out dictValue, out dict1))
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
                                    if (Configuration.FlattenIfJArrayWhenReading)
                                    {
                                        foreach (var j in ToJObjects(new JToken[] { kvp.Value is JToken ? (JToken)kvp.Value : new JValue(kvp.Value) }))
                                            yield return j;
                                    }
                                    else
                                    {
                                        foreach (var j in ToJObjectsDoNotFlattenJArray(new JToken[] { kvp.Value is JToken ? (JToken)kvp.Value : new JValue(kvp.Value) }))
                                            yield return j;
                                    }
                                }
                            }
                        }
                        else if (dict1)
                        {
                            yield return jo;
                        }
                        else
                            yield return jo;
                    }
                }
                else
                {
                    if (!Configuration.AllowComplexJSONPath)
                        throw new JsonException("Complex JSON path not supported.");

                    //IEnumerable<JObject> result = null;
                    //try
                    //{
                    //    result = ToJObjects(JObject.Load(sr).SelectTokens(Configuration.JSONPath));
                    //}
                    //catch
                    //{
                    //    result = ToJObjects(JArray.Load(sr).SelectTokens(Configuration.JSONPath));
                    //}
                    //if (result != null)
                    //{
                    //    foreach (var t in result)
                    //    {
                    //        yield return t;
                    //    }
                    //}

                    IEnumerable<JObject> result = null;
                    try
                    {
                        result = ToJObjectsDoNotFlattenJArray(ChoJObjectLoader.InvokeJObjectLoader(sr, Configuration.JsonLoadSettings, Configuration.JObjectLoadOptions,
                                Configuration.CustomJObjectLoader).SelectTokens(Configuration.JSONPath));
                    }
                    catch
                    {
                        result = ToJObjectsDoNotFlattenJArray(ChoJObjectLoader.InvokeJArrayLoader(sr, Configuration.JsonLoadSettings, Configuration.CustomJArrayLoader,
                            Configuration.UseImplicitJArrayLoader, Configuration.MaxJArrayItemsLoad).SelectTokens(Configuration.JSONPath));
                    }
                    if (result != null)
                    {
                        foreach (var t in result)
                        {
                            yield return t;
                        }
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

        private bool IsSimpleJSONPath(string jsonPath, out string[] tokens, out bool dictKey, out bool dictValue, out bool dict)
        {
            dictKey = false;
            dictValue = false;
            dict = false;
            tokens = null;

            if (jsonPath.StartsWith("$"))
                jsonPath = jsonPath.Substring(1);
            while (jsonPath.StartsWith("."))
                jsonPath = jsonPath.Substring(1);
            if (jsonPath.Length == 0)
                return false;

            var tokens1 = jsonPath.SplitNTrim(".");
            if (tokens1.Length == 1 && tokens1[0].StartsWith("~"))
            {
                tokens = new string[] { tokens1[0].Substring(1).Length == 0 ? "*" : tokens1[0].Substring(1) };
                dictKey = true;
                return true;
            }
            else if (tokens1.Length == 1 && tokens1[0].StartsWith("^"))
            {
                tokens = new string[] { tokens1[0].Substring(1).Length == 0 ? "*" : tokens1[0].Substring(1) };
                dictValue = true;
                return true;
            }
            else if (tokens1.Length == 1 && tokens1[0].StartsWith("*"))
            {
                tokens = new string[] { tokens1[0].Substring(1).Length == 0 ? "*" : tokens1[0].Substring(1) };
                dict = true;
                return true;
            }

            foreach (var token in tokens1)
            {
                if (token.IsNullOrWhiteSpace())
                    return false;
                if (!IsValidIdentifier(token))
                    return false;
            }

            tokens = tokens1;
            return true;
        }
        public bool IsValidIdentifier(string name)
        {
            return Regex.IsMatch(name, @"^([a-zA-Z_])([a-zA-Z_0-9\-:\[\]])*$");
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
                        if (sr.TokenType == JsonToken.StartObject && (sr.Path == elementName || sr.Path.EndsWith($".{elementName}")))
                            return true;
                    }
                }
                else if (sr.TokenType == JsonToken.StartArray)
                {
                    if (sr.Path == elementName || sr.Path.EndsWith($".{elementName}"))
                        return true;

                    while (sr.Read())
                    {
                        if (sr.TokenType == JsonToken.StartObject)
                        {
                            return ReadToFollowing(sr, elementName);
                        }
                    }
                }
                else if (sr.TokenType == JsonToken.PropertyName)
                {
                    if (sr.TokenType == JsonToken.PropertyName && (sr.Path == elementName || sr.Path.EndsWith($".{elementName}")))
                        return true;
                }
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
                    while (ReadToFollowing(sr, elementNames.Skip(elementNames.Length - 1).First()))
                    {
                        foreach (var node in ReadNodes(sr))
                            yield return node;

                        break;
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

        private IEnumerable<JObject> ToJObjectsDoNotFlattenJArray(IEnumerable<JToken> tokens)
        {
            foreach (var t in tokens)
            {
                if (t is JArray)
                {
                    yield return new JObject(new JProperty("Value", t));
                }
                else if (t is JObject)
                    yield return t.ToObject<JObject>();
                else if (t is JValue)
                    yield return new JObject(new JProperty("Value", ((JValue)t).Value));
            }

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
                        if (t is JObject)
                            yield return t.ToObject<JObject>();
                        else if (t is JArray)
                        {
                            foreach (var i in t as JArray)
                            {
                                if (i is JObject)
                                    yield return i.ToObject<JObject>();
                                else
                                {
                                    dynamic x1 = new JObject();
                                    x1.Value = i;
                                    yield return x1;
                                }
                            }
                        }
                        else
                        {
                            dynamic x1 = new JObject();
                            x1.Value = t;
                            yield return x1;
                        }
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

        private IEnumerable<JObject> FlattenNodeIfOn(IEnumerable<JObject> jObjects)
        {
            var arr = _recBuffer.Value.ToArray();
            _recBuffer.Value.Clear();

            foreach (var rec in arr)
                yield return rec;

            if (Configuration.FlattenNode)
            {
                foreach (var jo in jObjects)
                {
                    foreach (var jo1 in jo.Flatten(Configuration.NestedKeySeparator, Configuration.ArrayIndexSeparator,
                        Configuration.NestedKeyResolver, Configuration.UseNestedKeyFormat, Configuration.IgnoreArrayIndex,
                        Configuration.FlattenByNodeName, Configuration.FlattenByJsonPath).OfType<JObject>())
                        yield return (JObject)jo1;
                }
            }
            else
            {
                foreach (var jo in jObjects)
                    yield return jo;
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
            //_se = new Lazy<JsonSerializer>(() => Configuration.JsonSerializerSettings != null ? JsonSerializer.Create(Configuration.JsonSerializerSettings) : null);
            _se = new Lazy<JsonSerializer>(() => Configuration.JsonSerializer);
            List<object> buffer = new List<object>();
            IDictionary<string, Type> recFieldTypes = null;

            if (Configuration.IgnoreFieldValueMode == null)
            {
                if (Configuration.JsonSerializerSettings.NullValueHandling == NullValueHandling.Ignore)
                    Configuration.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null;
            }
            else
            {
                if ((Configuration.IgnoreFieldValueMode | ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null)
                    Configuration.JsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            }

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal && !Configuration.UseJSONSerialization)
            {
                if (Configuration.AutoDiscoverFieldTypes && Configuration.MaxScanRows > 0)
                {
                    List<string> fns = new List<string>();
                    int counter1 = 0;
                    foreach (var obj in FlattenNodeIfOn(jObjects))
                    {
                        if (counter1 > Configuration.MaxScanRows)
                            break;
                        fns = fns.Union(obj.Properties().Select(p => p.Name).ToList()).ToList();
                        _recBuffer.Value.Add(obj);

                        counter1++;
                    }

                    Configuration.ValidateInternal(fns.ToArray());
                }
            }

            foreach (var obj in FlattenNodeIfOn(jObjects))
            {
                if (RecordType == typeof(JObject))
                {
                    yield return obj;
                    continue;
                }

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
                    if (Configuration.SupportsMultiRecordTypes && (Configuration.RecordTypeSelector != null
                        || !Configuration.KnownTypeDiscriminator.IsNullOrWhiteSpace()) && !Configuration.RecordTypeMappedInternal)
                    {
                    }
                    else
                        Configuration.ValidateInternal(pair);
                    var dict = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.Name, i => i.FieldType == null ? null : i.FieldType);
                    //if (Configuration.MaxScanRows == 0)
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

                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal && !Configuration.UseJSONSerialization)
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

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
            {
                if (buffer.Count > 0)
                {
                    Configuration.UpdateFieldTypesIfAny(recFieldTypes);
                    var dict = recFieldTypes = Configuration.JSONRecordFieldConfigurations.ToDictionary(i => i.FieldName, i => i.FieldType == null ? null : i.FieldType);
                    RaiseMembersDiscovered(dict);

                    foreach (object rec1 in buffer)
                        yield return new ChoDynamicObject(MigrateToNewSchema(rec1 as IDictionary<string, object>, recFieldTypes, Configuration.TypeConverterFormatSpec));
                }
            }

            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1, true);
        }

        private void RemoveIgnoreFields(IDictionary<string, object> rec)
        {
            if (rec == null)
                return;

            foreach (var fd in Configuration.IgnoredFields)
            {
                if (rec.ContainsKey(fd))
                    rec.Remove(fd);
            }

            foreach (var kvp in rec)
            {
                if (kvp.Value is IDictionary<string, object>)
                    RemoveIgnoreFields(kvp.Value as IDictionary<string, object>);
                else if (kvp.Value is IList)
                {
                    foreach (var item in (IList)kvp.Value)
                    {
                        RemoveIgnoreFields(item as IDictionary<string, object>);
                    }
                }
            }
        }

        private bool LoadNode(Tuple<long, JObject> pair, ref object rec)
        {
            bool ignoreFieldValue = pair.Item2.IgnoreFieldValue(Configuration.IgnoreFieldValueMode);
            if (ignoreFieldValue)
                return false;
            else if (pair.Item2 == null && !Configuration.IsDynamicObjectInternal)
            {
                rec = RecordType.CreateInstanceAndDefaultToMembers(Configuration.RecordFieldConfigurationsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as ChoRecordFieldConfiguration));
                return true;
            }

            if (Configuration.SupportsMultiRecordTypes && (Configuration.RecordTypeSelector != null || !Configuration.KnownTypeDiscriminator.IsNullOrWhiteSpace()))
            {
                Type recType = null;

                if (!Configuration.KnownTypeDiscriminator.IsNullOrWhiteSpace())
                {
                    if (pair.Item2.ContainsKey(Configuration.KnownTypeDiscriminator))
                    {
                        JValue value = pair.Item2[Configuration.KnownTypeDiscriminator] as JValue;
                        if (value != null && Configuration.KnownTypes != null && Configuration.KnownTypes.ContainsKey(value.ToString()))
                            recType = Configuration.KnownTypes[value.ToString()];
                        else if (Configuration.RecordTypeSelector != null)
                            recType = Configuration.RecordTypeSelector(new Tuple<long, JToken>(pair.Item1, pair.Item2[Configuration.KnownTypeDiscriminator]));
                    }
                }
                else
                {
                    recType = Configuration.RecordTypeSelector(pair);
                }
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
                    Configuration.MapRecordFieldsForType(recType);
                    Configuration.ValidateInternal(null);
                }

                if (recType != null)
                {
                    rec = recType.IsDynamicType() ? new ChoDynamicObject()
                    {
                        ThrowExceptionIfPropNotExists = Configuration.ThrowExceptionIfDynamicPropNotExists == null ? ChoDynamicObjectSettings.ThrowExceptionIfPropNotExists : Configuration.ThrowExceptionIfDynamicPropNotExists.Value,
                    } : ChoActivator.CreateInstance(recType);
                    RecordType = recType;
                }
            }
            else if (!Configuration.UseJSONSerialization || Configuration.IsDynamicObjectInternal)
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
                if (Configuration.CustomNodeSelecter != null)
                {
                    pair = new Tuple<long, JObject>(pair.Item1, Configuration.CustomNodeSelecter(pair.Item2));
                }
                //else if (Configuration.NodeConvertersForType.ContainsKey(RecordType) && Configuration.NodeConvertersForType[RecordType] != null)
                //{
                //    pair = new Tuple<long, JObject>(pair.Item1, Configuration.NodeConvertersForType[RecordType](pair.Item2) as JObject);
                //}

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                bool hasTypeConverter = false;
                if (ChoTypeConverter.Global.Contains(rec.GetNType()))
                {
                    var tc = ChoTypeConverter.Global.GetConverter(rec.GetNType());
                    if (tc != null)
                    {
                        hasTypeConverter = true;

                        var JObject = pair.Item2 as JObject;
                        if (JObject != null && JObject.Properties().Count() == 1 && JObject.ContainsKey("Value"))
                        {
                            if (Configuration.SourceTypeInternal != null)
                            {
                                rec = ChoConvert.ConvertFrom(JsonConvert.DeserializeObject(JObject["Value"].ToString(), Configuration.SourceTypeInternal, Configuration.JsonSerializerSettings), Configuration.RecordTypeInternal,
                                    null, new object[] { tc }, culture: Configuration.Culture, config: Configuration);
                            }
                            else
                            {
                                rec = ChoConvert.ConvertFrom(JObject["Value"], Configuration.RecordTypeInternal, null, new object[] { tc },
                                    culture: Configuration.Culture, config: Configuration);
                            }
                        }
                        else
                        {
                            if (Configuration.SourceTypeInternal != null)
                                rec = ChoConvert.ConvertFrom(JsonConvert.DeserializeObject(JObject.ToString(), Configuration.SourceTypeInternal, Configuration.JsonSerializerSettings), Configuration.RecordTypeInternal,
                                    null, new object[] { tc }, culture: Configuration.Culture, config: Configuration);
                            else
                                rec = ChoConvert.ConvertFrom(JObject, Configuration.RecordTypeInternal,
                                    null, new object[] { tc }, culture: Configuration.Culture, config: Configuration);
                        }

                        return true;
                    }
                }

                if (!hasTypeConverter && !Configuration.UseJSONSerialization
                    && !typeof(ICollection).IsAssignableFrom(RecordType)
                    && !(RecordType.IsGenericType && RecordType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    )
                {
                    if (Configuration.SupportsMultiRecordTypes)
                    {
                        rec = DeserializeNode((JToken)pair.Item2, RecordType, null);
                    }
                    else
                    {
                        if (!FillRecord(ref rec, pair))
                            return false;
                    }

                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                        if (Configuration.ConvertToNestedObject && Configuration.NestedKeySeparator != null)
                        {
                            rec = rec.ConvertToNestedObject(Configuration.NestedKeySeparator.Value, Configuration.ArrayIndexSeparator,
                                allowNestedConversion: Configuration.AllowNestedConversion, maxArraySize: Configuration.MaxNestedConversionArraySize);
                        }
                        else if (Configuration.ConvertToFlattenObject && Configuration.NestedKeySeparator != null)
                        {
                            rec = rec.ConvertToFlattenObject(Configuration.NestedKeySeparator.Value, Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator,
                                Configuration.IgnoreDictionaryFieldPrefix);
                        }
                    }
                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);
                }
                else
                {
                    Reader.ContractResolverState = new ChoContractResolverState
                    {
                        Index = pair.Item1,
                        Record = ChoActivator.CreateInstanceNCache(RecordType),
                    };

                    //rec = _se.Value != null ? pair.Item2.ToObject(RecordType, _se.Value) : pair.Item2.ToObject(RecordType);
                    //if (Configuration.IsDynamicObject)
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                        if (pair.Item2 is IDictionary)
                            rec = pair.Item2;
                        else
                            rec = JsonConvert.DeserializeObject<ExpandoObject>(pair.Item2.ToString(), Configuration.JsonSerializerSettings);

                        //Remove any ignore fields
                        if (Configuration.IgnoredFields.Count != 0)
                        {
                            RemoveIgnoreFields(rec as IDictionary<string, object>);
                        }

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

        public IEnumerable<JToken> SelectTokens(JObject node, string path)
        {
            bool dictKey = false;
            bool dictValue = false;
            bool dict1 = false;
            string[] tokens = null;
            if (IsSimpleJSONPath(path, out tokens, out dictKey, out dictValue, out dict1))
            {
                if (dictKey)
                {
                    var match = tokens.FirstOrDefault();
                    foreach (var dict in ToCollections(node))
                    {
                        var input = tokens.FirstOrDefault();
                        var regex = new Regex(ChoWildcard.WildcardToRegex(input), RegexOptions.Compiled);
                        foreach (var kvp in dict)
                        {
                            if (input == "*")
                            {
                                foreach (var j in new JToken[] { new JValue(kvp.Key) })
                                    yield return j;
                            }
                            else if (regex.IsMatch(kvp.Key))
                            {
                                foreach (var j in new JToken[] { new JValue(kvp.Key) })
                                    yield return j;
                            }
                        }
                    }
                }
                else if (dictValue)
                {
                    var input = tokens.FirstOrDefault();
                    var regex = new Regex(ChoWildcard.WildcardToRegex(input), RegexOptions.Compiled);
                    foreach (var dict in ToCollections(node))
                    {
                        foreach (var kvp in dict)
                        {
                            if (input == "*")
                            {
                                foreach (var j in ToJObjects(new JToken[] { kvp.Value is JToken ? (JToken)kvp.Value : new JValue(kvp.Value) }))
                                    yield return j;
                            }
                            else if (regex.IsMatch(kvp.Key))
                            {
                                foreach (var j in ToJObjects(new JToken[] { kvp.Value is JToken ? (JToken)kvp.Value : new JValue(kvp.Value) }))
                                    yield return j;
                            }
                        }
                    }
                }
                else if (dict1)
                {
                    yield return node;
                }
                else
                {
                    foreach (var n in node.SelectTokens(path))
                        yield return n;
                }
            }
            else
            {
                foreach (var n in node.SelectTokens(path))
                    yield return n;
            }
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

            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
            {

            }
            else
            {
                if (rec.FillIfCustomSerialization(pair.Item2))
                    return true;

                if (FillIfKeyValueObject(ref rec, pair.Item2))
                    return true;
            }
                                
            Func<object, object> nodeConverterForType = null;
            if (Configuration.HasNodeConverterForType(RecordType, out nodeConverterForType)
                        || Configuration.HasNodeConverterForType(RecordType.GetUnderlyingType(), out nodeConverterForType))
            {
                if (nodeConverterForType != null)
                {
                    rec = nodeConverterForType(pair.Item2);
                    return true;
                }
            }

            object rootRec = rec;
            foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                {
                    if (Configuration.IgnoredFields.Contains(kvp.Key))
                        continue;
                }

                jToken = null;
                jTokens = null;
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

                //fieldValue = dictValues[kvp.Key];
                if (!kvp.Value.JSONPath.IsNullOrWhiteSpace() && kvp.Value.JSONPath != kvp.Value.FieldName)
                {
                    jTokens = SelectTokens(node, kvp.Value.JSONPath).ToArray();
                    if (fieldConfig.FieldType != null && fieldConfig.FieldType != typeof(object) && !fieldConfig.FieldType.IsCollection()
                        && !fieldConfig.FieldType.IsGenericList()
                        && !fieldConfig.FieldType.IsGenericEnumerable()
                        && jToken != null)
                    {
                        jToken = jTokens.FirstOrDefault();
                        jTokens = null;
                    }

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

                    if (fieldConfig.FieldType != null && fieldConfig.FieldType != typeof(object) && !fieldConfig.FieldType.IsCollection()
                        && !fieldConfig.FieldType.IsGenericList()
                        && !fieldConfig.FieldType.IsGenericEnumerable()
                        && jToken != null)
                    {
                        jToken = jToken is JArray ? ((JArray)jToken).FirstOrDefault() : jToken;
                    }
                    else if (fieldConfig.FieldType != null && fieldConfig.FieldType != typeof(object) &&
                        !typeof(JToken).IsAssignableFrom(fieldConfig.FieldType) &&
                        (fieldConfig.FieldType.IsCollection()
                         || fieldConfig.FieldType.IsGenericList()
                         || fieldConfig.FieldType.IsGenericEnumerable())
                         && jToken != null)
                    {
                        var itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                        if (itemType.IsSimple())
                        {

                        }
                        else
                            jTokens = new JToken[] { jToken };
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
                    if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObjectInternal)
                    {
                    }
                    else
                    {
                        if (pi != null)
                        {
                            if (kvp.Value.FieldTypeSelector != null)
                            {
                                Type rt = null;
                                JObject jObj = pair.Item2;
                                if (!kvp.Value.FieldTypeDiscriminator.IsNullOrWhiteSpace() && jObj.ContainsKey(kvp.Value.FieldTypeDiscriminator))
                                {
                                    rt = kvp.Value.FieldTypeSelector(jObj[kvp.Value.FieldTypeDiscriminator]);
                                }
                                else
                                    rt = kvp.Value.FieldTypeSelector(pair.Item2);
                                kvp.Value.FieldType = rt == null ? pi.PropertyType : rt;
                            }
                            else
                                kvp.Value.FieldType = pi.PropertyType;
                        }
                        else
                            kvp.Value.FieldType = typeof(string);
                    }

                    object v1 = !jTokens.IsNullOrEmpty() ? (object)jTokens : jToken == null ? node : jToken;
                    nodeConverterForType = null;
                    if (fieldConfig.CustomSerializer != null)
                        fieldValue = fieldConfig.CustomSerializer(v1);
                    else if (RaiseRecordFieldDeserialize(rec, pair.Item1, kvp.Key, ref v1))
                        fieldValue = v1;
                    else if (fieldConfig.PropCustomSerializer != null)
                        fieldValue = ChoCustomSerializer.Deserialize(v1, fieldConfig.FieldType, fieldConfig.PropCustomSerializer, fieldConfig.PropCustomSerializerParams, Configuration.Culture, fieldConfig.Name);
                    else if (Configuration.HasNodeConverterForType(fieldConfig.FieldType, out nodeConverterForType)
                        || Configuration.HasNodeConverterForType(fieldConfig.FieldType.GetUnderlyingType(), out nodeConverterForType))
                    {
                        if (nodeConverterForType != null)
                            fieldValue = nodeConverterForType(fieldValue);
                        else
                            fieldValue = null;
                    }
                    else
                    {
                        if (fieldConfig.FieldType == null)
                        {
                            var isArray = Configuration.IsArray(fieldConfig, fieldValue);
                            if (isArray != null && !isArray.Value)
                            {
                                if (fieldValue is JToken[])
                                {
                                    fieldValue = ((JToken[])fieldValue).FirstOrDefault();
                                    if (fieldValue is JArray)
                                    {
                                        fieldValue = ((JArray)fieldValue).FirstOrDefault();
                                    }
                                }
                                else if (fieldValue is JArray)
                                {
                                    fieldValue = ((JArray)fieldValue).FirstOrDefault();
                                }
                            }
                        }
                        else
                        {
                            if (fieldConfig.FieldType != null && fieldConfig.FieldType != typeof(object) && !fieldConfig.FieldType.IsCollection() && !fieldConfig.FieldType.IsGenericList()
                                && !fieldConfig.FieldType.IsGenericEnumerable() && fieldValue is JToken[])
                            {
                                if (!fieldConfig.HasConvertersInternal())
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
                            {
                                if (!fieldConfig.HasConvertersInternal())
                                    fieldValue = ((JToken[])fieldValue).FirstOrDefault();
                            }

                            if (fieldValue is JToken)
                            {
                                fieldValue = DeserializeNode((JToken)fieldValue, fieldConfig.FieldType, fieldConfig);
                            }
                        }
                        else if (fieldConfig.SourceType != null || !fieldConfig.HasConvertersInternal())
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
                                if (fieldConfig.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                                {
                                    var rt = RaiseRecordTypeSelector(fieldConfig, fieldValue);
                                    if (rt != null)
                                        itemType = rt;
                                }

                                if (!typeof(JToken).IsAssignableFrom(itemType))
                                {
                                    var jToken1 = (JToken)fieldValue;
                                    fieldValue = DeserializeNode(jToken1, itemType, fieldConfig);
                                }
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
                                        if (fieldConfig.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                                        {
                                            var rt = RaiseRecordTypeSelector(fieldConfig, ele);
                                            if (rt != null)
                                                itemType = rt;
                                        }
                                        object fv = DeserializeNode(ele, itemType, fieldConfig);
                                        list.Add(fv);
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    var fi = ((JArray)fieldValue).FirstOrDefault();
                                    if (fieldConfig.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                                    {
                                        var rt = RaiseRecordTypeSelector(fieldConfig, fi);
                                        if (rt != null)
                                            itemType = rt;
                                    }
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
                                        if (fieldConfig.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                                        {
                                            var rt = RaiseRecordTypeSelector(fieldConfig, ele);
                                            if (rt != null)
                                                itemType = rt;
                                        }

                                        try
                                        {
                                            object fv = null;
                                            if (fieldConfig.ItemConverter != null || typeof(IChoItemConvertable).IsAssignableFrom(RecordType))
                                                fv = RaiseItemConverter(fieldConfig, ele);
                                            else
                                                fv = DeserializeNode(ele, itemType, fieldConfig);

                                            if (!fieldConfig.HasConvertersInternal() && !itemType.IsCollectionType() && fv is IList && !(fv is JToken))
                                                list.AddRange(((IList)fv).OfType<object>());
                                            else
                                                list.Add(fv);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                                            {
                                                ChoETLFramework.WriteLog(TraceSwitch.TraceError, "Error [{0}] found. Ignoring item in the field...".FormatString(ex.Message));
                                                continue;
                                            }
                                        }
                                    }
                                    fieldValue = list.ToArray();
                                }
                                else
                                {
                                    var fi = ((JToken[])fieldValue).FirstOrDefault();
                                    if (fieldConfig.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                                    {
                                        var rt = RaiseRecordTypeSelector(fieldConfig, fi);
                                        if (rt != null)
                                            itemType = rt;
                                    }
                                    fieldValue = DeserializeNode(fi, itemType, fieldConfig);
                                }
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

                    if (fieldConfig.IgnoreFieldValueMode == null)
                    {
                        if (fieldValue.IsObjectNullOrEmpty() && fieldConfig.IsDefaultValueSpecifiedInternal)
                            fieldValue = fieldConfig.DefaultValue;
                    }
                    else
                    {
                        bool ignoreFieldValue = fieldValue.IgnoreFieldValue(fieldConfig.IgnoreFieldValueMode);
                        if (ignoreFieldValue && fieldConfig.IsDefaultValueSpecifiedInternal)
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
                            fieldConfig.PIInternal = pi;
                            fieldConfig.PropConvertersInternal = ChoTypeDescriptor.GetTypeConverters(fieldConfig.PIInternal);
                            fieldConfig.PropConverterParamsInternal = ChoTypeDescriptor.GetTypeConverterParams(fieldConfig.PIInternal);

                            //Load Custom Serializer
                            fieldConfig.PropCustomSerializer = ChoTypeDescriptor.GetCustomSerializer(fieldConfig.PIInternal);
                            fieldConfig.PropCustomSerializerParams = ChoTypeDescriptor.GetCustomSerializerParams(fieldConfig.PIInternal);
                        }

                        if (pi != null)
                            rec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture, config: Configuration);
                        else if (RecordType.IsSimple())
                            rec = ChoConvert.ConvertFrom(fieldValue, RecordType, Configuration.Culture, config: Configuration);
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

            if (Configuration.RecordFieldConfigurationsDict.Count == 1 && Configuration.RecordFieldConfigurationsDict.First().Key == "Value"
                && !Configuration.IsDynamicObjectInternal)
            {
                if (RecordType.IsSimple())
                {

                }
                else
                    rec = rootRec;
            }
            else
            {
                rec = rootRec;
            }

            if (rec is ChoDynamicObject dobj)
            {
                if (Configuration.NullValueHandling == ChoNullValueHandling.Ignore)
                    rec = dobj.IgnoreNullValues();
            }

            return true;
        }

        private object DeserializeNode(JToken jtoken, Type type, ChoJSONRecordFieldConfiguration config)
        {
            object value = null;
            type = type == null ? (config != null && config.FieldType == null ? null /*typeof(string)*/ : config.FieldType) : type;

            if (config != null && (config.ItemRecordTypeSelector != null || typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType)))
            {
                var rt = RaiseRecordTypeSelector(config, jtoken);
                if (rt != null)
                    type = rt;
            }

            try
            {
                value = ToObject(jtoken, type, config != null ? config.UseJSONSerialization : false, config);
            }
            catch
            {
                if (config != null && (config.ItemConverter != null || typeof(IChoItemConvertable).IsAssignableFrom(RecordType)))
                    value = RaiseItemConverter(config, jtoken);
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
                        var fv = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, propConverters, propConverterParams, 
                            Configuration.Culture, config: Configuration);
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
                        var fv = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, propConverters, propConverterParams, 
                            Configuration.Culture, config: Configuration);
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
                //if (fieldValue is IList)
                //{
                //    fieldValue = ((IList)fieldValue).Cast(fieldConfig.ItemConverter);
                //}
                //else
                fieldValue = fieldConfig.ItemConverter(fieldValue);
            }
            else
            {
                if (typeof(IChoItemConvertable).IsAssignableFrom(RecordType))
                {
                    var rec = ChoActivator.CreateInstanceNCache(RecordType);
                    if (rec is IChoItemConvertable)
                        fieldValue = ((IChoItemConvertable)rec).ItemConvert(fieldConfig.Name, fieldValue);
                }
            }

            return fieldValue;
        }

        private Type RaiseRecordTypeSelector(ChoJSONRecordFieldConfiguration fieldConfig, object fieldValue)
        {
            if (fieldConfig.ItemRecordTypeSelector != null)
            {
                if (!fieldConfig.ItemTypeDiscriminator.IsNullOrWhiteSpace() && fieldValue is JObject && ((JObject)fieldValue).ContainsKey(fieldConfig.ItemTypeDiscriminator))
                    fieldValue = ((JObject)fieldValue)[fieldConfig.ItemTypeDiscriminator];

                return fieldConfig.ItemRecordTypeSelector(fieldValue);
            }
            else
            {
                if (typeof(IChoRecordTypeSelector).IsAssignableFrom(RecordType))
                {
                    var rec = ChoActivator.CreateInstanceNCache(RecordType);
                    if (rec is IChoRecordTypeSelector)
                    {
                        if (!fieldConfig.FieldTypeDiscriminator.IsNullOrWhiteSpace() && fieldValue is JObject && ((JObject)fieldValue).ContainsKey(fieldConfig.FieldTypeDiscriminator))
                            fieldValue = ((JObject)fieldValue)[fieldConfig.FieldTypeDiscriminator];
                        return ((IChoRecordTypeSelector)rec).SelectRecordType(fieldConfig.Name, fieldValue);
                    }
                }
            }

            return null;
        }

        private bool FillIfKeyValueObject(ref object rec, JToken jObject)
        {
            if (rec == null)
                return false;
            //if (rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
            //    || typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            //{
            //    rec = ToObject(jObject, RecordType); //, config.UseJSONSerialization, config);
            //    return true;
            //}
            //else
            //    return false;

            if (rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            {
                IDictionary<string, object> dict = ToDynamic(jObject) as IDictionary<string, object>;
                if (dict == null || dict.Count == 0)
                    return true;

                FillIfKeyValueObject(rec, dict.First());
                return true;
            }
            return false;
        }


        private IEnumerable<object> FillIfKeyValueObject(JToken jObject, Type recType)
        {
            //if (rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
            //    || typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            //{
            //    rec = ToObject(jObject, RecordType); //, config.UseJSONSerialization, config);
            //    return true;
            //}
            //else
            //    return false;

            if (recType.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                || typeof(IChoKeyValueType).IsAssignableFrom(recType))
            {
                IDictionary<string, object> dict = ToDynamic(jObject) as IDictionary<string, object>;
                if (dict == null || dict.Count == 0)
                    yield break;

                foreach (var kvp in dict)
                {
                    object rec = ChoActivator.CreateInstance(recType);
                    FillIfKeyValueObject(rec, kvp);
                    yield return rec;
                }
            }
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
            if (type == null || (type.IsDynamicType() && useJSONSerialization == null))
            {
                switch (jToken.Type)
                {
                    case JTokenType.Null:
                        return null;
                    case JTokenType.Boolean:
                        return (bool)jToken;
                    case JTokenType.String:
                        return (string)jToken;
                    case JTokenType.Integer:
                        return (long)jToken;
                    case JTokenType.Float:
                        return (double)jToken;
                    case JTokenType.Date:
                        if (Configuration.JsonSerializerSettings.DateParseHandling == DateParseHandling.DateTimeOffset)
                            return (DateTimeOffset)jToken;
                        else if (Configuration.JsonSerializerSettings.DateParseHandling == DateParseHandling.DateTime)
                            return (DateTime)jToken;
                        else
                            return (string)jToken;
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
                //if (type.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null
                //|| typeof(IChoKeyValueType).IsAssignableFrom(type))
                //{
                //    return FillIfKeyValueObject(type, jToken);
                //}

                IContractResolver contractResolver = config != null ? config.ContractResolver : null;
                var savedContractResolver = _se.Value.ContractResolver;
                try
                {
                    if (contractResolver != null)
                        _se.Value.ContractResolver = contractResolver;

                    bool lUseJSONSerialization = useJSONSerialization == null ? Configuration.UseJSONSerialization : useJSONSerialization.Value;
                    if (true) //lUseJSONSerialization)
                    {
                        if (config != null)
                        {
                            //if (config.HasConverters())
                            //    type = config.SourceType != null ? config.SourceType : typeof(object);
                            //else
                                type = config.SourceType != null ? config.SourceType : type;
                        }

                        return JTokenToObject(jToken, type, _se, config);
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
                finally
                {
                    if (contractResolver != null)
                        _se.Value.ContractResolver = savedContractResolver;
                }
            }
        }

        public object JTokenToObject(JToken jToken, Type objectType, Lazy<JsonSerializer> jsonSerializer = null, ChoJSONRecordFieldConfiguration fieldConfig = null)
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
                    else if (typeof(IChoKeyValueType).IsAssignableFrom(objectType))
                    {
                        var recs = FillIfKeyValueObject(jToken, objectType).ToArray();
                        return recs;
                    }
                    else
                    {
                        var JObject = jToken as JObject;

                        bool disableImplcityOp = false;
                        if (ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(objectType) != null)
                            disableImplcityOp = ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(objectType).Flag;
                        else
                            disableImplcityOp = ChoTypeDescriptor.IsTurnedOffImplicitOpsOnType(objectType);

                        if (!disableImplcityOp)
                        {
                            if (jToken is JToken)
                            {
                                var castTypes = objectType.GetImplicitTypeCastOps();

                                foreach (var ct in castTypes)
                                {
                                    try
                                    {
                                        return jToken.ToObject(ct);
                                    }
                                    catch { }
                                }
                            }
                        }

                        if (JObject != null && JObject.Properties().Count() == 1 && JObject.ContainsKey("Value"))
                        {
                            return ChoUtility.CastObjectTo(JObject["Value"], objectType);
                        }
                        else if (jToken is JValue)
                        {
                            var value = jToken as JValue;

                            try
                            {
                                object retval = value.Value;
                                //var retval = ChoConvert.ConvertFrom(value.Value, objectType, 
                                //    parameters: fieldConfig != null && !fieldConfig.FormatText.IsNullOrWhiteSpace() ? new object[] { fieldConfig.FormatText } : null);

                                if (ChoETLRecordHelper.ConvertMemberValue(null, null, fieldConfig, ref retval, Configuration.Culture))
                                {

                                }
                                if (retval == null && objectType.IsNullableType())
                                {
                                    return jToken.ToObject(objectType, jsonSerializer.Value);
                                }
                                else
                                    return retval;
                            }
                            catch
                            {
                                return jToken.ToObject(objectType, jsonSerializer.Value);
                            }
                        }
                        else
                            return jToken.ToObject(objectType, jsonSerializer.Value);
                    }
                }
            }
            catch (Exception outerEx)
            {
                try
                {
                    if (objectType.IsGenericList() || objectType.IsGenericEnumerable())
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
                        return jToken.ToObject(typeof(string), jsonSerializer.Value);
                }
                catch
                {
                    throw outerEx;
                }
                //                throw;
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
                        return (long)jToken;
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
                        IDictionary<string, object> dict = Configuration.JsonSerializer == null ? jToken.ToObject(typeof(ChoDynamicObject)) as Dictionary<string, object> :
                            jToken.ToObject(typeof(ChoDynamicObject), Configuration.JsonSerializer) as IDictionary<string, object>;

                        if (jToken is JObject && ((JObject)jToken).ContainsKey("$type"))
                        {
                            dict.Add("$type", ((JObject)jToken)["$type"]);
                        }
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

        //private void HandleCollection(JToken[] jTokens, KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp)
        //{
        //    if (false) //typeof(ICollection).IsAssignableFrom(kvp.Value.FieldType) && !kvp.Value.FieldType.IsArray)
        //    {
        //        Type itemType = kvp.Value.FieldType.GetItemType();
        //        IList<object> list = new List<object>();
        //        foreach (var jt in jTokens)
        //            list.Add(jt.ToObject(itemType));

        //        MethodInfo method = GetType().GetMethod("CloneListAs", BindingFlags.NonPublic | BindingFlags.Instance);
        //        MethodInfo genericMethod = method.MakeGenericMethod(itemType);
        //        fieldValue = genericMethod.Invoke(this, new[] { list });
        //    }
        //    else
        //    {
        //        List<object> list = new List<object>();
        //        foreach (var jt in jTokens)
        //        {
        //            if (fieldConfig.CustomSerializer != null)
        //                list.Add(fieldConfig.CustomSerializer(jt));
        //            else
        //            {
        //                list.Add(ToObject(jt, kvp.Value.FieldType, kvp.Value.UseJSONSerialization));
        //            }
        //        }
        //        fieldValue = list.ToArray();
        //    }
        //}

        private List<T> CloneListAs<T>(IList<object> source)
        {
            // Here we can do anything we want with T
            // T == source[0].GetType()
            return source.Cast<T>().ToList();
        }
        private string CleanFieldValue(ChoJSONRecordFieldConfiguration config, Type fieldType, string fieldValue)
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
