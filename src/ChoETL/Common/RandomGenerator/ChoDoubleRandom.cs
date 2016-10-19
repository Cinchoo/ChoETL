using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDoubleRandom
    {
        public static string NextAsText()
        {
            return NextAsText(double.MinValue, double.MaxValue);
        }

        public static string NextAsText(double minValue, double maxValue)
        {
            return Next(minValue, maxValue).ToString();
        }

        public static double Next()
        {
            return Next(double.MinValue, double.MaxValue);
        }

        public static double Next(double minValue, double maxValue)
        {
            ChoCryptoRandom random = new ChoCryptoRandom();
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }
}
