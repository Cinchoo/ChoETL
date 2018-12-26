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
        private IChoNotifyRecordRead _callbackRecord;
        private IChoNotifyRecordFieldRead _callbackFieldRecord;
        private IChoRecordFieldSerializable _callbackRecordSeriablizable;
        private bool _configCheckDone = false;
        private Lazy<JsonSerializer> _se;
        internal ChoReader Reader = null;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONRecordReader(Type recordType, ChoJSONRecordConfiguration configuration) : base(recordType, false)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);
            _callbackFieldRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(recordType);
            if (_callbackFieldRecord == null)
                _callbackFieldRecord = _callbackRecord;
            _callbackRecordSeriablizable = ChoMetadataObjectCache.CreateMetadataObject<IChoRecordFieldSerializable>(recordType);
            if (_callbackRecordSeriablizable == null)
                _callbackRecordSeriablizable = _callbackRecord as IChoRecordFieldSerializable;

            //Configuration.Validate();
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            JsonTextReader sr = source as JsonTextReader;
            ChoGuard.ArgumentNotNull(sr, "JsonTextReader");

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

        private IEnumerable<JObject> ReadJObjects(JsonTextReader sr)
        {
            if (Configuration.JSONPath.IsNullOrWhiteSpace())
            {
                sr.SupportMultipleContent = Configuration.SupportMultipleContent == null ? true : Configuration.SupportMultipleContent.Value;
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
                        }
                    }
                    if (sr.TokenType == JsonToken.StartObject)
                        yield return (JObject)JToken.ReadFrom(sr);
                }
            }
            else
            {
                while (sr.Read())
                {
                    if (sr.TokenType == JsonToken.StartArray)
                    {
                        foreach (var t in ToJObjects(JArray.Load(sr).SelectTokens(Configuration.JSONPath)))
                        {
                            yield return t;
                        }
                    }
                    if (sr.TokenType == JsonToken.StartObject)
                    {
                        foreach (var t in ToJObjects(JObject.Load(sr).SelectTokens(Configuration.JSONPath)))
                        {
                            yield return t;
                        }
                    }
                }
            }
        }
        
        private bool IsKeyValuePairArray(JArray array)
        {
            try
            {
                object key = ((JArray)array).FirstOrDefault();
                object value = ((JArray)array).Skip(1).FirstOrDefault();

                if (key is JValue && value != null)
                    return true;
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
                        foreach (JToken item in ((JArray)t))
                        {
                            if (item is JObject)
                                yield return item.ToObject<JObject>();
                            else if (item is JValue)
                            {
                                dynamic x = new JObject();
                                x.Value = ((JValue)item).Value;
                                yield return x;
                            }
                            else if (item is JArray)
                            {
                                dynamic x = new JObject();
                                x.Value = item;
                                yield return x;
                            }
                        }
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
                            if (skipUntil.Value)
                                skip = skipUntil;
                            else
                                skip = true;
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
                    RaiseMembersDiscovered(dict);
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
            }
            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1);
        }

        private bool LoadNode(Tuple<long, JObject> pair, ref object rec)
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

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }

                if (!Configuration.UseJSONSerialization
                    && !typeof(ICollection).IsAssignableFrom(Configuration.RecordType))
                {
                    if (!FillRecord(rec, pair))
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
                        rec = JsonConvert.DeserializeObject<ExpandoObject>(pair.Item2.ToString(), new ExpandoObjectConverter());
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

        private bool FillRecord(object rec, Tuple<long, JObject> pair)
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
                        else
                            continue;
                    }
                }

                fieldValue = !jTokens.IsNullOrEmpty() ? (object)jTokens : jToken;

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    continue;

                //if (Configuration.IsDynamicObject) //rec is ExpandoObject)
                if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
                {
                }
                else
                {
                    if (pi != null)
                        kvp.Value.FieldType = pi.PropertyType;
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
                            fieldValue = ToObject((JToken)fieldValue, null, fieldConfig.UseJSONSerialization);
                            fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
                        }
                        else if (fieldValue is JToken[])
                        {
                            List<object> arr = new List<object>();
                            foreach (var ele in (JToken[])fieldValue)
                            {
                                object fv = ToObject(ele, null, fieldConfig.UseJSONSerialization);

                                if (fieldConfig.ItemConverter != null)
                                    fv = fieldConfig.ItemConverter(fv);

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
                            try
                            {
                                fieldValue = ToObject((JToken)fieldValue, fieldConfig.FieldType, fieldConfig.UseJSONSerialization);
                            }
                            catch
                            {
                            }
                            fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
                        }
                    }
                    //else if (fieldConfig.FieldType.IsCollection())
                    //{
                    //    List<object> list = new List<object>();
                    //    Type itemType = fieldConfig.FieldType.GetItemType().GetUnderlyingType();

                    //    if (fieldValue is JToken)
                    //    {
                    //        if (fieldConfig.ItemConverter != null)
                    //            fieldValue = fieldConfig.ItemConverter(fieldValue);
                    //        else
                    //            fieldValue = ToObject((JToken)fieldValue, itemType);
                    //    }
                    //    else if (fieldValue is JToken[])
                    //    {
                    //        foreach (var ele in (JToken[])fieldValue)
                    //        {
                    //            if (fieldConfig.ItemConverter != null)
                    //                list.Add(fieldConfig.ItemConverter(ele));
                    //            else
                    //            {
                    //                fieldValue = ToObject(ele, itemType);
                    //            }
                    //        }
                    //        fieldValue = list.ToArray();
                    //    }
                    //}
                    else
                    {
                        List<object> list = new List<object>();
                        Type itemType = fieldConfig.FieldType.GetUnderlyingType();
                        
                        //if (itemType.IsCollectionType())
                        //    itemType = itemType.GetItemType().GetUnderlyingType();

                        if (fieldValue is JToken)
                        {
                            fieldValue = ToObject((JToken)fieldValue, itemType, fieldConfig.UseJSONSerialization);
                            fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
                        }
                        else if (fieldValue is JArray)
                        {
                            if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                            {
                                itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                                foreach (var ele in (JArray)fieldValue)
                                {
                                    object fv = ToObject(ele, itemType, fieldConfig.UseJSONSerialization);
                                    if (fieldConfig.ItemConverter != null)
                                        fv = fieldConfig.ItemConverter(fv);

                                    list.Add(fv);
                                }
                                fieldValue = list.ToArray();
                            }
                            else
                            {
                                var fi = ((JArray)fieldValue).FirstOrDefault();
                                fieldValue = ToObject(fi, itemType, fieldConfig.UseJSONSerialization);
                                fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
                            }
                        }
                        else if (fieldValue is JToken[])
                        {
                            itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();
                            if (fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                            {
                                var isJArray = ((JToken[])fieldValue).Length == 1 && ((JToken[])fieldValue)[0] is JArray;
                                var array = isJArray ? ((JArray)((JToken[])fieldValue)[0]).ToArray() : (JToken[])fieldValue;
                                foreach (var ele in array)
                                {
                                    object fv = ToObject(ele, itemType, fieldConfig.UseJSONSerialization);
                                    if (fieldConfig.ItemConverter != null)
                                        fv = fieldConfig.ItemConverter(fv);

                                    list.Add(fv);
                                }
                                fieldValue = list.ToArray();
                            }
                            else
                            {
                                var fi = ((JToken[])fieldValue).FirstOrDefault();
                                fieldValue = ToObject(fi, itemType, fieldConfig.UseJSONSerialization);
                                fieldValue = RaiseItemConverter(fieldConfig, fieldValue);
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

                try
                {
                    bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
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
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
                                {
                                    if (ex is ValidationException)
                                        throw;

                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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

            //Find any object members and serialize them
            if (!Configuration.SupportsMultiRecordTypes && Configuration.IsDynamicObject)
            {

            }
            else
            {
                rec = SerializeObjectMembers(rec);
                rec = AssignDefaultsToNullableMembers(rec);
            }

            return true;
        }

        private object AssignDefaultsToNullableMembers(object target, bool isTop = true)
        {
            if (target == null)
                return target;

            Type recordType = target.GetType();
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

        private object ToObject(JToken jToken, Type type, bool? useJSONSerialization = null)
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
                    if (_se == null || _se.Value == null)
                        return jToken.ToObject(type);
                    else
                        return jToken.ToObject(type, _se.Value);
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
                            return DeserializeToObject(type, jToken);
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

        private object DeserializeToObject(Type type, JToken token)
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
                Dictionary<string, object> dict = jToken.ToObject(typeof(Dictionary<string, object>)) as Dictionary<string, object>;

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

            ChoFieldValueTrimOption fieldValueTrimOption = config.GetFieldValueTrimOptionForRead(fieldType);

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
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeginLoad(state), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeginLoad(state), true);
            }
            return true;
        }

        private void RaiseEndLoad(object state)
        {
            if (_callbackRecord != null)
            {
                ChoActionEx.RunWithIgnoreError(() => _callbackRecord.EndLoad(state));
            }
            else if (Reader != null)
            {
                ChoActionEx.RunWithIgnoreError(() => Reader.RaiseEndLoad(state));
            }
        }

        private bool? RaiseSkipUntil(Tuple<long, JObject> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackRecord.SkipUntil(index, state));

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseSkipUntil(index, state));

                return retValue;
            }
            return null;
        }

        private bool? RaiseDoWhile(Tuple<long, JObject> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreErrorNullableReturn<bool>(() => _callbackRecord.DoWhile(index, state));

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool? retValue = ChoFuncEx.RunWithIgnoreError<bool?>(() => Reader.RaiseDoWhile(index, state));

                return retValue;
            }
            return null;
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, JObject> pair)
        {
            if (_callbackRecord != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordLoad(target, index, ref state), true);

                if (retValue)
                    pair = new Tuple<long, JObject>(index, state as JObject);

                return retValue;
            }
            else if (Reader != null)
            {
                long index = pair.Item1;
                object state = pair.Item2;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordLoad(target, index, ref state), true);

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
            if (_callbackRecord != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            else if (Reader != null)
            {
                ret = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordLoad(target, pair.Item1, pair.Item2, ref sp), true);
            }
            skip = sp;
            return ret;
        }

        private bool RaiseRecordLoadError(object target, Tuple<long, JObject> pair, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordLoadError(target, pair.Item1, pair.Item2, ex), false);
            }
            return true;
        }

        private bool RaiseBeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (_callbackFieldRecord != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

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
            else if (Reader != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseBeforeRecordFieldLoad(target, index, propName, ref state), true);

                if (retValue)
                    value = state;

                return retValue;
            }
            return true;
        }

        private bool RaiseAfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            if (_callbackFieldRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackFieldRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackFieldRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (target is IChoNotifyRecordFieldRead)
            {
                return ChoFuncEx.RunWithIgnoreError(() => ((IChoNotifyRecordFieldRead)target).RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, value, ex), false);
            }
            return true;
        }

        private bool RaiseRecordFieldDeserialize(object target, long index, string propName, ref object value)
        {
            if (_callbackRecordSeriablizable is IChoRecordFieldSerializable)
            {
                IChoRecordFieldSerializable rec = _callbackRecordSeriablizable as IChoRecordFieldSerializable;
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => rec.RecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            else if (Reader != null && Reader is IChoSerializableWriter)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => ((IChoSerializableReader)Reader).RaiseRecordFieldDeserialize(target, index, propName, ref state), false);

                value = state;

                return retValue;
            }
            return false;
        }

        #endregion Event Raisers
    }
}
