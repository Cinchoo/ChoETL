using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoDESCryptoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    return new ChoCryptoServiceProvider().DecryptDES(text);
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string text = value as string;
                if (!text.IsNullOrWhiteSpace())
                {
                    return new ChoCryptoServiceProvider().EncryptDES(text);
                }
            }

            return value;
        }
    }
}
