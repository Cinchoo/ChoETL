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
    [ChoTypeConverter(typeof(byte))]
#if !NETSTANDARD2_0
    public class ChoByteConverter : IValueConverter
#else
    public class ChoByteConverter : IChoValueConverter
#endif
    {
        private NumberStyles? GetConvertTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.ByteNumberStyle;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.ByteNumberStyle);
        }
        private string GetConvertBackTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.ByteFormat;

            return parameter.GetValueAt(1, ChoTypeConverterFormatSpec.Instance.ByteFormat);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles? format = GetConvertTypeFormat(parameter); //.GetValueAt<NumberStyles?>(0, ChoTypeConverterFormatSpec.Instance.ByteNumberStyle);
                return format == null ? byte.Parse(text, culture) : byte.Parse(text, format.Value, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is byte && targetType == typeof(string))
            {
                byte convValue = (byte)value;
                string format = GetConvertBackTypeFormat(parameter); //.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.ByteFormat);
                return !format.IsNullOrWhiteSpace() ? convValue.ToString(format, culture) : convValue.ToString(culture);
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
