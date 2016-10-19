using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoCurrencyRandom
    {
        public static string NextAsText(string format = null)
        {
            return NextAsText(double.MinValue, double.MaxValue);
        }

        public static string NextAsText(double minValue, double maxValue, string format = null)
        {
            if (format.IsNullOrWhiteSpace())
                return Next(minValue, maxValue).ToString();
            else
                return Next(minValue, maxValue).ToString(format);
        }

        public static double Next()
        {
            return Next(double.MinValue + 100, double.MaxValue - 100);
        }

        public static double Next(double minValue, double maxValue)
        {
            ChoCryptoRandom random = new ChoCryptoRandom();
            return Math.Round(random.NextDouble() * (maxValue - minValue) + minValue, 2);
        }
    }
}
