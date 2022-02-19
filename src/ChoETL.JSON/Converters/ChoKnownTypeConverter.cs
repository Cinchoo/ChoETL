using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoKnownTypeConverter : JsonConverter, IChoJSONConverter
    {
        private Type _baseType;
        private string _knownTypeDiscriminator;
        private Dictionary<string, Type> _knownTypes = null;
        public Func<object, Type> _recordTypeSelector = null;

        protected ChoKnownTypeConverter()
        {

        }

        public ChoKnownTypeConverter(Type baseType)
        {
            Init(baseType);
        }

        public ChoKnownTypeConverter(Type baseType, string knownTypeDiscriminator, Dictionary<string, Type> knownTypes, Func<object, Type> recordTypeSelector = null)
        {
            Init(baseType, knownTypeDiscriminator, knownTypes, recordTypeSelector);
        }

        protected void Init(Type baseType)
        {
            string knownTypeDiscriminator = null;
            Dictionary<string, Type> knownTypes;

            knownTypes = ChoTypeDescriptor.GetTypeAttributes<ChoKnownTypeAttribute>(baseType).Where(a => a.Type != null && !a.Value.IsNullOrWhiteSpace())
                .GroupBy(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.First().Type, StringComparer.InvariantCultureIgnoreCase);

            var kta = ChoTypeDescriptor.GetTypeAttribute<ChoKnownTypeDiscriminatorAttribute>(baseType);
            if (kta != null && !kta.Discriminator.IsNullOrWhiteSpace())
                knownTypeDiscriminator = kta.Discriminator.Trim();

            Init(baseType, knownTypeDiscriminator, knownTypes, null);
        }

        protected void Init(Type baseType, string knownTypeDiscriminator, Dictionary<string, Type> knownTypes, Func<object, Type> recordTypeSelector)
        {
            _knownTypeDiscriminator = knownTypeDiscriminator;
            _knownTypes = knownTypes;
            _baseType = baseType;
            _recordTypeSelector = recordTypeSelector;
        }

        public JsonSerializer Serializer { get; set; }
        public dynamic Context { get; set; }

        public override bool CanConvert(Type objectType)
        {
            return _baseType == objectType;
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (typeof(IList).IsAssignableFrom(objectType))
            {
                Type itemType = objectType.GetItemType();
                var arr = JArray.Load(reader);


                IList result = ChoActivator.CreateInstance(typeof(IList<>).MakeGenericType(itemType)) as IList;
                foreach (var jo in arr.OfType<JObject>())
                    result.Add(jo.ToObjectEx(ResolveType(jo, itemType), serializer));

                return typeof(Array).IsAssignableFrom(objectType) ? result.ConvertToArray() : result;
            }
            else
            {
                var jo = JObject.Load(reader);
                var newType = ResolveType(jo, objectType);
                return jo.ToObjectEx(newType, serializer);
            }
        }

        private Type ResolveType(JObject jo, Type objectType)
        {
            if (!_knownTypeDiscriminator.IsNullOrWhiteSpace())
            {
                if (jo.ContainsKey(_knownTypeDiscriminator))
                {
                    JValue value = jo[_knownTypeDiscriminator] as JValue;
                    if (value != null && _knownTypes != null && _knownTypes.ContainsKey(value.ToString()))
                        objectType = _knownTypes[value.ToString()];
                    else if (_recordTypeSelector != null)
                        objectType = _recordTypeSelector(new Tuple<long, JToken>(-1, jo[_knownTypeDiscriminator]));
                }
            }
            return objectType;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

    }
    public class ChoKnownTypeConverter<T> : ChoKnownTypeConverter
    {
        public ChoKnownTypeConverter()
        {
            Type baseType = typeof(T);
            Init(baseType);
        }
    }
}
