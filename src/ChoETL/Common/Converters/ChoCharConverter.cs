using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(Char))]
#if !NETSTANDARD2_0
    public class ChoCharConverter : IValueConverter
#else
    public class ChoCharConverter : IChoValueConverter
#endif
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                return text.FirstOrDefault();
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is char ch && targetType == typeof(string))
            {
                if (ch == ChoCharEx.NUL)
                    return String.Empty;
                else
                    return ch.ToString();
            }
            else if (value == DBNull.Value)
                return null;
            else
                return value;
        }
    }
}
