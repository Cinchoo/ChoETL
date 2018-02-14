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
    [ChoTypeConverter(typeof(ChoCDATA))]
    public class ChoCDATAToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value is string)
                return new ChoCDATA(value as string);

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ChoCDATA && targetType == typeof(string))
            {
                return ((ChoCDATA)value).Value;
            }

            return value;
        }
    }
}
