using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	public static class ChoEnumrableEx
    {
        public static IEnumerable<V> ZipOrDefault<T, U, V>(
            this IEnumerable<T> one,
            IEnumerable<U> two,
            Func<T, U, V> f)
        {
            using (var oneIter = one.GetEnumerator())
            {
                using (var twoIter = two.GetEnumerator())
                {
                    while (oneIter.MoveNext())
                    {
                        yield return f(oneIter.Current,
                            twoIter.MoveNext() ?
                                twoIter.Current :
                                default(U));
                    }

                    while (twoIter.MoveNext())
                    {
                        yield return f(oneIter.Current, twoIter.Current);
                    }
                }
            }
        }
        public static IDataReader AsDataReader(this IEnumerable collection, Action<IDictionary<string, Type>> membersDiscovered = null)
		{
			var e = new ChoStdDeferedObjectMemberDiscoverer(collection);
            if (membersDiscovered != null)
            {
                e.MembersDiscovered += (o, e1) =>
                {
                    membersDiscovered(e1.Value);
                };
            }
            var dr = new ChoEnumerableDataReader(e.AsEnumerable(), e);
			return dr;
		}

		private static KeyValuePair<string, Type>[] GetMembers(object item)
		{
			if (item is IDictionary)
			{
				List<KeyValuePair<string, Type>> list = new List<KeyValuePair<string, Type>>();
				foreach (var key in ((IDictionary)item).Keys)
					list.Add(new KeyValuePair<string, Type>(key.ToNString(), ((IDictionary)item)[key] == null ? typeof(object) : ((IDictionary)item)[key].GetType()));
				return list.ToArray();
			}
			if (item is IDictionary<string, object>)
			{
				List<KeyValuePair<string, Type>> list = new List<KeyValuePair<string, Type>>();
				foreach (var key in ((IDictionary<string, object>)item).Keys)
					list.Add(new KeyValuePair<string, Type>(key.ToNString(), ((IDictionary<string, object>)item)[key] == null ? typeof(object) : ((IDictionary<string, object>)item)[key].GetType()));
				return list.ToArray();
			}
			else if (item is IList)
				return GetMembers(((IList)item).OfType<object>().Select(i => i != null).FirstOrDefault());
			else
				return item.GetType().GetProperties().Select(kvp => new KeyValuePair<string, Type>(kvp.Name, kvp.PropertyType)).ToArray();
		}

		public static DataTable AsDataTable(this IEnumerable collection, string tableName = null, CultureInfo ci = null)
		{
			DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            if (ci != null)
            {
                dt.Locale = ci;
            }
            dt.Load(AsDataReader(collection));
			return dt;
		}

		public static int Fill(this IEnumerable collection, DataTable dt)
		{
			if (dt == null)
				throw new ArgumentException("Missing datatable.");

			var dr = AsDataReader(collection);
			DataTable dtSchema = dr.GetSchemaTable();

			if (dt.Columns.Count == 0)
				dt.Load(dr);
			else
			{
				var match = dt.Columns.OfType<DataColumn>().Select(dc => dc.ColumnName).Intersect(
					dtSchema.Rows.OfType<DataRow>().Select(dr1 => (string)dr1["ColumnName"]));
				if (match.Any())
				{
					while (dr.Read())
					{
						DataRow dataRow = dt.NewRow();
						foreach (string cn in match)
						{
							dataRow[((DataColumn)dt.Columns[cn])] = dr[cn];
						}
						dt.Rows.Add(dataRow);
					}
				}
				else
				{
					while (dr.Read())
					{
						DataRow dataRow = dt.NewRow();
						for (int i = 0; i < dt.Columns.Count; i++)
						{
							dataRow[((DataColumn)dt.Columns[i])] = dr[i];
						}
						dt.Rows.Add(dataRow);
					}
				}
			}

			return dt.Rows.Count;
		}
	}

	public sealed class ChoStdDeferedObjectMemberDiscoverer : IEnumerable<object>, IChoDeferedObjectMemberDiscoverer
	{
		public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
		private readonly IEnumerable _collection = null;
		private readonly ChoPeekEnumerator<object> _enumerator = null;

		public ChoStdDeferedObjectMemberDiscoverer(IEnumerable collection)
		{
			_collection = collection;
			_enumerator = new ChoPeekEnumerator<object>(_collection.OfType<object>(), (Func<object, bool?>)null);
			_enumerator.MembersDiscovered += (o, e) => MembersDiscovered.Raise(o, e);
		}

		public IEnumerator<object> GetEnumerator()
		{
			return _enumerator;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

}
