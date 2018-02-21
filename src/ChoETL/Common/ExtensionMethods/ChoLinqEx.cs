using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoLinqEx
    {
        public static IEnumerable<T> Touch<T>(this IEnumerable<T> items) =>
            items == null || items.Count() == 0 ? Enumerable.Repeat(Activator.CreateInstance<T>(), 1) : items;
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

		public static string GetMemberName<TClass, TField>(this Expression<Func<TClass, TField>> field)
		{
			return (field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member.Name;
		}
	}
}
