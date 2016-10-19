using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoNumericConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (!((string)value).IsNumber())
                    throw new ApplicationException("'{0}' value is not number.".FormatString(value));
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (!((string)value).IsNumber())
                    throw new ApplicationException("'{0}' value is not number.".FormatString(value));
            }
            return value;
        }
    }
}
