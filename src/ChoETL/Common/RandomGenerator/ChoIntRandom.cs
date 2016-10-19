using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoIntRandom
    {
        public static string NextAsText()
        {
            return NextAsText(int.MinValue, int.MaxValue);
        }

        public static string NextAsText(int minValue, int maxValue)
        {
            return Next(minValue, maxValue).ToString();
        }

        public static int Next()
        {
            return Next(int.MinValue, int.MaxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            ChoCryptoRandom random = new ChoCryptoRandom();
            return random.Next(minValue, maxValue);
        }
    }
}
