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
	public enum CompareStatus { Unchanged, Changed, New, Deleted }

    public static class ChoEnumerableEx
    {
        public class CompareResult<T>
        {
            public T MasterRecord { get; private set; }
            public T DetailRecord { get; private set; }
            public CompareStatus Status { get; private set; }

            public CompareResult(T masterRecord, T detailRecord, CompareStatus status)
            {
                MasterRecord = masterRecord;
                DetailRecord = detailRecord;
                Status = status;
            }
        }
        public static object ConvertToArray(this IList collection)
        {
            // guess type
            Type type;
            if (collection.GetType().IsGenericType && collection.GetType().GetGenericArguments().Length == 0)
                type = collection.GetType().GetGenericArguments()[0];
            else if (collection.Count > 0)
                type = collection[0].GetType();
            else
                throw new NotSupportedException("Failed to identify collection type for: " + collection.GetType());

            var array = (object[])Array.CreateInstance(type, collection.Count);
            for (int i = 0; i < array.Length; ++i)
                array[i] = collection[i];
            return array;
        }

        public static IEnumerable<TResult> OfBaseType<TResult>(this IEnumerable source)
        {
            if (source == null)
                yield break;

            Type resultType = typeof(TResult);
            foreach (var item in source)
            {
                if (item == null)
                    yield return default(TResult);
                if (resultType.IsAssignableFrom(item.GetType()))
                    yield return (TResult)item;
            }
        }

        public static object ConvertToArray(this IList collection, Type arrayType)
        {
            var array = (object[])Array.CreateInstance(arrayType, collection.Count);
            for (int i = 0; i < array.Length; ++i)
            {
                var obj = collection[i];

                // if it's not castable, try to convert it
                if (!arrayType.IsInstanceOfType(obj))
                    obj = Convert.ChangeType(obj, arrayType);

                array[i] = obj;
            }

            return array;
        }

        public static IEnumerable<CompareResult<ChoDynamicObject>> Compare(this IEnumerable<ChoDynamicObject> master, IEnumerable<ChoDynamicObject> detail,
            string csvKeyColumns = null, string csvCompareColumns = null, Func<ChoDynamicObject, ChoDynamicObject, int> comparer = null,
            IDictionary<Type, IComparer> typeComparerers = null)
        {
            var equalityComparer = new ChoDynamicObjectEqualityComparer(csvKeyColumns);
            var comparerObj = comparer == null ? new ChoDynamicObjectComparer(csvKeyColumns) : new ChoDynamicObjectComparer(comparer);
            if (typeComparerers != null)
                comparerObj.TypeComparerers = new System.Collections.Concurrent.ConcurrentDictionary<Type, IComparer>(typeComparerers);
            var changeComparer = csvCompareColumns.IsNullOrEmpty() ? new ChoDynamicObjectEqualityComparer((string)null) : new ChoDynamicObjectEqualityComparer(csvCompareColumns);

            return Compare(master, detail, equalityComparer, comparerObj, changeComparer);
        }

        //public static IEnumerable<CompareResult<ChoDynamicObject>> Compare(this IEnumerable<ChoDynamicObject> master, IEnumerable<ChoDynamicObject> detail,
        //    string[] keyColumns = null, string[] compareColumns = null, Func<ChoDynamicObject, ChoDynamicObject, int> comparer = null,
        //    IDictionary<Type, IComparer> typeComparerers = null)
        //{
        //    var equalityComparer = new ChoDynamicObjectEqualityComparer(keyColumns);
        //    var comparerObj = comparer == null ? new ChoDynamicObjectComparer(keyColumns) : new ChoDynamicObjectComparer(comparer);
        //    if (typeComparerers != null)
        //        comparerObj.TypeComparerers = new System.Collections.Concurrent.ConcurrentDictionary<Type, IComparer>(typeComparerers);
        //    var changeComparer = compareColumns.IsNullOrEmpty() ? new ChoDynamicObjectEqualityComparer((string)null) : new ChoDynamicObjectEqualityComparer(compareColumns);

        //    return Compare(master, detail, equalityComparer, comparerObj, changeComparer);
        //}

        public static IEnumerable<CompareResult<T>> Compare<T>(this IEnumerable<T> master, IEnumerable<T> detail,
            IEqualityComparer<T> equalityComparer = null,
            Func<T, T, int> comparerFunc = null,
            IEqualityComparer<T> changeComparer = null,
            IDictionary<Type, IComparer> typeComparerers = null)
        {
            var comparer = comparerFunc == null ? new ChoLamdaComparer<T>(comparerFunc) : null;
            return Compare(master, detail, equalityComparer, comparer, changeComparer, typeComparerers);
        }

        public static IEnumerable<CompareResult<T>> Compare<T>(this IEnumerable<T> master, IEnumerable<T> detail,
            IEqualityComparer<T> equalityComparer = null,
            IComparer<T> comparer = null,
            IEqualityComparer<T> changeComparer = null,
            IDictionary<Type, IComparer> typeComparerers = null)
        {
            ChoGuard.ArgumentNotNull(master, "Master");
            ChoGuard.ArgumentNotNull(detail, "Detail");
            ChoGuard.ArgumentNotNull(equalityComparer, nameof(equalityComparer));
            ChoGuard.ArgumentNotNull(comparer, nameof(comparer));

            var r1 = master.GetEnumerator();
            var r2 = detail.GetEnumerator();

            var b1 = r1.MoveNext();
            var b2 = r2.MoveNext();

            T rec1 = default(T);
            T rec2 = default(T);

            if (changeComparer == null)
                changeComparer = equalityComparer;

            while (true)
            {
                CompareStatus status = CompareStatus.Unchanged;

                if (!b1 && !b2)
                    break;
                else if (b1 && b2)
                {
                    rec1 = r1.Current;
                    rec2 = r2.Current;

                    if (equalityComparer.Equals(rec1, rec2))
                    {
                        status = changeComparer.Equals(rec1, rec2) ? CompareStatus.Unchanged : CompareStatus.Changed;
                        b1 = r1.MoveNext();
                        b2 = r2.MoveNext();
                    }
                    else if (comparer.Compare(rec1, rec2) < 0)
                    {
                        status = CompareStatus.Deleted;
                        b1 = r1.MoveNext();
                        rec2 = default(T);
                    }
                    else
                    {
                        rec1 = default(T);
                        status = CompareStatus.New;
                        b2 = r2.MoveNext();
                    }
                }
                else if (b1)
                {
                    rec1 = r1.Current;
                    rec2 = default(T);
                    status = CompareStatus.Deleted;
                    b1 = r1.MoveNext();
                }
                else if (b2)
                {
                    rec1 = default(T);
                    rec2 = r2.Current;
                    status = CompareStatus.New;
                    b2 = r2.MoveNext();
                }
                else
                    break;

                yield return new CompareResult<T>(rec1, rec2, status);
            }
        }

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
        public static IDataReader AsDataReader(this IEnumerable collection, 
            Action<IDictionary<string, Type>> membersDiscovered = null, string[] selectedFields = null, 
            string[] excludeFields = null)
		{
			var e = new ChoStdDeferedObjectMemberDiscoverer(collection);
            if (membersDiscovered != null)
            {
                e.MembersDiscovered += (o, e1) =>
                {
                    membersDiscovered(e1.Value);
                };
            }
            else
            {
                e.MembersDiscovered += (o, e1) =>
                {
                    if (excludeFields != null)
                    {
                        foreach (var ef in excludeFields)
                        {
                            if (e1.Value.ContainsKey(ef))
                                e1.Value.Remove(ef);
                        }

                    }
                    else if (selectedFields != null)
                    {
                        foreach (var sf in e1.Value.Keys.Except(selectedFields).ToArray())
                        {
                            e1.Value.Remove(sf);
                        }
                    }
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

		public static DataTable AsDataTable(this IEnumerable collection, string tableName = null, 
            CultureInfo ci = null, Action<IDictionary<string, Type>> membersDiscovered = null,
            string[] selectedFields = null, string[] excludeFields = null)
		{
			DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            if (ci != null)
            {
                dt.Locale = ci;
            }
            dt.Load(AsDataReader(collection, membersDiscovered, selectedFields, excludeFields));
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
        public static int FillByIndex(this IEnumerable collection, DataTable dt)
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
                if (false)
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

        public Type ItemType => _collection != null ? _collection.GetType().GetItemType() : typeof(object);

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
