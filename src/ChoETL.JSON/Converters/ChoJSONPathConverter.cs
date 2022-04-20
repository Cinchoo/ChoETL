using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoJSONPathConverterAttribute : Attribute
    {

    }
    public class ChoJSONPathConverter : JsonConverter
    {
        public static readonly ChoJSONPathConverter Instance = new ChoJSONPathConverter();

        /// <inheritdoc />
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            object targetObj = Activator.CreateInstance(objectType);
            dynamic ctx = serializer.Context.Context;
            StringComparison comparision = ctx != null ? 
                ChoUtility.CastTo<StringComparison>(ctx.StringComparision, StringComparison.InvariantCultureIgnoreCase) : 
                StringComparison.InvariantCultureIgnoreCase;

            foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                string jsonPath = null;

                ChoJSONPathAttribute att1 = prop.GetCustomAttributes(true)
                    .OfType<ChoJSONPathAttribute>()
                    .FirstOrDefault();

                if (att1 != null && !att1.JSONPath.IsNullOrWhiteSpace())
                {
                    jsonPath = att1.JSONPath;
                }
                else if (att1 == null)
                {
                    JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                        .OfType<JsonPropertyAttribute>()
                        .FirstOrDefault();
                    jsonPath = att != null ? att.PropertyName : null; // prop.Name;
                }

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath);
                }

                JToken token = jsonPath.IsNullOrWhiteSpace() ? jo.GetProperty(prop.Name, comparision) : jo.SelectToken(jsonPath);
                if (token != null && token.Type != JTokenType.Null)
                {
                    object value = token.ToObject(prop.PropertyType, serializer);
                    prop.SetValue(targetObj, value, null);
                }
            }

            return targetObj;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called when [JsonConverter] attribute is used
            return objectType.GetCustomAttributes(true).OfType<ChoJSONPathConverterAttribute>().Any();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var properties = value.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.CanWrite);
            JObject main = new JObject();
            foreach (PropertyInfo prop in properties)
            {
                string jsonPath = null;

                ChoJSONPathAttribute att1 = prop.GetCustomAttributes(true)
                    .OfType<ChoJSONPathAttribute>()
                    .FirstOrDefault();

                if (att1 != null && !att1.JSONPath.IsNullOrWhiteSpace())
                {
                    jsonPath = att1.JSONPath;
                }
                else if (att1 == null)
                {
                    JsonPropertyAttribute att = prop.GetCustomAttributes(true)
                        .OfType<JsonPropertyAttribute>()
                        .FirstOrDefault();
                    jsonPath = att != null ? att.PropertyName : prop.Name;
                }
                if (!Regex.IsMatch(jsonPath, @"^[a-zA-Z0-9_.-]+$"))
                {
                    throw new InvalidOperationException("JProperties of JsonPathConverter can have only letters, numbers, underscores, hyphens and dots but name was '" + jsonPath + "'."); // Array operations not permitted
                }

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath);
                }

                var nesting = jsonPath.Split('.');
                JObject lastLevel = main;

                for (int i = 0; i < nesting.Length; i++)
                {
                    if (i == nesting.Length - 1)
                    {
                        lastLevel[nesting[i]] = new JValue(prop.GetValue(value));
                    }
                    else
                    {
                        if (lastLevel[nesting[i]] == null)
                        {
                            lastLevel[nesting[i]] = new JObject();
                        }

                        lastLevel = (JObject)lastLevel[nesting[i]];
                    }
                }
            }

            serializer.Serialize(writer, main);
        }
    }

}
