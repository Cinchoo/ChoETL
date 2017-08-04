using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPeekEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerable<T> _enumerable;
        private IEnumerator<T> _enumerator;
        private T _peek;
        private bool _didPeek;
        private Func<T, bool?> _filterFunc = (T) => false;
        private T _current;

        public ChoPeekEnumerator(IEnumerable<T> enumerable, Func<T, bool?> filterFunc = null)
        {
            ChoGuard.ArgumentNotNull(enumerable, "enumerable");
            _enumerable = enumerable;
            if (filterFunc != null)
                _filterFunc = filterFunc;

            Reset();
        }

        #region IEnumerator implementation
        public virtual bool MoveNext()
        {
            return _didPeek ? !(_didPeek = false) : MoveToNext();
        }

        public virtual void Reset()
        {
            _enumerator = _enumerable.GetEnumerator();
            _didPeek = false;
        }

        object IEnumerator.Current { get { return this.Current; } }

        #endregion

        #region IDisposable implementation

        public virtual void Dispose()
        {
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
            bool? filterRet = null;
            _current = default(T);
            while (ret = _enumerator.MoveNext())
            {
                if (_filterFunc != null)
                    filterRet = _filterFunc(_enumerator.Current);

                if (filterRet == null)
                {
                    _current = default(T);
                    return false;
                }

                if (filterRet.Value)
                    continue;

                _current = _enumerator.Current;
                break;
            }

            return ret;
        }
    }
}
