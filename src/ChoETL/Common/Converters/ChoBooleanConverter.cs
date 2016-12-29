using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [ChoTypeConverter(typeof(Boolean))]
    public class ChoBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
                return System.Convert.ToBoolean(System.Convert.ToInt32(value));
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                return ((bool)value) ? "1" : "0";
            }
            else
                return value;
        }
    }
}
