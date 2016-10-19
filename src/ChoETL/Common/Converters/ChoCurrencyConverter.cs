using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    string format = parameter.GetValueAt<string>(0);
                    return !format.IsNullOrWhiteSpace() ? Double.Parse((string)value, format.ParseEnum<NumberStyles>()) : Double.Parse((string)value, NumberStyles.Currency);
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                string format = parameter.GetValueAt<string>(1);
                return !format.IsNullOrWhiteSpace() ? String.Format("{0:" + format + "}", value) : "{0:0.00}".FormatString(value);
            }
            else
                return value;
        }
    }
}
