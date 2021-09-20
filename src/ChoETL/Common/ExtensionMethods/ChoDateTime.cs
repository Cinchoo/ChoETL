using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDateTime
    {
        public static bool TryParseExact(this string value, string format, IFormatProvider formatProvider, out DateTime result)
        {
            return TryParseExact(value, format, formatProvider, System.Globalization.DateTimeStyles.None, out result);

            //result = DateTime.MinValue;
            //try
            //{
            //    result = DateTime.ParseExact(value, format, formatProvider);
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }
        public static bool TryParseExact(this string value, string format, IFormatProvider formatProvider, System.Globalization.DateTimeStyles dateTimeStyles, out DateTime result)
        {
            return DateTime.TryParseExact(value, format, formatProvider, dateTimeStyles, out result);
        }
    }
}
