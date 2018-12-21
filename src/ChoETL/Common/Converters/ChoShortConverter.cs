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
    [ChoTypeConverter(typeof(short))]
#if !NETSTANDARD2_0
    public class ChoShortConverter : IValueConverter
#else
    public class ChoShortConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = parameter.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.ShortNumberStyle);
                return format == null ? short.Parse(text, culture) : short.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is short && targetType == typeof(string))
            {
                short convValue = (short)value;
                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.ShortFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else
                return value;
        }
    }
}
