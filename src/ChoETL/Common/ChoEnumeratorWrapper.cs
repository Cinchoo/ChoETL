using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoEnumeratorWrapper
    {
        public static IEnumerable<T> BuildEnumerable<T>(
                Func<bool> moveNext, Func<T> current, Action dispose = null)
        {
            var po = new ChoEnumeratorWrapperInternal<T>(moveNext, current);
            foreach (var s in po)
                yield return s;

            dispose?.Invoke();
        }

        private class ChoEnumeratorWrapperInternal<T>
        {
            private readonly Func<bool> _moveNext;
            private readonly Func<T> _current;

            public ChoEnumeratorWrapperInternal(Func<bool> moveNext, Func<T> current)
            {
                ChoGuard.ArgumentNotNull(moveNext, "MoveNext");
                ChoGuard.ArgumentNotNull(current, "Current");

                _moveNext = moveNext;
                _current = current;
            }

            public ChoEnumeratorWrapperInternal<T> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                return _moveNext();
            }

            public T Current
            {
                get { return _current(); }
            }
        }
    }
}