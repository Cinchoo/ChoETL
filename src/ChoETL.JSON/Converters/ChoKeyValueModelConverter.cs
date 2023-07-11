using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ChoETL
{
    public class ChoKeyValueModelConverter : JsonConverter
    {
        public static readonly ChoKeyValueModelConverter Instance = new ChoKeyValueModelConverter();

        public override bool CanConvert(Type objectType)
        {
            if (objectType == null)
                return false;

            bool isKVPObject = false;
            if (typeof(IChoKeyValueType).IsAssignableFrom(objectType))
                isKVPObject = true;
            else
            {
                var isKVPAttrDefined = ChoTypeDescriptor.GetTypeAttribute<ChoKeyValueTypeAttribute>(objectType) != null;
                if (isKVPAttrDefined)
                {
                    var kP = ChoTypeDescriptor.GetProperties<ChoKeyAttribute>(objectType).FirstOrDefault();
                    var vP = ChoTypeDescriptor.GetProperties<ChoValueAttribute>(objectType).FirstOrDefault();
                    if (kP != null && vP != null)
                        isKVPObject = true;
                }
            }

            return isKVPObject;
        }

        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, string>>(reader);
            var item = dict.First();

            var rec = ChoActivator.CreateInstance(objectType);
            if (typeof(IChoKeyValueType).IsAssignableFrom(objectType))
            {
                IChoKeyValueType kvp = rec as IChoKeyValueType;
                kvp.Key = item.Key;
                kvp.Value = item.Value;
            }
            else
            {
                var kP = ChoTypeDescriptor.GetProperties<ChoKeyAttribute>(objectType).FirstOrDefault();
                var vP = ChoTypeDescriptor.GetProperties<ChoValueAttribute>(objectType).FirstOrDefault();

                ChoType.SetPropertyValue(rec, kP.Name, item.Key);
                ChoType.SetPropertyValue(rec, vP.Name, item.Value);

            }
            return rec;
        }

        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            if (value == null)
                return;
            Type objectType = value.GetType();

            var dict = new Dictionary<string, object>();
            if (typeof(IChoKeyValueType).IsAssignableFrom(objectType))
            {
                IChoKeyValueType kvp = value as IChoKeyValueType;
                var propName = kvp.Key.ToNString();
                var propValue = kvp.Value == null ? (string)null : kvp.Value;
                if (!propName.IsNullOrWhiteSpace())
                    dict.Add(propName, propValue);
            }
            else
            {
                var kP = ChoTypeDescriptor.GetProperties<ChoKeyAttribute>(objectType).FirstOrDefault();
                var vP = ChoTypeDescriptor.GetProperties<ChoValueAttribute>(objectType).FirstOrDefault();
                var propName = ChoType.GetPropertyValue(value, kP.Name).ToNString();
                var propValue = ChoType.GetPropertyValue(value, vP.Name);

                if (!propName.IsNullOrWhiteSpace())
                    dict.Add(propName, propValue);
            }
               
            serializer.Serialize(writer, dict);
        }
    }
}
