using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoEnumerableDataReader : ChoObjectDataReader
    {
        private readonly IEnumerator _enumerator;
        private readonly Type _type;
        private object _current;

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
            _dynamicMembersInfo = membersInfo;

            foreach (Type intface in collection.GetType().GetInterfaces())
            {
                if (intface.IsGenericType && intface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    _type = intface.GetGenericArguments()[0];
                }
            }

            if (_type == null && collection.GetType().IsGenericType)
            {
                _type = collection.GetType().GetGenericArguments()[0];

            }

            if (_type == null)
            {
                throw new ArgumentException(
                    "collection must be IEnumerable<>. Use other constructor for IEnumerable and specify type");
            }

            SetFields(_type, membersInfo);

            _enumerator = collection.GetEnumerator();

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
            bool returnValue = _enumerator.MoveNext();
            _current = returnValue ? _enumerator.Current : _type.IsValueType ? Activator.CreateInstance(_type) : null;
            //if (_current != null && _current is IChoDynamicRecord)
            //{
            //    //SetFields(_current as IChoDynamicRecord);
            //}
            //else
            //    SetFields(_type);

            return returnValue;
        }
    }
}
