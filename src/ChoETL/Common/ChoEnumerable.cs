using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEnumerable
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action(element);
        }
        public static IEnumerable<T> AsEnumerableFrom<T>(Func<T> select, int count = 1)
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
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            List<T> nextbatch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                nextbatch.Add(item);
                if (nextbatch.Count == batchSize)
                {
                    yield return nextbatch;
                    nextbatch = new List<T>();
                }
            }

            if (nextbatch.Count > 0)
                yield return nextbatch;
        }
    }
}
