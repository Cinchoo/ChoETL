using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVConverter : IChoValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var genericCSVReader = typeof(ChoCSVReader<>).MakeGenericType(targetType);
            dynamic readerInstance = ChoActivator.CreateInstance(genericCSVReader, new object[] { new StringBuilder(value as string), null });
            var disposable = readerInstance as IDisposable;
            using (disposable)
            {
                readerInstance.ThrowAndStopOnMissingField(false);

                var recs = readerInstance.ToArray();
                return (recs as IList)?.OfType<object>().FirstOrDefault();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
