using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPeekEnumerator<T> : IEnumerator<T>, IChoDeferedObjectMemberDiscoverer
    {
        private readonly IEnumerable<T> _enumerable;
        private IEnumerator<T> _enumerator;
        private T _peek;
        private bool _didPeek;
        private Func<T, bool?> _filterFunc = (T) => false;
        private Func<T, Tuple<bool?, T>> _filterFuncEx = (T) => new Tuple<bool?, T>(false, T);
        private T _current;
        private bool _firstItem = true;

        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;

        public ChoPeekEnumerator(IEnumerable<T> enumerable)
        {
            ChoGuard.ArgumentNotNull(enumerable, "enumerable");
            _enumerable = enumerable;
            Reset();
        }

        public ChoPeekEnumerator(IEnumerable<T> enumerable, Func<T, bool?> filterFunc)
        {
            ChoGuard.ArgumentNotNull(enumerable, "enumerable");
            _enumerable = enumerable;
            if (filterFunc != null)
                _filterFunc = filterFunc;
            _filterFuncEx = null;

            Reset();
        }

        public ChoPeekEnumerator(IEnumerable<T> enumerable, Func<T, Tuple<bool?, T>> filterFuncEx)
        {
            ChoGuard.ArgumentNotNull(enumerable, "enumerable");
            _enumerable = enumerable;
            if (_filterFuncEx != null)
                _filterFuncEx = filterFuncEx;
            _filterFunc = null;

            Reset();
        }

        #region IEnumerator implementation
        public virtual bool MoveNext()
        {
            return _didPeek ? !(_didPeek = false) : MoveToNext();
        }

        public virtual void Reset()
        {
            _firstItem = true;
            _enumerator = _enumerable.GetEnumerator();
            _didPeek = false;
        }

        object IEnumerator.Current { get { return this.Current; } }

        #endregion

        #region IDisposable implementation

        public virtual void Dispose()
        {
            _firstItem = true;
            _enumerator.Dispose();
        }
        #endregion

        #region IEnumerator implementation
        public virtual T Current
        {
            get { return _didPeek ? _peek : _current; }
        }
        #endregion

        private void TryFetchPeek()
        {
            if (!_didPeek && (_didPeek = MoveToNext()))
            {
                _peek = _current;
            }
        }

        public T Peek
        {
            get
            {
                TryFetchPeek();
                if (!_didPeek)
                    return default(T);
                    //throw new InvalidOperationException("Enumeration already finished.");

                return _peek;
            }
        }

        private bool MoveToNext()
        {
            bool ret = false;
            _current = default(T);
            while (ret = _enumerator.MoveNext())
            {
                if (_filterFuncEx != null)
                {
                    Tuple<bool?, T> filterRet = null;
                    filterRet = _filterFuncEx(_enumerator.Current);
                    if (filterRet == null || filterRet.Item1 == null)
                    {
                        _current = default(T);
                        return false;
                    }

                    if (filterRet.Item1.Value)
                        continue;

                    _current = filterRet.Item2;
                }
                else if (_filterFunc != null)
                {
                    bool? filterRet = null;
                    filterRet = _filterFunc(_enumerator.Current);
                    if (filterRet == null)
                    {
                        _current = default(T);
                        return false;
                    }

                    if (filterRet.Value)
                        continue;

                    _current = _enumerator.Current;
                }

                if (_current != null && _firstItem)
                {
                    _firstItem = false;
                    MembersDiscovered.Raise(this, new ChoEventArgs<IDictionary<string, Type>>(GetMembers(_current).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
                }
                break;
            }

            return ret;
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
    }
}
