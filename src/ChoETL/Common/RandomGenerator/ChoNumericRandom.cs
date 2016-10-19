using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoNumericRandom
    {
        private const string CHARS = "1234567890";

        public static string Next(int length = 10)
        {
            return ChoStringRandom.Next(CHARS, length);
        }
    }
}
