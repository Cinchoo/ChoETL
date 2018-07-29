using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoConvertible
    {
        bool Convert(string propName, object propValue, CultureInfo culture, out object convPropValue);
        bool ConvertBack(string propName, object propValue, Type targetType, CultureInfo culture, out object convPropValue);
    }
}
