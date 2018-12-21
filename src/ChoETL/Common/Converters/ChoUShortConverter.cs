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
    [ChoTypeConverter(typeof(ushort))]
#if !NETSTANDARD2_0
    public class ChoUShortConverter : IValueConverter
#else
    public class ChoUShortConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = parameter.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.UShortNumberStyle);
                return format == null ? ushort.Parse(text, culture) : ushort.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ushort && targetType == typeof(string))
            {
                ushort convValue = (ushort)value;
                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.UShortFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else
                return value;
        }
    }
}
