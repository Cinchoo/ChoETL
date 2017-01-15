using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [ChoTypeConverter(typeof(short))]
    public class ChoShortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    NumberStyles format = parameter.GetValueAt<NumberStyles>(0, ChoTypeConverterFormatSpec.Instance.Value.ShortNumberStyle);
                    return short.Parse(text, format, culture);
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is short)
            {
                string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.Value.ShortFormat);
                return !format.IsNullOrWhiteSpace() ? ((short)value).ToString(format, culture) : value;
            }
            else
                return value;
        }
    }
}
