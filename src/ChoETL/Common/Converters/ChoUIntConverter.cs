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
    [ChoTypeConverter(typeof(uint))]
    public class ChoUIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    NumberStyles format = parameter.GetValueAt<NumberStyles>(0, ChoTypeConverterFormatSpec.Instance.Value.UIntNumberStyle);
                    return uint.Parse(text, format, culture);
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is uint)
            {
                string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.Value.UIntFormat);
                return !format.IsNullOrWhiteSpace() ? ((uint)value).ToString(format, culture) : value;
            }
            else
                return value;
        }
    }
}
