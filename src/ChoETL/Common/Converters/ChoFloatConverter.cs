using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(float))]
#if !NETSTANDARD2_0
    public class ChoFloatConverter : IValueConverter
#else
    public class ChoFloatConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = parameter.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.FloatNumberStyle);
                if (format == null)
                {
                    float decResult = 0;
                    if (float.TryParse(text, NumberStyles.Currency, culture, out decResult))
                        return decResult;
                }
                return format == null ? float.Parse(text, culture) : float.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is float && targetType == typeof(string))
            {
                float convValue = (float)value;
                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.FloatFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else
                return value;
        }
    }
}
