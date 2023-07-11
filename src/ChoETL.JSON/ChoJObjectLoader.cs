using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJObjectLoader
    {
        public static JToken InvokeJObjectLoader(JsonReader reader, JsonLoadSettings JsonLoadSettings = null, ChoJObjectLoadOptions? JObjectLoadOptions = null,
            Func<JsonReader, JsonLoadSettings, JObject> CustomJObjectLoader = null)
        {
            try
            {
                if (JObjectLoadOptions != null)
                {
                    switch (JObjectLoadOptions)
                    {
                        case ChoJObjectLoadOptions.All:
                            return JObject.Load(reader, JsonLoadSettings);
                        case ChoJObjectLoadOptions.None:
                            reader.Skip();
                            return ChoJSONObjects.EmptyJObject;
                        default:
                            return LoadJObject(reader, JObjectLoadOptions.Value) as JObject;
                    }
                }
                else
                {
                    if (CustomJObjectLoader != null)
                    {
                        var retValue = CustomJObjectLoader(reader, JsonLoadSettings);
                        reader.Skip();
                        return retValue;
                    }
                    else
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            return JObject.Load(reader, JsonLoadSettings);
                        }
                        else if (reader.TokenType == JsonToken.StartArray)
                        {
                            return JArray.Load(reader, JsonLoadSettings);
                        }
                        else if (reader.TokenType != JsonToken.EndArray)
                        {
                            if (reader.TokenType == JsonToken.EndObject
                                || reader.TokenType == JsonToken.EndConstructor)
                                return null;
                            else if (reader.TokenType == JsonToken.EndArray)
                                return null;

                            return JToken.Load(reader);
                        }
                    }
                    return null;
                }
            }
            finally
            {
            }
        }

        public static JObject ToJObject(JToken value)
        {
            if (value == null)
                return null;

            if (value is JObject)
                return value as JObject;
            else if (value is JArray)
            {
                JObject x = new JObject(new JProperty("Value", value));
                return x;
            }
            else if (value is JProperty)
            {
                return new JObject(value as JProperty);
            }
            else if (value is JValue)
            {
                JObject x = new JObject(new JProperty("Value", value));
                return x;
            }
            return value as JObject;
        }

        public static JArray InvokeJArrayLoader(JsonReader reader, JsonLoadSettings JsonLoadSettings = null, Func<JsonReader, JsonLoadSettings, JArray> CustomJArrayLoader = null,
            bool UseImplicitJArrayLoader = true, long MaxJArrayItemsLoad = 0)
        {
            try
            {
                if (false) //CountOnly)
                {
                    reader.Skip();
                    return ChoJSONObjects.EmptyJArray;
                }
                else
                {
                    if (CustomJArrayLoader != null)
                        return CustomJArrayLoader(reader, JsonLoadSettings);
                    else if (UseImplicitJArrayLoader)
                    {
                        JArray ja = new JArray();
                        long count = 0;
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.StartObject)
                            {
                                count++;
                                if (MaxJArrayItemsLoad > 0 && count > MaxJArrayItemsLoad)
                                    reader.Skip();
                                else
                                {
                                    var jo = InvokeJObjectLoader(reader);
                                    ja.Add(jo);
                                }
                            }
                            else if (reader.TokenType == JsonToken.EndObject)
                            {
                                break;
                            }
                            //else if (reader.TokenType == JsonToken.StartArray)
                            //{
                            //    int count = 0;
                            //    while (reader.Read())
                            //    {
                            //        if (reader.TokenType == JsonToken.StartObject)
                            //        {
                            //            var jo = InvokeJObjectLoader(reader);
                            //            ja.Add(jo);

                            //            count++;
                            //            if (count % 10 == 0)
                            //                break;
                            //        }
                            //    }
                            //}
                        }
                        return ja;
                    }
                    else
                    {
                        var retValue = JArray.Load(reader, JsonLoadSettings);
                        return retValue;
                    }
                }
            }
            finally
            {
            }
        }
        private static JToken LoadJObject(JsonReader reader, ChoJObjectLoadOptions options)
        {
            var path = reader.Path;
            var jo = new JObject();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeNestedObjects) == ChoJObjectLoadOptions.ExcludeNestedObjects)
                    {
                        reader.Skip();
                        return ChoJSONObjects.UndefinedValue;
                    }
                    else
                    {
                        return LoadJObject(reader, options);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    if ((options & ChoJObjectLoadOptions.ExcludeArrays) == ChoJObjectLoadOptions.ExcludeArrays)
                    {
                        reader.Skip();
                        return ChoJSONObjects.UndefinedValue;
                    }
                    else
                    {
                        return InvokeJArrayLoader(reader);
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    var propName = reader.Value.ToNString();
                    //reader.Read();
                    var value = LoadJObject(reader, options);
                    if (ChoJSONObjects.UndefinedValue == value)
                    {
                    }
                    else
                    {
                        if (!jo.ContainsKey(propName))
                            jo.Add(propName, value);
                    }
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
                    var token = JToken.FromObject(reader.Value);
                    return token;
                }
                else
                    return JValue.CreateNull();

                if (reader.TokenType == JsonToken.EndObject && reader.Path == path)
                    break;
            }

            return jo;
        }

        private static void Skip(JsonReader reader)
        {
            var path = reader.Path;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Path == path)
                    break;
            }
        }
    }
}
