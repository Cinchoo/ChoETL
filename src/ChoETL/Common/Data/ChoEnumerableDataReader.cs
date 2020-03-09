using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoDeferedObjectMemberDiscoverer
    {
        event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
    }

    public class ChoEnumerableDataReader : ChoObjectDataReader
    {
        private readonly IEnumerator _enumerator;
        private readonly Type _type;
        private object _current;
        private bool _isDeferred = false;
        private bool _firstElementExists = false;

        /// <summary>
        /// Create an IDataReader over an instance of IEnumerable&lt;>.
        /// 
        /// Note: anonymous type arguments are acceptable.
        /// 
        /// Use other constructor for IEnumerable.
        /// </summary>
        /// <param name="collection">IEnumerable&lt;>. For IEnumerable use other constructor and specify type.</param>
        public ChoEnumerableDataReader(IEnumerable collection, KeyValuePair<string, Type>[] membersInfo = null)
        {
            ChoGuard.ArgumentNotNull(collection, "Collection");

            _type = GetElementType(collection);
            _dynamicMembersInfo = membersInfo;
            SetFields(_type, membersInfo);

            _enumerator = collection.GetEnumerator();

        }

        public ChoEnumerableDataReader(IEnumerable collection, IChoDeferedObjectMemberDiscoverer dom)
        {
            ChoGuard.ArgumentNotNull(collection, "Collection");
            ChoGuard.ArgumentNotNull(dom, "DeferedObjectMemberDiscoverer");
            _isDeferred = true;
            _type = GetElementType(collection);

            dom.MembersDiscovered += (o, e) =>
            {
                _dynamicMembersInfo = e.Value.ToArray();
                SetFields(_type, e.Value.ToArray());
            };

            _enumerator = collection.GetEnumerator();
            _firstElementExists = _enumerator.MoveNext();

            if (_enumerator.Current is ChoDynamicObject && ((ChoDynamicObject)_enumerator.Current).IsHeaderOnlyObject)
                _firstElementExists = false;
        }

        /// <summary>
        /// Create an IDataReader over an instance of IEnumerable.
        /// Use other constructor for IEnumerable&lt;>
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="elementType"></param>
        public ChoEnumerableDataReader(IEnumerable collection, Type elementType, KeyValuePair<string, Type>[] dynamicMembersInfo = null)
            : base(elementType, dynamicMembersInfo)
        {
            _type = elementType;
            _enumerator = collection.GetEnumerator();
        }

        /// <summary>
        /// Helper method to create generic lists from anonymous type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IList ToGenericList(Type type)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { type }));
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <returns>
        /// The <see cref="T:System.Object"/> which will contain the field value upon return.
        /// </returns>
        /// <param name="i">The index of the field to find. 
        /// </param><exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. 
        /// </exception><filterpriority>2</filterpriority>
        public override object GetValue(int i)
        {
            if (i < 0 || i >= Fields.Count)
            {
                throw new IndexOutOfRangeException();
            }

            return Fields[i].GetValue(_current);
        }

        /// <summary>
        /// Advances the <see cref="T:System.Data.IDataReader"/> to the next record.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override bool Read()
        {
            bool returnValue = false;
            if (_isDeferred)
            {
                returnValue = _firstElementExists;
                _isDeferred = false;
            }
            else
            {
                returnValue = _enumerator.MoveNext();
            }
            _current = returnValue ? _enumerator.Current : _type.IsValueType ? ChoActivator.CreateInstance(_type) : null;
            return returnValue;
        }

        private Type GetElementType(IEnumerable collection)
        {
            Type type = null;
            foreach (Type intface in collection.GetType().GetInterfaces())
            {
                if (intface.IsGenericType && intface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    type = intface.GetGenericArguments()[0];
                }
            }

            if (type == null && collection.GetType().IsGenericType)
            {
                type = collection.GetType().GetGenericArguments()[0];

            }

            if (type == null)
            {
                throw new ArgumentException(
                    "collection must be IEnumerable<>. Use other constructor for IEnumerable and specify type");
            }
            return type;
        }
    }
}
