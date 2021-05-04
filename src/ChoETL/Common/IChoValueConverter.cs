using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    // Summary:
    //     Provides a way to apply custom logic to a binding.
    public interface IChoValueConverter
    {
        object Convert(object value, Type targetType, object parameter, CultureInfo culture);
        object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
    }

    public interface IChoHeaderConverter
    {
        string GetHeader(string name, string fieldName, object parameter, CultureInfo culture);
    }

    public interface IChoValueSelector
    {
        object ExtractValue(string name, string fieldName, object value, CultureInfo culture);
    }

    public interface IChoCollectionConverter
    {

    }
}
