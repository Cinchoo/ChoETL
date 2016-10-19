using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDateTimeRandom
    {
        static readonly ChoCryptoRandom _rnd = new ChoCryptoRandom();
                
        public static DateTime Next()
        {
            return Next(DateTime.Today, DateTime.Today.AddYears(50));
        }

        public static DateTime Next(DateTime from, DateTime to)
        {
            var range = to - from;
            var randTimeSpan = new TimeSpan((long)(_rnd.NextDouble() * range.Ticks));
            return from + randTimeSpan;
        }

        public static string NextAsText(string format)
        {
            return Next().ToString(format);
        }

        public static string NextAsText(DateTime from, DateTime to, string format)
        {
            return Next(from, to).ToString(format);
        }
    }
}
