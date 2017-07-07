using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEnumerable
    {
        public static IEnumerable<T> AsEnumerable<T>(Func<T> select, int count = 1)
        {
            if (select == null)
                yield break;

            if (count <= 0)
                yield break;
            else
            {
                for (int i = 0; i < count; i++)
                    yield return select();
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(T @this)
        {
            if (@this == null)
                yield break;

            yield return @this;
        }
    }
}
