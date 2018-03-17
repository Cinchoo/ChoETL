using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool _configCheckDone = false;
        private Lazy<JsonSerializer> _se;
        internal ChoReader Reader = null;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONRecordReader(Type recordType, ChoJSONRecordConfiguration configuration) : base(recordType)
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;

            _callbackRecord = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(recordType);

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
                        }
                    }
                    if (sr.TokenType == JsonToken.StartObject)
                        yield return (JObject)JToken.ReadFrom(sr);
                }
            }
            else
            {
                foreach (var t in ToJObjects(JObject.Load(sr).SelectTokens(Configuration.JSONPath)))
                {
                    yield return t;
                }
            }
        }

        private IEnumerable<JObject> ToJObjects(IEnumerable<JToken> tokens)
        {
            foreach (var t in tokens)
            {
                if (t is JArray)
                {
                    foreach (JToken item in ((JArray)t))
                        yield return item.ToObject<JObject>();
                }
                else if (t is JObject)
                    yield return t.ToObject<JObject>();
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
            if (!Configuration.UseJSONSerialization)
                rec = Configuration.IsDynamicObject ? new ChoDynamicObject() { ThrowExceptionIfPropNotExists = true } : Activator.CreateInstance(RecordType);

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

                if (!Configuration.UseJSONSerialization)
                {
                    if (!FillRecord(rec, pair))
                        return false;

                    if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
                        rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);
                }
                else
                {
                    //rec = _se.Value != null ? pair.Item2.ToObject(RecordType, _se.Value) : pair.Item2.ToObject(RecordType);
                    if (Configuration.IsDynamicObject)
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
                    throw;

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

            if (!Configuration.IsDynamicObject)
            {
                if (rec.FillIfCustomSerialization(pair.Item2))
                    return true;

                if (FillIfKeyValueObject(rec, pair))
                    return true;
            }

            object rootRec = rec;
			foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

				rec = GetDeclaringRecord(kvp.Value.DeclaringMember, rootRec);

				//fieldValue = dictValues[kvp.Key];
				if (!kvp.Value.JSONPath.IsNullOrWhiteSpace())
                {
                    jTokens = node.SelectTokens(kvp.Value.JSONPath).ToArray();
                    jToken = jTokens.FirstOrDefault();
                    if (jToken == null)
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                    }
                }
                else
                {
                    if (!node.TryGetValue(kvp.Key, StringComparison.CurrentCultureIgnoreCase, out jToken))
                    {
                        if (Configuration.ColumnCountStrict)
                            throw new ChoParserException("No matching '{0}' field found.".FormatString(fieldConfig.FieldName));
                    }
                }

                fieldValue = !jTokens.IsNullOrEmpty() ? (object)jTokens : jToken;

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    continue;

                if (Configuration.IsDynamicObject) //rec is ExpandoObject)
                {
                }
                else
                {
                    if (pi != null)
                        kvp.Value.FieldType = pi.PropertyType;
                    else
                        kvp.Value.FieldType = typeof(string);
                }


                if (fieldConfig.ValueConverter != null)
                    fieldValue = fieldConfig.ValueConverter(fieldValue);
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
                            if (fieldConfig.ItemConverter != null)
                                fieldValue = fieldConfig.ItemConverter(fieldValue);
                            else
                                fieldValue = ToObject((JToken)fieldValue, null);
                        }
                        else if (fieldValue is JToken[])
                        {
                            List<object> arr = new List<object>();
                            foreach (var ele in (JToken[])fieldValue)
                            {
                                if (fieldConfig.ItemConverter != null)
                                    arr.Add(fieldConfig.ItemConverter(ele));
                                else
                                    arr.Add(ToObject(ele, null));
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
                            if (fieldConfig.ItemConverter != null)
                                fieldValue = fieldConfig.ItemConverter(fieldValue);
                            else
                                fieldValue = ToObject((JToken)fieldValue, fieldConfig.FieldType);
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
                        Type itemType = fieldConfig.FieldType.GetUnderlyingType().GetItemType().GetUnderlyingType();

                        if (fieldValue is JToken)
                        {
                            if (fieldConfig.ItemConverter != null)
                                fieldValue = fieldConfig.ItemConverter(fieldValue);
                            else
                                fieldValue = ToObject((JToken)fieldValue, itemType);
                        }
                        else if (fieldValue is JToken[])
                        {
                            var fi = ((JToken[])fieldValue).FirstOrDefault();

                            if (fi is JArray && !fieldConfig.FieldType.GetUnderlyingType().IsCollection())
                            {
                                if (fieldConfig.ItemConverter != null)
                                    fieldValue = fieldConfig.ItemConverter(fi);
                                else
                                {
                                    fieldValue = ToObject(fi, fieldConfig.FieldType);
                                }
                            }
                            else
                            {
                                foreach (var ele in (JToken[])fieldValue)
                                {
                                    if (fieldConfig.ItemConverter != null)
                                        list.Add(fieldConfig.ItemConverter(ele));
                                    else
                                    {
                                        list.Add(ToObject(ele, itemType));
                                    }
                                }
                                fieldValue = list.ToArray();
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

                try
                {
                    bool ignoreFieldValue = fieldConfig.IgnoreFieldValue(fieldValue);
                    if (ignoreFieldValue)
                        fieldValue = fieldConfig.IsDefaultValueSpecified ? fieldConfig.DefaultValue : null;

                    if (Configuration.IsDynamicObject)
                    {
                        var dict = rec as IDictionary<string, Object>;

                        dict.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);

                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                            dict.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                    }
                    else
                    {
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
                        if (Configuration.IsDynamicObject)
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

            return true;
        }

        private bool FillIfKeyValueObject(object rec, Tuple<long, JObject> pair)
        {
            IDictionary<string, object> dict = ToDynamic(pair.Item2) as IDictionary<string, object>;
            if (dict.Count == 0)
                return true;

            var isKVPAttrDefined = rec.GetType().GetCustomAttribute<ChoKeyValueTypeAttribute>() != null;

            if (isKVPAttrDefined)
            {
                var kP = rec.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoKeyAttribute>() != null).FirstOrDefault();
                var vP = rec.GetType().GetProperties().Where(p => p.GetCustomAttribute<ChoValueAttribute>() != null).FirstOrDefault();

                if (kP != null && vP != null)
                {
                    kP.SetValue(rec, dict.First().Key);
                    vP.SetValue(rec, dict.First().Value);
                    return true;
                }
            }
            if (typeof(IChoKeyValueType).IsAssignableFrom(rec.GetType()))
            {
                IChoKeyValueType kvp = rec as IChoKeyValueType;

                kvp.Key = dict.First().Key;
                kvp.Value = dict.First().Value;
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
                bool lUseJSONSerialization = useJSONSerialization == null ? Configuration.UseJSONSerialization : useJSONSerialization.Value;
                if (lUseJSONSerialization)
                {
                    if (_se == null || _se.Value == null)
                        return jToken.ToObject(type);
                    else
                        return jToken.ToObject(type, _se.Value);
                }
                else
                {
                    if (_se == null || _se.Value == null)
                        return jToken.ToObject(type);
                    else
                        return jToken.ToObject(type, _se.Value);
                }
            }
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
                        return new KeyValuePair<string, object>(kvp.Key, ToDynamic((JToken)kvp.Value));
                    else
                        return kvp;
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
                    if (fieldConfig.ValueConverter != null)
                        list.Add(fieldConfig.ValueConverter(jt));
                    else
                    {
                        list.Add(ToObject(jt, kvp.Value.FieldType));
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
        private string CleanFieldValue(ChoJSONRecordFieldConfiguration config, Type fieldType, string fieldValue)
        {
            if (fieldValue == null) return fieldValue;

            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim;

            if (config.FieldValueTrimOption == null)
            {
                //if (fieldType == typeof(string))
                //    fieldValueTrimOption = ChoFieldValueTrimOption.None;
            }
            else
                fieldValueTrimOption = config.FieldValueTrimOption.Value;

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
                        fieldValue = fieldValue.Substring(0, config.Size.Value);
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
            if (_callbackRecord != null)
            {
                object state = value;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

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
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordFieldLoad(target, index, propName, value), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordFieldLoad(target, index, propName, value), true);
            }
            return true;
        }

        private bool RaiseRecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, value, ex), false);
            }
            return true;
        }

        #endregion Event Raisers
    }
}
