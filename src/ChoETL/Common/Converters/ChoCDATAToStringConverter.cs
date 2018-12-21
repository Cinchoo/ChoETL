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
    [ChoTypeConverter(typeof(ChoCDATA))]
#if !NETSTANDARD2_0
    public class ChoCDATAToStringConverter : IValueConverter
#else
    public class ChoCDATAToStringConverter : IChoValueConverter
#endif
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
