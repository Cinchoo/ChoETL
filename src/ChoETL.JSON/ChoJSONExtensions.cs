using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
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

        public static JToken Flatten(this string json)
        {
            JToken input = JToken.Parse(json);
            return Flatten(input);
        }

        public static JToken Flatten(this JToken input)
        {
            var res = new JArray();
            foreach (var obj in GetFlattenedObjects(input, null))
                res.Add(obj);
            return res;
        }

        private static IEnumerable<JToken> GetFlattenedObjects(JToken token, IEnumerable<JProperty> otherProperties = null)
        {
            if (token is JObject obj)
            {
                var children = obj.Children<JProperty>().GroupBy(prop => prop.Value?.Type == JTokenType.Array).ToDictionary(gr => gr.Key);
                if (children.TryGetValue(false, out var directProps))
                    otherProperties = otherProperties?.Concat(directProps) ?? directProps;

                if (children.TryGetValue(true, out var ChildCollections))
                {
                    foreach (var childObj in ChildCollections.SelectMany(childColl => childColl.Values()).SelectMany(childColl => GetFlattenedObjects(childColl, otherProperties)))
                        yield return childObj;
                }
                else
                {
                    var res = new JObject();
                    if (otherProperties != null)
                        foreach (var prop in otherProperties)
                            res.Add(prop);
                    yield return res;
                }
            }
            else if (token is JArray arr)
            {
                foreach (var co in token.Children().SelectMany(c => GetFlattenedObjects(c, otherProperties)))
                    yield return co;
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

        public static string JTokenToString(this JToken jt)
        {
            if (jt != null && jt.Type == JTokenType.String)
                return $"\"{jt.ToNString()}\"";
            else
                return jt.ToNString();
        }

        public static JToken SerializeToJToken(this JsonSerializer serializer, object value, Formatting? formatting = null, JsonSerializerSettings settings = null,
            bool dontUseConverter = false)
        {
            JsonConverter conv = null;
            if (!dontUseConverter)
            {
                Type vt = value != null ? value.GetType() : typeof(object);
                var convName = GetTypeConverterName(vt);
                conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == vt)).FirstOrDefault();
                if (conv == null && ChoJSONConvertersCache.IsInitialized)
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

            if (conv != null)
                t = JToken.Parse(JsonConvert.SerializeObject(value, formatting.Value, conv));
            else if (settings != null)
                t = JToken.Parse(JsonConvert.SerializeObject(value, formatting.Value, settings));
            else
                t = JToken.FromObject(value, serializer);
            return t;
        }

        public static object DeserializeObject(this JsonSerializer serializer, JsonReader reader, Type objType)
        {
            var convName = GetTypeConverterName(objType);
            var conv = serializer.Converters.Where(c => c.GetType().Name == convName || (c.GetType().IsGenericType && c.GetType().GetGenericArguments()[0] == objType)).FirstOrDefault();
            if (conv == null && ChoJSONConvertersCache.IsInitialized)
            {
                if (ChoJSONConvertersCache.Contains(convName))
                    conv = ChoJSONConvertersCache.Get(convName);
            }

            JToken t = null;
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
    }
    public class ChoDynamicObjectConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is ChoDynamicObject)
            {
                var obj = (value as ChoDynamicObject).AsDictionary();

                if (serializer.NullValueHandling == NullValueHandling.Ignore)
                {
                    foreach (var key in obj.Keys)
                    {
                        if (obj[key] == null)
                            obj.Remove(key);
                    }
                }

                if (obj.Count > 0)
                {
                    var t = serializer.SerializeToJToken(obj, dontUseConverter: true);
                    t.WriteTo(writer);
                }
            }
            else
            {
                var t = serializer.SerializeToJToken(value, dontUseConverter: true);
                t.WriteTo(writer);
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadValue(reader);
        }

        private object ReadValue(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (!reader.Read())
                    throw new Exception("Unexpected end.");
            }

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadObject(reader);
                case JsonToken.StartArray:
                    return ReadList(reader);
                default:
                    if (IsPrimitiveToken(reader.TokenType))
                        return reader.Value;

                    throw new Exception("Unexpected token when converting ExpandoObject: {0}".FormatString(reader.TokenType));
            }
        }
        internal static bool IsPrimitiveToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Undefined:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return true;
                default:
                    return false;
            }
        }
        private object ReadList(JsonReader reader)
        {
            IList<object> list = new List<object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        break;
                    default:
                        object v = ReadValue(reader);

                        list.Add(v);
                        break;
                    case JsonToken.EndArray:
                        return list;
                }
            }

            throw new Exception("Unexpected end.");
        }

        private object ReadObject(JsonReader reader)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        string propertyName = reader.Value.ToString();

                        if (!reader.Read())
                            throw new Exception("Unexpected end.");

                        object v = ReadValue(reader);

                        expandoObject[propertyName] = v;
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return expandoObject;
                }
            }

            throw new Exception("Unexpected end.");
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ChoDynamicObject);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return true; }
        }
    }
}
