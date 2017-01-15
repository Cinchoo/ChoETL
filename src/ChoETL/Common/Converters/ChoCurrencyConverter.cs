using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [ChoTypeConverter(typeof(ChoCurrency))]
    public class ChoCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                if (culture == null)
                    culture = System.Threading.Thread.CurrentThread.CurrentCulture;

                string text = value as string;
                if (text.IsNullOrWhiteSpace())
                    text = "0";

                NumberStyles ns = parameter.GetValueAt<NumberStyles>(0, ChoTypeConverterFormatSpec.Instance.Value.CurrencyNumberStyle);
                return Double.Parse(text, ns, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                if (culture == null)
                    culture = System.Threading.Thread.CurrentThread.CurrentCulture;

                string format = parameter.GetValueAt<string>(1, ChoTypeConverterFormatSpec.Instance.Value.CurrencyFormat);
                if (format.IsNullOrWhiteSpace())
                    format = "C";

                return String.Format(culture, "{0:" + format + "}", value);
            }

            return value;
        }
    }
}
