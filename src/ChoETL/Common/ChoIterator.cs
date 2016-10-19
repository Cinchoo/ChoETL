using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoIterator<T> : IDisposable
    {
        private readonly IEnumerator<T> _enumerator;

        public ChoIterator(IEnumerable<T> enumerable)
        {
            ChoGuard.ArgumentNotNull(enumerable, "enumerable");
            _enumerator = enumerable.GetEnumerator();
        }

        public ChoIterator(IEnumerator<T> enumerator)
        {
            ChoGuard.ArgumentNotNull(enumerator, "enumerator");
            _enumerator = enumerator;
        }

        public T Next()
        {
            _enumerator.MoveNext();
            return _enumerator.Current;
        }

        public void Dispose()
        {
            _enumerator.Reset();
        }
    }
}
