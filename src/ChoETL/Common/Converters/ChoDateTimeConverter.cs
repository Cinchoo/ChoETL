using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    string format = parameter.GetValueAt<string>(0);
                    return format != null ? DateTime.ParseExact((string)value, format, null) : value;
                }
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime)
            {
                string format = parameter.GetValueAt<string>(0);
                return format != null ? ((DateTime)value).ToString(format) : value;
            }
            else
                return value;
        }
    }
}
