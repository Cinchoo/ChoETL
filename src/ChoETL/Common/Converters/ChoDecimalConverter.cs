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
    [ChoTypeConverter(typeof(Decimal))]
    public class ChoDecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    NumberStyles format = parameter.GetValueAt<NumberStyles>(0, ChoTypeConverterFormatSpec.Instance.Value.DecimalNumberStyle);
                    return Decimal.Parse(text, format, culture);
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Decimal)
            {
                string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.Value.DecimalFormat);
                return !format.IsNullOrWhiteSpace() ? ((Decimal)value).ToString(format, culture) : value;
            }
            else
                return value;
        }
    }
}
