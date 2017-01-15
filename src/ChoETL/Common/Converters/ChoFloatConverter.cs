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
    [ChoTypeConverter(typeof(float))]
    public class ChoFloatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    NumberStyles format = parameter.GetValueAt<NumberStyles>(0, ChoTypeConverterFormatSpec.Instance.Value.FloatNumberStyle);
                    return float.Parse(text, format, culture);
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is float)
            {
                string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.Value.FloatFormat);
                return !format.IsNullOrWhiteSpace() ? ((float)value).ToString(format, culture) : value;
            }
            else
                return value;
        }
    }
}
