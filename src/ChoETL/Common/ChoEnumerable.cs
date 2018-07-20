using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEnumerable
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
                yield return element;
            }
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
            if (!typeof(T).IsValueType && (object)@this == null)
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

        public static IEnumerable<IEnumerable<T>> GroupWhile<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            using (var iterator = source.GetEnumerator())
            {
                if (!iterator.MoveNext())
                    yield break;

                List<T> list = new List<T>() { iterator.Current };

                while (iterator.MoveNext())
                {
                    if (predicate(iterator.Current))
                    {
                        list.Add(iterator.Current);
                    }
                    else
                    {
                        yield return list;
                        list = new List<T>() { iterator.Current };
                    }
                }
                yield return list;
            }
        }

        public static IEnumerable<KeyValuePair<T, object[]>> ToMasterDetail<T>(this IEnumerable source, Func<object, bool> predicate = null)
        {
            return ToMasterDetail<T, object>(source, predicate);
        }

        public static IEnumerable<KeyValuePair<T1, T2[]>> ToMasterDetail<T1, T2>(this IEnumerable source, Func<object, bool> predicate = null)
        {
            if (source == null)
                yield break;

            if (predicate == null)
                predicate = (obj) => obj != null && obj.GetType() == typeof(T1);

            foreach (var item in source.AsTypedEnumerable<object>().GroupWhile<object>(src => !predicate(src))
                .Select(group => new KeyValuePair<T1, T2[]>(
                    group.First().CastTo<T1>(),
                    group.Skip(1).OfType<T2>().ToArray())
                ))
            {
                yield return item;
            }
        }
    }
}
