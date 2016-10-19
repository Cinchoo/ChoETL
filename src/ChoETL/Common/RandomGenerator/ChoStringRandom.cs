using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection;

namespace ChoETL
{
    public static class ChoStringRandom
    {
        public static string Next(string chars, int length = 10)
        {
            ChoGuard.ArgumentNotNullOrEmpty(chars, "Chars");

            if (length <= 0)
                throw new ArgumentException("Length must be > 0.");

            var random = new ChoCryptoRandom();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
