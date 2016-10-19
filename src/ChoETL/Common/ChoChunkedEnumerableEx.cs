using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoChunkedEnumerableEx
    {
        class ChoChunkedEnumerable<T> : IEnumerable<T>
        {
            class ChoChildEnumerator : IEnumerator<T>
            {
                ChoChunkedEnumerable<T> parent;
                int position;
                bool done = false;
                T current;


                public ChoChildEnumerator(ChoChunkedEnumerable<T> parent)
                {
                    this.parent = parent;
                    position = -1;
                    parent.wrapper.AddRef();
                }

                public T Current
                {
                    get
                    {
                        if (position == -1 || done)
                        {
                            throw new InvalidOperationException();
                        }
                        return current;

                    }
                }

                public void Dispose()
                {
                    if (!done)
                    {
                        done = true;
                        parent.wrapper.RemoveRef();
                    }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    position++;

                    if (position + 1 > parent.chunkSize)
                    {
                        done = true;
                    }

                    if (!done)
                    {
                        done = !parent.wrapper.Get(position + parent.start, out current);
                    }

                    return !done;

                }

                public void Reset()
                {
                    // per http://msdn.microsoft.com/en-us/library/system.collections.ienumerator.reset.aspx
                    throw new NotSupportedException();
                }
            }

            ChoEnumeratorWrapper<T> wrapper;
            int chunkSize;
            int start;

            public ChoChunkedEnumerable(ChoEnumeratorWrapper<T> wrapper, int chunkSize, int start)
            {
                this.wrapper = wrapper;
                this.chunkSize = chunkSize;
                this.start = start;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new ChoChildEnumerator(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }

        class ChoEnumeratorWrapper<T>
        {
            public ChoEnumeratorWrapper(IEnumerable<T> source)
            {
                SourceEumerable = source;
            }
            IEnumerable<T> SourceEumerable { get; set; }

            Enumeration currentEnumeration;

            class Enumeration
            {
                public IEnumerator<T> Source { get; set; }
                public int Position { get; set; }
                public bool AtEnd { get; set; }
            }

            public bool Get(int pos, out T item)
            {

                if (currentEnumeration != null && currentEnumeration.Position > pos)
                {
                    currentEnumeration.Source.Dispose();
                    currentEnumeration = null;
                }

                if (currentEnumeration == null)
                {
                    currentEnumeration = new Enumeration { Position = -1, Source = SourceEumerable.GetEnumerator(), AtEnd = false };
                }

                item = default(T);
                if (currentEnumeration.AtEnd)
                {
                    return false;
                }

                while (currentEnumeration.Position < pos)
                {
                    currentEnumeration.AtEnd = !currentEnumeration.Source.MoveNext();
                    currentEnumeration.Position++;

                    if (currentEnumeration.AtEnd)
                    {
                        return false;
                    }

                }

                item = currentEnumeration.Source.Current;

                return true;
            }

            int refs = 0;

            // needed for dispose semantics 
            public void AddRef()
            {
                refs++;
            }

            public void RemoveRef()
            {
                refs--;
                if (refs == 0 && currentEnumeration != null)
                {
                    var copy = currentEnumeration;
                    currentEnumeration = null;
                    copy.Source.Dispose();
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            if (chunksize < 1) throw new InvalidOperationException();

            var wrapper = new ChoEnumeratorWrapper<T>(source);

            int currentPos = 0;
            T ignore;
            try
            {
                wrapper.AddRef();
                while (wrapper.Get(currentPos, out ignore))
                {
                    yield return new ChoChunkedEnumerable<T>(wrapper, chunksize, currentPos);
                    currentPos += chunksize;
                }
            }
            finally
            {
                wrapper.RemoveRef();
            }
        }
    }
}
