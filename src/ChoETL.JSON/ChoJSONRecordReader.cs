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
            bool abortRequested = false;
            _se = new Lazy<JsonSerializer>(() => Configuration.JsonSerializerSettings != null ? JsonSerializer.Create(Configuration.JsonSerializerSettings) : null);

            foreach (var obj in jObjects)
            {
                pair = new Tuple<long, JObject>(++counter, obj);

                if (!_configCheckDone)
                {
                    Configuration.Validate(pair);
                    RaiseMembersDiscovered(Configuration.JSONRecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType == null ? typeof(string) : i.FieldType)).ToArray());
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
            }
            if (!abortRequested && pair != null)
                RaisedRowsLoaded(pair.Item1);
        }

        private ExpandoObjectConverter _expandoObjectConverter = new ExpandoObjectConverter();
        private bool LoadNode(Tuple<long, JObject> pair, ref object rec)
        {
            if (!Configuration.UseJSONSerialization)
                rec = Configuration.IsDynamicObject ? new ExpandoObject() : Activator.CreateInstance(RecordType);

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
                        rec = JsonConvert.DeserializeObject(pair.Item2.ToString(), RecordType);
                        if ((Configuration.ObjectValidationMode & ChoObjectValidationMode.Off) != ChoObjectValidationMode.Off)
                            rec.DoObjectLevelValidation(Configuration, Configuration.JSONRecordFieldConfigurations);

                    }
                }


                if (!RaiseAfterRecordLoad(rec, pair))
                    return false;
            }
            catch (ChoParserException)
            {
                throw;
            }
            catch (ChoMissingRecordFieldException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ChoETLFramework.HandleException(ex);
                if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                {
                    rec = null;
                }
                else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                {
                    if (!RaiseRecordLoadError(rec, pair, ex))
                        throw;
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

            foreach (KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp in Configuration.RecordFieldConfigurationsDict)
            {
                fieldValue = null;
                fieldConfig = kvp.Value;
                if (Configuration.PIDict != null)
                    Configuration.PIDict.TryGetValue(kvp.Key, out pi);

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

                fieldValue = jTokens != null ? (object)jTokens : jToken;

                if (!RaiseBeforeRecordFieldLoad(rec, pair.Item1, kvp.Key, ref fieldValue))
                    continue;

                if (Configuration.IsDynamicObject) //rec is ExpandoObject)
                {
                    if (kvp.Value.FieldType != null)
                    {
                        if (fieldValue is JToken)
                        {
                            fieldValue = ((JToken)fieldValue).ToObject(kvp.Value.FieldType);
                        }
                        else if (fieldValue is JToken[])
                        {
                            jTokens = (JToken[])fieldValue;
                            fieldValue = null;
                            HandleCollection(jTokens, kvp);
                        }
                    }
                }
                else
                {
                    if (pi != null)
                    {
                        kvp.Value.FieldType = pi.PropertyType;
                    }
                    else
                        kvp.Value.FieldType = typeof(string);

                    if (fieldValue is JToken)
                    {
                        fieldValue = ((JToken)fieldValue).ToObject(kvp.Value.FieldType);
                    }
                    else if (fieldValue is JToken[])
                    {
                        jTokens = (JToken[])fieldValue;
                        fieldValue = null;
                        HandleCollection(jTokens, kvp);
                    }
                }

                if (!(fieldValue is ICollection))
                {
                    if (fieldValue is string)
                        fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue as string);
                    else if (fieldValue is JValue)
                        fieldValue = CleanFieldValue(fieldConfig, kvp.Value.FieldType, fieldValue.ToString());
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
                    throw;
                }
                catch (ChoMissingRecordFieldException)
                {
                    if (Configuration.ThrowAndStopOnMissingField)
                        throw;
                }
                catch (Exception ex)
                {
                    ChoETLFramework.HandleException(ex);

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
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else if (pi != null)
                        {
                            if (rec.SetFallbackValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else if (rec.SetDefaultValue(kvp.Key, kvp.Value, Configuration.Culture))
                                rec.DoMemberLevelValidation(kvp.Key, kvp.Value, Configuration.ObjectValidationMode);
                            else
                                throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
                        }
                        else
                            throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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
                                if (!RaiseRecordFieldLoadError(rec, pair.Item1, kvp.Key, fieldValue, ex))
                                    throw new ChoReaderException($"Failed to parse '{fieldValue}' value for '{fieldConfig.FieldName}' field.", ex);
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

        private void HandleCollection(JToken[] jTokens, KeyValuePair<string, ChoJSONRecordFieldConfiguration> kvp)
        {
            if (typeof(ICollection).IsAssignableFrom(kvp.Value.FieldType) && !kvp.Value.FieldType.IsArray)
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
                foreach (var jt in jTokens)
                {
                    fieldValue = jt.ToObject(kvp.Value.FieldType);
                    break;
                }
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

        private bool RaiseAfterRecordLoad(object target, Tuple<long, JObject> pair)
        {
            if (_callbackRecord != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseAfterRecordLoad(target, pair.Item1, pair.Item2), true);
            }
            return true;
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
                return ChoFuncEx.RunWithIgnoreError(() => _callbackRecord.RecordFieldLoadError(target, index, propName, value, ex), true);
            }
            else if (Reader != null)
            {
                return ChoFuncEx.RunWithIgnoreError(() => Reader.RaiseRecordFieldLoadError(target, index, propName, value, ex), true);
            }
            return true;
        }

        #endregion Event Raisers
    }
}
