using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoJSONTypeConverter<T> : IChoValueConverter, IChoJSONConverter, IChoCollectionConverter
    {
        public JsonSerializer Serializer { get; set; }
        public dynamic Context { get; set; } = new ChoDynamicObject();

        private Func<object, object> _converter;

        public ChoJSONTypeConverter(Func<object, object> converter)
        {
            _converter = converter;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (T)_converter?.Invoke(new { value, targetType, parameter, culture, serializer = Serializer, context = Context }.ToDynamic());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ret = _converter?.Invoke(new { value, targetType, parameter, culture, serializer = Serializer, context = Context }.ToDynamic());
            return ret;
        }
    }
}
