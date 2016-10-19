using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoCharRandom
    {
        public static char Next()
        {
            return ChoAlphaNumericRandom.Next(1)[0];
        }

        public static char Next(string chars)
        {
            ChoGuard.ArgumentNotNullOrEmpty(chars, "Chars");

            return ChoStringRandom.Next(chars, 1)[0];
        }
    }
}
