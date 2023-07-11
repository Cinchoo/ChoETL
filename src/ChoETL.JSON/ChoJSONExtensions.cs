using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONExtensions
    {
        static ChoJSONExtensions()
        {
        }
        public static JToken GetProperty(this JToken token, string name, StringComparison comparer = StringComparison.InvariantCultureIgnoreCase)
        {
            if (token == null)
            {
                return null;
            }
            var obj = token as JObject;
            JToken match;
            if (obj.TryGetValue(name, comparer, out match))
            {
                return match;
            }
            return null;
        }

        /*
        public static string ToJson(this List<Item> items)
        {
            var lookup = items.ToLookup(x => x.ParentId);
            JObject ToJson(int? parentId)
            {
                JProperty ToProperty(Item item)
                {
                    switch (item.Type)
                    {
                        case "":
                            return new JProperty(item.Name, ToJson(item.Id));
                        case "String":
                            return new JProperty(item.Name, item.Description);
                        case "Array":
                            return new JProperty(item.Name, lookup[item.Id].Select(x => x.Description).ToArray());
                        case "Int":
                            return new JProperty(item.Name, int.Parse(item.Description));
                        default:
                            return new JProperty(item.Name);
                    }
                }
                return new JObject(lookup[parentId].Select(x => ToProperty(x)));
            }
            var output = ToJson(null);
            var text = Newtonsoft.Json.JsonConvert.SerializeObject(output, Newtonsoft.Json.Formatting.Indented);
            return text;
        }
        public object ToHierarchy<TSource, TKey>(this IEnumerable<TSource> items, Func<TSource, TKey> keySelector,
            Func<ILookup<TKey, TSource>, TSource, JProperty> propertySelector
            )
            where TSource : class
        {
            var lookup = items.ToLookup(keySelector);
            JObject ToJson(TKey parentId)
            {
                return new JObject(lookup[parentId].Select(x => propertySelector(lookup, x)));
            }
            //var output = ToJson(null);
        }
        */
        public static T ToObjectEx<T>(this JObject jo, JsonSerializer serializer)
        {
            return (T)ToObjectEx(jo, typeof(T), serializer);
        }
        public static object ToObjectEx(this JObject jo, Type objType, JsonSerializer serializer)
        {
            var obj = ChoActivator.CreateInstance(objType);
            serializer.Populate(jo.CreateReader(), obj);
            return obj;
        }

        public static JToken Rename(this JToken token, string newName)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot rename a null token");

            JProperty property;

            if (token.Type == JTokenType.Property)
            {
                if (token.Parent == null)
                    throw new InvalidOperationException("Cannot rename a property with no parent");

                property = (JProperty)token;
            }
            else
            {
                if (token.Parent == null || token.Parent.Type != JTokenType.Property)
                    throw new InvalidOperationException("This token's parent is not a JProperty; cannot rename");

                property = (JProperty)token.Parent;
            }

            // Note: to avoid triggering a clone of the existing property's value,
            // we need to save a reference to it and then null out property.Value
            // before adding the value to the new JProperty.  
            // Thanks to @dbc for the suggestion.

            var existingValue = property.Value;
            property.Value = null;
            var newProperty = new JProperty(newName, existingValue);
            return newProperty;
            //property.Replace(newProperty);
        }

        static string[] GetAllNestedKeys(JObject jsonObject)
        {
            ChoGuard.ArgumentNotNull(jsonObject, "JObject");
            var keysToFlattenBy = jsonObject.SelectTokens("$..*")
                .Where(t => t.Type == JTokenType.Array || t.Type == JTokenType.Object)
                .Select(t => t.Path)
                .Where(t => t.Length > 0 && char.IsNumber(t[t.Length - 1]))
                .Select(t => t.Split('.').LastOrDefault() ?? string.Empty)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToArray();

            return keysToFlattenBy;

        }

        static string[] GetAllNestedKeys(string json)
        {
            var jsonObject = JObject.Parse(json);
            return GetAllNestedKeys(jsonObject);
        }

        public static JsonWriter CreateJSONWriter(this StringBuilder sb)
        {
            ChoGuard.ArgumentNotNull(sb, nameof(sb));
            return CreateJSONWriter(new StringWriter(sb));
        }

        public static JsonWriter CreateJSONWriter(this string filePath)
        {
            ChoGuard.ArgumentNotNull(filePath, nameof(filePath));
            return CreateJSONWriter(new StreamWriter(filePath));
        }

        public static JsonWriter CreateJSONWriter(this TextWriter writer)
        {
            ChoGuard.ArgumentNotNull(writer, nameof(writer));

            JsonWriter jwriter = new JsonTextWriter(writer);
            jwriter.Formatting = Newtonsoft.Json.Formatting.None;
            return jwriter;
        }
        public static void WriteFormattedRawValue(this JsonWriter writer, string json, Action<JsonReader> setup = null)
        {
            if (json == null)
                writer.WriteRawValue(json);
            else
            {
                if (setup == null)
                {
                    setup = (rd) =>
                    {
                        rd.DateParseHandling = DateParseHandling.None;
                        rd.FloatParseHandling = default;
                    };
                }
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    setup(reader);
                    writer.WriteToken(reader);
                }
            }
        }

        public static void WriteReader(this JsonWriter writer, JsonReader reader, ChoJObjectLoadOptions? options = null)
        {
            ChoGuard.ArgumentNotNull(reader, nameof(reader));
            ChoGuard.ArgumentNotNull(writer, nameof(writer));

            if (options == null)
                options = ChoJObjectLoadOptions.All;

            writer.WriteStartObject();
            writer.WriteToReader(reader, options);
        }

        private static void WriteToReader(this JsonWriter writer, JsonReader reader, ChoJObjectLoadOptions? options = null)
        {
            var path = reader.Path;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeNestedObjects) == ChoJObjectLoadOptions.ExcludeNestedObjects)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        writer.WriteStartObject();
                        writer.WriteToReader(reader, options);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    try

                    {
                        if ((options & ChoJObjectLoadOptions.ExcludeNestedObjects) == ChoJObjectLoadOptions.ExcludeNestedObjects)
                        {

                        }
                        else
                            writer.WriteEndObject();
                    }
                    catch { }
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeArrays) == ChoJObjectLoadOptions.ExcludeArrays)
                    {
                        reader.Skip();
                    }
                    else
                    {
                        writer.WriteStartArray();
                        //InvokeJArrayLoader(reader);
                        return;
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeArrays) == ChoJObjectLoadOptions.ExcludeArrays)
                    {
                    }
                    else
                    {
                        writer.WriteEndArray();
                    }
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propName = reader.Value.ToNString();
                    writer.WritePropertyName(propName);

                    writer.WriteToReader(reader, options);
                }
                else if (reader.TokenType == JsonToken.Integer
                    || reader.TokenType == JsonToken.Float
                    || reader.TokenType == JsonToken.String
                    || reader.TokenType == JsonToken.Boolean
                    || reader.TokenType == JsonToken.Date
                    || reader.TokenType == JsonToken.Bytes
                    || reader.TokenType == JsonToken.Raw
                    || reader.TokenType == JsonToken.String
                    )
                {
                    writer.WriteValue(reader.Value);
                }
                else
                    writer.WriteValue(JValue.CreateNull());

                if (reader.Path == path)
                    break;
            }
        }

        public static JsonSerializer DeepCopy(this JsonSerializer serializer)
        {
            var copiedSerializer = new JsonSerializer
            {
                Context = serializer.Context,
                Culture = serializer.Culture,
                ContractResolver = serializer.ContractResolver,
                ConstructorHandling = serializer.ConstructorHandling,
                CheckAdditionalContent = serializer.CheckAdditionalContent,
                DateFormatHandling = serializer.DateFormatHandling,
                DateFormatString = serializer.DateFormatString,
                DateParseHandling = serializer.DateParseHandling,
                DateTimeZoneHandling = serializer.DateTimeZoneHandling,
                DefaultValueHandling = serializer.DefaultValueHandling,
                EqualityComparer = serializer.EqualityComparer,
                FloatFormatHandling = serializer.FloatFormatHandling,
                Formatting = serializer.Formatting,
                FloatParseHandling = serializer.FloatParseHandling,
                MaxDepth = serializer.MaxDepth,
                MetadataPropertyHandling = serializer.MetadataPropertyHandling,
                MissingMemberHandling = serializer.MissingMemberHandling,
                NullValueHandling = serializer.NullValueHandling,
                ObjectCreationHandling = serializer.ObjectCreationHandling,
                PreserveReferencesHandling = serializer.PreserveReferencesHandling,
                ReferenceResolver = serializer.ReferenceResolver,
                ReferenceLoopHandling = serializer.ReferenceLoopHandling,
                StringEscapeHandling = serializer.StringEscapeHandling,
                TraceWriter = serializer.TraceWriter,
                TypeNameHandling = serializer.TypeNameHandling,
                SerializationBinder = serializer.SerializationBinder,
                TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling
            };
            foreach (var converter in serializer.Converters)
            {
                copiedSerializer.Converters.Add(converter);
            }
            return copiedSerializer;
        }
        public static JsonReader CopyReaderForObject(this JsonReader reader, JToken jToken)
        {
            // create reader and copy over settings
            JsonReader jTokenReader = jToken.CreateReader();
            jTokenReader.Culture = reader.Culture;
            jTokenReader.DateFormatString = reader.DateFormatString;
            jTokenReader.DateParseHandling = reader.DateParseHandling;
            jTokenReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jTokenReader.FloatParseHandling = reader.FloatParseHandling;
            jTokenReader.MaxDepth = reader.MaxDepth;
            jTokenReader.SupportMultipleContent = reader.SupportMultipleContent;
            return jTokenReader;
        }
        public static IEnumerable<JObject> FlattenHierarchy(this string json, string childrenNodeName, string[] mapNodeNames = null, Func<JObject, object> mapper = null)
        {
            JObject input = JObject.Parse(json);
            return FlattenHierarchy(input, childrenNodeName, mapNodeNames, mapper);
        }

        public static IEnumerable<JObject> FlattenHierarchy(this JObject input, string childrenNodeName, string[] mapNodeNames = null, Func<JObject, object> mapper = null)
        {
            if (input == null) yield break;
            yield return Map(input, childrenNodeName, mapNodeNames, mapper);
            foreach (var item in input[childrenNodeName] as JArray)
            {
                foreach (var node in FlattenHierarchy(item as JObject, childrenNodeName, mapNodeNames, mapper))
                    yield return node;
            }
        }

        private static JObject Map(JObject input, string childrenNodeName, string[] mapNodeNames = null, Func<JObject, object> mapper = null)
        {
            if (childrenNodeName.IsNullOrWhiteSpace() || !input.ContainsKey(childrenNodeName))
                return input;

            if (mapNodeNames != null && mapNodeNames.Length > 0)
            {
                JObject ret = new JObject();
                foreach (var mn in mapNodeNames)
                {
                    if (input.ContainsKey(mn))
                        ret.Add(mn, input[mn]);
                    else
                        ret.Add(mn, null);
                }
                return ret;
            }
            else if (mapper != null)
            {
                return JObject.FromObject(mapper(input));
            }
            else
            {
                var result = (JObject)input.DeepClone();
                if (result.ContainsKey(childrenNodeName))
                    result.Remove(childrenNodeName);
                return result;
            }
        }
        public static Dictionary<string, object> Flatten(this string json)
        {
            JToken token = JToken.Parse(json);
            return Flatten(token);
        }
        public static Dictionary<string, object> Flatten(this JToken token)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            FillDictionaryFromJToken(dict, token, "");
            return dict;
        }
        private static string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }
        public static JToken Flatten(this string json, char? nestedKeySeparator = null, Func<string, string, string> nestedKeyResolver = null,
            char? arrayIndexSeparator = null, bool useNestedKeyFormat = false, bool ignoreArrayIndex = true,
            string flattenByNodeName = null, string flattenByJsonPath = null)
        {
            JToken input = JToken.Parse(json);
            return Flatten(input, nestedKeySeparator, arrayIndexSeparator, nestedKeyResolver, useNestedKeyFormat, ignoreArrayIndex,
                flattenByNodeName, flattenByJsonPath);
        }

        public static JToken Flatten(this JToken input, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, 
            Func<string, string, string> nestedKeyResolver = null,
            bool useNestedKeyFormat = false, bool ignoreArrayIndex = true, string flattenByNodeName = null, string flattenByJsonPath = null
            )
        {
            var res = new JArray();
            foreach (var obj in GetFlattenedObjects(input, null, null, nestedKeySeparator == null ? String.Empty : nestedKeySeparator.ToString(), 
                nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator == null ? String.Empty : arrayIndexSeparator.ToString(), 
                ignoreArrayIndex, null, flattenByNodeName, flattenByJsonPath, true))
                res.Add(obj);
            return res;
        }

        private static void FlattenJObject(JToken token, IDictionary<bool, IList<JProperty>> results = null, bool bucket = true)
        {
            if (results == null)
                results = new Dictionary<bool, IList<JProperty>>();
            if (!results.ContainsKey(false))
                results.Add(false, new List<JProperty>());
            if (!results.ContainsKey(true))
                results.Add(true, new List<JProperty>());


            if (token is JObject obj)
            {
                var children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                if (children.TryGetValue(false, out var directProps))
                {
                    var otherProperties = results[false];
                    otherProperties = otherProperties.Concat(directProps).ToList();
                    foreach (var r in otherProperties)
                        FlattenJObject(r, results, false);
                }
                else if (children.TryGetValue(true, out var ChildCollections))
                {
                    foreach (var r in ChildCollections.SelectMany(childColl => childColl.Values().Select(c => c)))
                        FlattenJObject(r, results, true);
                }
            }
            else if (token is JProperty)
            {
                var properties = results[bucket];
                var value = ((JProperty)token).Value;

                if (value is JValue)
                    properties.Add(token as JProperty);
                else
                {
                    FlattenJObject(value, results, bucket);
                }
            }
            else
            {
            }
        }

        /*
                    foreach (var jo1 in directProps.Where(j => j.Value is JObject))
                    {
                        foreach (var jt in GetFlattenedObjects(jo1.Value, otherProperties, jo1.Name).OfType<JObject>())
                        {
                            foreach (var prop1 in jt.Properties())
                                result.Add(prop1);
                        }
                    }


                    JObject result = new JObject();
                    foreach (var jo1 in directProps)
                    {
                        if (jo1.Value is JObject)
                        {
                            foreach (var jt in GetFlattenedObjects(jo1.Value, otherProperties, jo1.Name).OfType<JObject>())
                            {
                                foreach (var prop1 in jt.Properties())
                                    result.Add(prop1);
                            }
                        }
                        else
                        {
                            result.Add(jo1);
                        }
                    }

                    //yield return result;

         */

        private static IEnumerable<JToken> GetFlattenedObjectsInternal(JToken token, IEnumerable<JProperty> otherProperties = null, string parentNodeName = null,
    string nestedKeySeparator = null, Func<string, string, string> nestedKeyResolver = null, bool useNestedKeyFormat = false,
    string arrayIndexSeparator = null,
    bool ignoreArrayIndex = true, int? arrayIndex = null)
        {
            //var results = new Dictionary<bool, IList<JProperty>>();
            //FlattenJObject(token, results);

            if (token is JObject obj)
            {
                var children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                if (children.TryGetValue(false, out var directProps))
                {
                    if (useNestedKeyFormat && parentNodeName != null)
                    {
                        List<JProperty> np = new List<JProperty>();
                        foreach (var jt in directProps)
                            np.Add(jt.Rename($"{parentNodeName}{nestedKeySeparator}{jt.Name}") as JProperty);

                        otherProperties = otherProperties?.Concat(np) ?? directProps;
                    }
                    else
                        otherProperties = otherProperties?.Concat(directProps) ?? directProps;
                }

                if (children.TryGetValue(true, out var ChildCollections) && ChildCollections.Where(c => c.Values().Count() > 0).Any())
                {
                    foreach (var childObj in ChildCollections.SelectMany(childColl => childColl.Values().Select(c => new { ParentNodeName = childColl.Name, ChildNode = c }))
                        .SelectMany((kvp, index) => GetFlattenedObjects(kvp.ChildNode, otherProperties,
                        useNestedKeyFormat ? (parentNodeName.IsNullOrWhiteSpace() ? $"{kvp.ParentNodeName}" : $"{parentNodeName}{nestedKeySeparator}{kvp.ParentNodeName}") : kvp.ParentNodeName,
                        nestedKeySeparator, nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator, ignoreArrayIndex, index)))
                        yield return childObj;
                }
                else
                {
                    var res = new JObject();
                    if (otherProperties != null)
                    {
                        foreach (var prop in otherProperties)
                        {
                            if (!res.ContainsKey(prop.Name))
                                res.Add(prop);
                            else
                            {
                                string newKey;
                                if (nestedKeyResolver == null)
                                {
                                    if (parentNodeName.IsNullOrWhiteSpace())
                                    {

                                    }
                                    else
                                    {
                                        newKey = $"{parentNodeName}{nestedKeySeparator}{prop.Name}";
                                        res.Add(prop.Rename(newKey));
                                    }
                                }
                                else
                                {
                                    newKey = nestedKeyResolver(parentNodeName, prop.Name);
                                    res.Add(prop.Rename(newKey));
                                }
                            }
                        }
                    }
                    yield return res;
                }
            }
            else if (token is JArray arr)
            {
                foreach (var co in token.Children().SelectMany((c, index) => GetFlattenedObjects(c, otherProperties,
                        useNestedKeyFormat ? (parentNodeName.IsNullOrWhiteSpace() ? $"{index}" : $"{parentNodeName}{nestedKeySeparator}{index}") : $"{index}",
                        nestedKeySeparator, nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator, ignoreArrayIndex, index)))
                    yield return co;
            }
            else if (token is JValue && arrayIndex != null)
            {
                var res = new JObject();
                if (otherProperties != null)
                {
                    foreach (var prop in otherProperties)
                    {
                        if (!res.ContainsKey(prop.Name))
                            res.Add(prop);
                        else
                        {
                            string newKey;
                            if (nestedKeyResolver == null)
                            {
                                if (parentNodeName.IsNullOrWhiteSpace())
                                {

                                }
                                else
                                {
                                    newKey = $"{parentNodeName}{nestedKeySeparator}{prop.Name}";
                                    res.Add(prop.Rename(newKey));
                                }
                            }
                            else
                            {
                                newKey = nestedKeyResolver(parentNodeName, prop.Name);
                                res.Add(prop.Rename(newKey));
                            }
                        }
                    }
                }
                JProperty prop1 = null;
                if (ignoreArrayIndex)
                    prop1 = new JProperty($"{parentNodeName.ToSingular()}", ((JValue)token).Value);
                else
                    prop1 = new JProperty($"{parentNodeName.ToSingular()}{arrayIndexSeparator}{arrayIndex.Value}", ((JValue)token).Value);

                if (!res.ContainsKey(prop1.Name))
                    res.Add(prop1);
                else
                {
                    string newKey;
                    if (nestedKeyResolver == null)
                    {
                        if (parentNodeName.IsNullOrWhiteSpace())
                        {

                        }
                        else
                        {
                            newKey = $"{parentNodeName}{nestedKeySeparator}{prop1.Name}";
                            res.Add(prop1.Rename(newKey));
                        }
                    }
                    else
                    {
                        newKey = nestedKeyResolver(parentNodeName, prop1.Name);
                        res.Add(prop1.Rename(newKey));
                    }
                }

                yield return res;
            }
            else
                throw new NotImplementedException(token.GetType().Name);
        }
        private static IEnumerable<JToken> GetFlattenedChildObjects(Dictionary<bool, IGrouping<bool, JProperty>> children, IEnumerable<JProperty> otherProperties = null, string parentNodeName = null,
            string nestedKeySeparator = null, Func<string, string, string> nestedKeyResolver = null, bool useNestedKeyFormat = false,
            string arrayIndexSeparator = null, bool ignoreArrayIndex = true, int? arrayIndex = null,
            string flattenByNodeName = null, string flattenByJsonPath = null)
        {
            if (children != null && children.TryGetValue(false, out var directProps))
            {
                if (useNestedKeyFormat && parentNodeName != null)
                {
                    List<JProperty> np = new List<JProperty>();
                    foreach (var jt in directProps)
                        np.Add(jt.Rename($"{parentNodeName}{nestedKeySeparator}{jt.Name}") as JProperty);

                    otherProperties = otherProperties?.Concat(np) ?? directProps;
                }
                else
                    otherProperties = otherProperties?.Concat(directProps) ?? directProps;
            }

            if (children != null && children.TryGetValue(true, out var ChildCollections) && ChildCollections.Where(c => c.Values().Count() > 0).Any())
            {
                foreach (var childObj in ChildCollections.SelectMany(childColl => childColl.Values().Select(c => new { ParentNodeName = childColl.Name, ChildNode = c }))
                    .SelectMany((kvp, index) => GetFlattenedObjectsInternal(kvp.ChildNode, otherProperties,
                    useNestedKeyFormat ? (parentNodeName.IsNullOrWhiteSpace() ? $"{kvp.ParentNodeName}" : $"{parentNodeName}{nestedKeySeparator}{kvp.ParentNodeName}") : kvp.ParentNodeName,
                    nestedKeySeparator, nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator, ignoreArrayIndex, index)))
                    yield return childObj;
            }
            else
            {
                var res = new JObject();
                if (otherProperties != null)
                {
                    foreach (var prop in otherProperties)
                    {
                        if (!res.ContainsKey(prop.Name))
                            res.Add(prop);
                        else
                        {
                            string newKey;
                            if (nestedKeyResolver == null)
                            {
                                if (parentNodeName.IsNullOrWhiteSpace())
                                {

                                }
                                else
                                {
                                    newKey = $"{parentNodeName}{nestedKeySeparator}{prop.Name}";
                                    res.Add(prop.Rename(newKey));
                                }
                            }
                            else
                            {
                                newKey = nestedKeyResolver(parentNodeName, prop.Name);
                                res.Add(prop.Rename(newKey));
                            }
                        }
                    }
                }
                yield return res;
            }
        }
        private static IEnumerable<JToken> GetFlattenedObjects(JToken token, IEnumerable<JProperty> otherProperties = null, string parentNodeName = null,
            string nestedKeySeparator = null, Func<string, string, string> nestedKeyResolver = null, bool useNestedKeyFormat = false,
            string arrayIndexSeparator = null, bool ignoreArrayIndex = true, int? arrayIndex = null,
            string flattenByNodeName = null, string flattenByJsonPath = null,
            bool rootCall = false)
        {
            //var results = new Dictionary<bool, IList<JProperty>>();
            //FlattenJObject(token, results);

            if (token is JObject obj)
            {
                Dictionary<bool, IGrouping<bool, JProperty>> children = null;

                if (rootCall)
                {
                    if (!flattenByJsonPath.IsNullOrWhiteSpace())
                    {
                        if (!flattenByNodeName.IsNullOrWhiteSpace())
                        {
                            foreach (var co in obj.SelectTokens(flattenByJsonPath))
                            {
                                children = co.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array
                                    && prop.Name == flattenByNodeName).ToDictionary(gr => gr.Key);

                                foreach (var c in GetFlattenedChildObjects(children, null, null, nestedKeySeparator == null ? String.Empty : nestedKeySeparator.ToString(),
                nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator == null ? String.Empty : arrayIndexSeparator.ToString(),
                ignoreArrayIndex, null, flattenByNodeName, flattenByJsonPath))
                                    yield return c;

                            }
                                
                            yield break;
                        }
                        else
                        {
                            foreach (var co in obj.SelectTokens(flattenByJsonPath))
                            {
                                children = co.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);

                                foreach (var c in GetFlattenedChildObjects(children, null, null, nestedKeySeparator == null ? String.Empty : nestedKeySeparator.ToString(),
                nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator == null ? String.Empty : arrayIndexSeparator.ToString(),
                ignoreArrayIndex, null, flattenByNodeName, flattenByJsonPath))
                                    yield return c;

                            }

                            yield break;
                        }
                    }
                    else if (!flattenByNodeName.IsNullOrWhiteSpace())
                    {
                        if (obj.Children<JProperty>().Any(p => p.Name == flattenByNodeName))
                        {
                            children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array
                                && prop.Name == flattenByNodeName).ToDictionary(gr => gr.Key);
                        }
                        else if (flattenByNodeName.Contains("."))
                        {
                            var tokens = flattenByNodeName.SplitNTrim(".");
                            var fbn = tokens.Skip(tokens.Length - 1).FirstOrDefault();
                            tokens = tokens.Take(tokens.Length - 1).ToArray();

                            JToken node = obj;
                            foreach (var cn in tokens)
                            {
                                if (node.Children<JProperty>().Any(p => p.Name == cn))
                                {
                                    JObject jo = node.Children<JProperty>().First(p => p.Name == cn).Value as JObject;
                                    if (jo != null)
                                    {
                                        foreach (var prop in jo.Children<JProperty>())
                                        {
                                            if (!useNestedKeyFormat)
                                                obj.Add(prop.Name, prop.Value);
                                            else
                                                obj.Add($"{cn}{nestedKeySeparator}{prop.Name}", prop.Value);
                                        }
                                    }
                                    obj.Remove(cn);
                                }
                                else
                                    yield break;
                            }
                            if (obj.Children<JProperty>().Any(p => p.Name == fbn || p.Name.EndsWith($"{nestedKeySeparator}{fbn}")))
                            {
                                children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array
                                    && (prop.Name == fbn || prop.Name.EndsWith($"{nestedKeySeparator}{fbn}"))).ToDictionary(gr => gr.Key);
                            }
                        }
                    }
                    else
                        children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                }
                else
                {
                    children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                }

                foreach (var co in GetFlattenedChildObjects(children, otherProperties, parentNodeName, nestedKeySeparator == null ? String.Empty : nestedKeySeparator.ToString(),
                nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator == null ? String.Empty : arrayIndexSeparator.ToString(),
                ignoreArrayIndex, null, flattenByNodeName, flattenByJsonPath))
                    yield return co;
            }
            else if (token is JArray arr)
            {
                foreach (var co in token.Children().SelectMany((c, index) => GetFlattenedObjects(c, otherProperties,
                        useNestedKeyFormat ? (parentNodeName.IsNullOrWhiteSpace() ? $"{index}" : $"{parentNodeName}{nestedKeySeparator}{index}") : $"{index}",
                        nestedKeySeparator, nestedKeyResolver, useNestedKeyFormat, arrayIndexSeparator, ignoreArrayIndex, index,
                        flattenByNodeName, flattenByJsonPath, true)))
                    yield return co;
            }
            else if (token is JValue && arrayIndex != null)
            {
                var res = new JObject();
                if (otherProperties != null)
                {
                    foreach (var prop in otherProperties)
                    {
                        if (!res.ContainsKey(prop.Name))
                            res.Add(prop);
                        else
                        {
                            string newKey;
                            if (nestedKeyResolver == null)
                            {
                                if (parentNodeName.IsNullOrWhiteSpace())
                                {

                                }
                                else
                                {
                                    newKey = $"{parentNodeName}{nestedKeySeparator}{prop.Name}";
                                    res.Add(prop.Rename(newKey));
                                }
                            }
                            else
                            {
                                newKey = nestedKeyResolver(parentNodeName, prop.Name);
                                res.Add(prop.Rename(newKey));
                            }
                        }
                    }
                }
                JProperty prop1 = null;
                if (ignoreArrayIndex)
                    prop1 = new JProperty($"{parentNodeName.ToSingular()}", ((JValue)token).Value);
                else
                    prop1 = new JProperty($"{parentNodeName.ToSingular()}{arrayIndexSeparator}{arrayIndex.Value}", ((JValue)token).Value);

                if (!res.ContainsKey(prop1.Name))
                    res.Add(prop1);
                else
                {
                    string newKey;
                    if (nestedKeyResolver == null)
                    {
                        if (parentNodeName.IsNullOrWhiteSpace())
                        {

                        }
                        else
                        {
                            newKey = $"{parentNodeName}{nestedKeySeparator}{prop1.Name}";
                            res.Add(prop1.Rename(newKey));
                        }
                    }
                    else
                    {
                        newKey = nestedKeyResolver(parentNodeName, prop1.Name);
                        res.Add(prop1.Rename(newKey));
                    }
                }

                yield return res;
            }
            else
                throw new NotImplementedException(token.GetType().Name);
        }

        private static string GetTypeConverterName(Type type)
        {
            if (type == null) return String.Empty;

            type = type.GetUnderlyingType();
            if (typeof(Array).IsAssignableFrom(type))
                return $"{type.GetItemType().GetUnderlyingType().Name}ArrayConverter";
            else if (typeof(IList).IsAssignableFrom(type))
                return $"{type.GetItemType().GetUnderlyingType().Name}ListConverter";
            else
                return $"{type.Name}Converter";
        }

        public static string JTokenToString(this JToken jt, JsonSerializer serializer, JsonSerializerSettings serializerSettings, Formatting formatting)
        {
            return JsonConvert.SerializeObject(jt, formatting, serializerSettings);
            if (jt != null && jt.Type == JTokenType.String)
                return $"\"{jt.ToNString()}\"";
            else
                return jt == null ? jt.ToNString() : jt.ToString(Formatting.Indented, serializer.Converters.ToArray());
        }

        public static JToken SerializeToJToken(this JsonSerializer serializer, object value, Formatting? formatting = null, JsonSerializerSettings settings = null,
            bool dontUseConverter = false, bool enableXmlAttributePrefix = false, bool keepNSPrefix = false)
        {
            JsonConverter conv = null;
            if (!dontUseConverter)
            {
                Type vt = value != null ? value.GetType() : typeof(object);
                var convName = GetTypeConverterName(vt);
                conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == vt)).FirstOrDefault();
                if (conv == null)
                {
                    if (ChoJSONConvertersCache.Contains(convName))
                        conv = ChoJSONConvertersCache.Get(convName);
                    else if (ChoJSONConvertersCache.Contains(vt))
                        conv = ChoJSONConvertersCache.Get(vt);
                }
            }

            if (value != null)
            {
                if (!value.GetType().IsSimple())
                {
                    bool disableImplcityOp = false;
                    if (ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(value.GetType()) != null)
                        disableImplcityOp = ChoTypeDescriptor.GetTypeAttribute<ChoTurnOffImplicitOpsAttribute>(value.GetType()).Flag;

                    if (!disableImplcityOp)
                    {
                        Type to = null;
                        if (value.GetType().CanCastToPrimitiveType(out to))
                            value = ChoConvert.ConvertTo(value, to);
                        else if (value.GetType().GetImplicitTypeCastBackOps().Any())
                        {
                            var castTypes = value.GetType().GetImplicitTypeCastBackOps();

                            foreach (var ct in castTypes)
                            {
                                try
                                {
                                    value = ChoConvert.ConvertTo(value, ct);
                                    break;
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            JToken t = null;
            if (settings != null)
            {
                if (conv != null)
                    settings.Converters.Add(conv);
            }
            if (formatting == null)
                formatting = serializer.Formatting;

            if (settings != null /*&& enableXmlAttributePrefix*/)
            {
                if (settings.Context.Context == null)
                    settings.Context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All, new ChoDynamicObject());

                dynamic ctx = settings.Context.Context;
                ctx.EnableXmlAttributePrefix = enableXmlAttributePrefix;
                ctx.KeepNSPrefix = keepNSPrefix;
                ctx.JsonSerializerSettings = settings;
            }

            if (conv != null)
            {
                serializer.Converters.Add(conv);
                t = value != null ? JToken.FromObject(value, serializer) : null;
            }
            //else if (settings != null)
            //    t = JToken.Parse(JsonConvert.SerializeObject(value, formatting.Value, settings));
            else
            {
                t = value != null ? JToken.FromObject(value, serializer) : null;
            }
            return t;
        }

        public static object DeserializeObject(this JsonSerializer serializer, JsonReader reader, Type objType)
        {
            var convName = GetTypeConverterName(objType);
            var conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == objType)).FirstOrDefault();
            if (conv == null)
            {
                if (ChoJSONConvertersCache.Contains(convName))
                    conv = ChoJSONConvertersCache.Get(convName);
            }

            if (conv == null)
            {
                return serializer.Deserialize(reader, objType);
            }
            else
            {
                return JsonConvert.DeserializeObject(JObject.ReadFrom(reader).ToString(), objType, conv);
            }
        }

        public static string DumpAsJson(this DataTable table, Formatting formatting = Formatting.Indented)
        {
            if (table == null)
                return String.Empty;

            return JsonConvert.SerializeObject(table, formatting);
        }

        public static object GetNameAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Name).FirstOrDefault();
        }

        public static object GetValueAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Value).FirstOrDefault();
        }

        public static object ToJSONObject(this IDictionary<string, object> dict, Type type)
        {
            object target = ChoActivator.CreateInstance(type);
            string key = null;
            foreach (var p in ChoType.GetProperties(type))
            {
                if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                key = p.Name;
                var attr = p.GetCustomAttribute<JsonPropertyAttribute>();
                if (attr != null && !attr.PropertyName.IsNullOrWhiteSpace())
                    key = attr.PropertyName.NTrim();

                if (!dict.ContainsKey(key))
                    continue;

                p.SetValue(target, dict[key].CastObjectTo(p.PropertyType));
            }

            return target;
        }

        public static T ToJSONObject<T>(this IDictionary<string, object> dict)
            where T : class, new()
        {
            return (T)ToJSONObject(dict, typeof(T));
        }

        public static IEnumerable<JValue> GetLeafValues(this JToken jToken)
        {
            if (jToken is JValue jValue)
            {
                yield return jValue;
            }
            else if (jToken is JArray jArray)
            {
                foreach (var result in GetLeafValuesFromJArray(jArray))
                {
                    yield return result;
                }
            }
            else if (jToken is JProperty jProperty)
            {
                foreach (var result in GetLeafValuesFromJProperty(jProperty))
                {
                    yield return result;
                }
            }
            else if (jToken is JObject jObject)
            {
                foreach (var result in GetLeafValuesFromJObject(jObject))
                {
                    yield return result;
                }
            }
        }

        #region Private helpers

        static IEnumerable<JValue> GetLeafValuesFromJArray(JArray jArray)
        {
            for (var i = 0; i < jArray.Count; i++)
            {
                foreach (var result in GetLeafValues(jArray[i]))
                {
                    yield return result;
                }
            }
        }

        static IEnumerable<JValue> GetLeafValuesFromJProperty(JProperty jProperty)
        {
            foreach (var result in GetLeafValues(jProperty.Value))
            {
                yield return result;
            }
        }

        static IEnumerable<JValue> GetLeafValuesFromJObject(JObject jObject)
        {
            foreach (var jToken in jObject.Children())
            {
                foreach (var result in GetLeafValues(jToken))
                {
                    yield return result;
                }
            }
        }

        #endregion
    }
}
