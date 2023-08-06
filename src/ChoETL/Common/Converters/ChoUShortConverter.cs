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
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.UShortNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.UShortNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.UShortFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.UShortFormat);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.UShortNumberStyle);
                return format == null ? ushort.Parse(text, culture) : ushort.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ushort && targetType == typeof(string))
            {
                ushort convValue = (ushort)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.UShortFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
