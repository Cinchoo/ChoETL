using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEnumerable
    {
        public static IEnumerable<T> AsEnumerable<T>(Func<T> select)
        {
            if (select == null)
                yield break;

            yield return select();
        }

        public static IEnumerable<T> AsEnumerable<T>(T @this)
        {
            if (@this == null)
                yield break;

            yield return @this;
        }
    }
}
