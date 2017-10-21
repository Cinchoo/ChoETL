using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [ChoTypeConverter(typeof(DateTime))]
    public class ChoDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    DateTime outValue;
                    string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.DateTimeFormat);
                    if (!format.IsNullOrWhiteSpace())
                    {
                        if (DateTime.TryParseExact(text, format, culture, System.Globalization.DateTimeStyles.None, out outValue))
                            return outValue;
                        else if (DateTime.TryParse(text, out outValue))
                            return outValue;
                        else
                            return value;
                    }
                    return !format.IsNullOrWhiteSpace() ? DateTime.ParseExact(text, format, culture) : value;
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime && targetType == typeof(string))
            {
                string format = parameter.GetValueAt<string>(0, ChoTypeConverterFormatSpec.Instance.DateTimeFormat);
                return !format.IsNullOrWhiteSpace() ? ((DateTime)value).ToString(format, culture) : ((DateTime)value).ToLongDateString();
            }
            else
                return value;
        }
    }
}
