using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoJSONNodeConverter<T> : JsonConverter<T>
    {
        private Func<object, object> _converter;
        public ChoJSONNodeConverter(Func<object, object> converter)
        {
            _converter = converter;
        }

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return (T)_converter?.Invoke(new { reader, objectType, existingValue, hasExistingValue, serializer }.ToDynamic());
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            var x = _converter?.Invoke(new { writer, value, serializer }.ToDynamic());
            if (x != null)
                writer.WriteRaw(x.ToString());
        }
    }
}
