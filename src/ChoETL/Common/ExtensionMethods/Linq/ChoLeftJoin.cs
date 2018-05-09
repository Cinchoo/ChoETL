using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	public static partial class ChoLinqEx
	{
		static KeyValuePair<TK, TV> Pair<TK, TV>(TK k, TV v) => new KeyValuePair<TK, TV>(k, v);

		public static IEnumerable<TResult> LeftJoin<TFirst, TSecond, TKey, TResult>(
				this IEnumerable<TFirst> first,
				IEnumerable<TSecond> second,
				Func<TFirst, TKey> firstKeySelector,
				Func<TSecond, TKey> secondKeySelector,
				Func<TFirst, TResult> firstSelector,
				Func<TFirst, TSecond, TResult> bothSelector,
				IEqualityComparer<TKey> comparer = null)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));
			if (firstKeySelector == null) throw new ArgumentNullException(nameof(firstKeySelector));
			if (secondKeySelector == null) throw new ArgumentNullException(nameof(secondKeySelector));
			if (firstSelector == null) throw new ArgumentNullException(nameof(firstSelector));
			if (bothSelector == null) throw new ArgumentNullException(nameof(bothSelector));

			return // TODO replace KeyValuePair<,> with (,) for clarity
				from j in first.GroupJoin(second, firstKeySelector, secondKeySelector,
										  (f, ss) => Pair(f, from s in ss select Pair(true, s)),
										  comparer)
				from s in j.Value.DefaultIfEmpty()
				select s.Key ? bothSelector(j.Key, s.Value) : firstSelector(j.Key);
		}

		public static IEnumerable<TResult> LeftJoin<TSource, TKey, TResult>(
				this IEnumerable<TSource> first,
				IEnumerable<TSource> second,
				Func<TSource, TKey> keySelector,
				Func<TSource, TResult> firstSelector,
				Func<TSource, TSource, TResult> bothSelector,
				IEqualityComparer<TKey> comparer = null)
		{
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
			return first.LeftJoin(second,
								  keySelector, keySelector,
								  firstSelector, bothSelector,
								  comparer);
		}
	}
}
