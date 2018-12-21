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
    [ChoTypeConverter(typeof(Double))]
#if !NETSTANDARD2_0
    public class ChoDoubleConverter : IValueConverter
#else
    public class ChoDoubleConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = parameter.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.DoubleNumberStyle);
                if (format == null)
                {
                    Double decResult = 0;
                    if (Double.TryParse(text, NumberStyles.Currency, culture, out decResult))
                        return decResult;
                }
                return format == null ? Double.Parse(text, culture) : Double.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Double && targetType == typeof(string))
            {
                Double convValue = (Double)value;
                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.DoubleFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else
                return value;
        }
    }
}
