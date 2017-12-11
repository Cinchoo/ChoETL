namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Diagnostics.Contracts;
    using System.Linq;

    #endregion

    /// <summary>A priority queue.</summary>
    public class ChoPriorityQueue : ICollection, IEnumerable
    {
        #region Member Variables

        /// <summary>The binary heap on which the priority queue is based.</summary>
        private ChoBinaryHeap _heap;

        #endregion

        #region Construction
        /// <summary>Initialize the queue.</summary>
        public ChoPriorityQueue() { _heap = new ChoBinaryHeap(); }

        /// <summary>Initialize the queue.</summary>
        /// <param name="queue">The queue is intialized with a shalled-copy of this queue.</param>
        public ChoPriorityQueue(ChoPriorityQueue queue)
        {
            _heap = queue._heap.Clone();
        }
        #endregion

        #region Methods
        /// <summary>Enqueues an item to the priority queue.</summary>
        /// <param name="priority">The priority of the object to be enqueued.</param>
        /// <param name="value">The object to be enqueued.</param>
        public virtual void Enqueue(int priority, object value)
        {
            _heap.Insert(priority, value);
        }

        /// <summary>Dequeues an object from the priority queue.</summary>
        /// <returns>The top item (max priority) from the queue.</returns>
        public virtual object Dequeue()
        {
            return _heap.Remove();
        }

        /// <summary>Empties the queue.</summary>
        public virtual void Clear()
        {
            _heap.Clear();
        }

        public virtual object[] ToArray()
        {
            return _heap.ToArray();
        }

        public virtual Array ToArray(Type type)
        {
            return _heap.ToArray(type);
        }

        #endregion

        #region Implementation of ICollection

        /// <summary>Copies the priority queue to an array.</summary>
        /// <param name="array">The array to which the queue should be copied.</param>
        /// <param name="index">The starting index.</param>
        public virtual void CopyTo(System.Array array, int index) { _heap.CopyTo(array, index); }

        /// <summary>Determines whether the priority queue is synchronized.</summary>
        public virtual bool IsSynchronized { get { return _heap.IsSynchronized; } }

        public virtual bool IsBlockingQueue
        {
            get { return false; }
        }

        /// <summary>Gets the number of items in the queue.</summary>
        public virtual int Count { get { return _heap.Count; } }

        /// <summary>Gets the synchronization _syncRoot object for the queue.</summary>
        public virtual object SyncRoot { get { return _heap.SyncRoot; } }
        
        #endregion

        #region Implementation of IEnumerable
        
        /// <summary>Gets the enumerator for the queue.</summary>
        /// <returns>An enumerator for the queue.</returns>
        public virtual IEnumerator GetEnumerator() { return _heap.GetEnumerator(); }

        #endregion

        #region Synchronization

        /// <summary>Returns a synchronized wrapper around the queue.</summary>
        /// <param name="queue">The queue to be synchronized.</param>
        /// <returns>A synchronized priority queue.</returns>
        public static ChoPriorityQueue Synchronize(ChoPriorityQueue queue)
        {
            // Return the queue if it is already synchronized.  Otherwise, wrap it
            // with a synchronized wrapper.
            if (queue is ChoSyncPriorityQueue) return queue;

            return new ChoSyncPriorityQueue(queue);
        }
        
        #endregion

        #region BlockingQueue

        public static ChoPriorityQueue BlockingQueue(ChoPriorityQueue queue)
        {
            // Return the queue if it is already synchronized.  Otherwise, wrap it
            // with a synchronized wrapper.
            if (queue is ChoBlockingPriorityQueue) return queue;

            return new ChoBlockingPriorityQueue(queue);
        }

        #endregion

        #region ChoSyncPriorityQueue Class

        /// <summary>A synchronized ChoPriorityQueue.</summary>
        public class ChoSyncPriorityQueue : ChoPriorityQueue
        {
            #region Construction
            /// <summary>Initialize the priority queue.</summary>
            /// <param name="queue">The queue to be synchronized.</param>
            internal ChoSyncPriorityQueue(ChoPriorityQueue queue)
            {
                // NOTE: We're synchronizing just be using a synchronized heap!
                // This implementation will need to change if we get more state.
                if (!(_heap is ChoBinaryHeap.ChoSyncBinaryHeap))
                {
                    _heap = ChoBinaryHeap.Synchronize(_heap);
                }
            }
            #endregion
        }

        #endregion ChoSyncPriorityQueue Class

        #region ChoBlockingPriorityQueue Class

        [Serializable]
        private class ChoBlockingPriorityQueue : ChoPriorityQueue
        {
            #region Instance Data Members (Private)

            private readonly ChoPriorityQueue _queue;

            #endregion Instance Data Members (Private)

            #region Constructors

            internal ChoBlockingPriorityQueue(ChoPriorityQueue q)
            {
                _queue = q;
            }

            #endregion Constructors

            #region ChoQueue Overrides

            public override void Clear()
            {
                _queue.Clear();
            }

            public override void CopyTo(Array array, int arrayIndex)
            {
                _queue.CopyTo(array, arrayIndex);
            }

            public override object Dequeue()
            {
                lock (_queue)
                {
                    while (_queue.Count == 0)
                    {
                        Monitor.Wait(_queue);
                    }
                    return _queue.Dequeue();
                }
            }

            public override void Enqueue(int priority, object value)
            {
                lock (_queue)
                {
                    _queue.Enqueue(priority, value);
                    Monitor.Pulse(_queue);
                }
            }

            public override IEnumerator GetEnumerator()
            {
                return _queue.GetEnumerator();
            }

            public override object[] ToArray()
            {
                return _queue.ToArray();
            }

            // Properties
            public override int Count
            {
                get { return _queue.Count; }
            }

            public override bool IsSynchronized
            {
                get { return _queue.IsSynchronized; }
            }

            public override bool IsBlockingQueue
            {
                get { return true; }
            }

            public override object SyncRoot
            {
                get { return _queue.SyncRoot; }
            }

            #endregion ChoQueue Overrides
        }

        #endregion ChoBlockingPriorityQueue Class

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public class PriorityQueue<T>
    {
        private const int c_initialCapacity = 4;
        private readonly IComparer<T> m_comparer;
        private T[] m_items;
        private int m_count;

        public PriorityQueue()
          : this(Comparer<T>.Default)
        {
        }

        public PriorityQueue(IComparer<T> comparer)
          : this(comparer, c_initialCapacity)
        {
        }

        public PriorityQueue(IComparer<T> comparer, int capacity)
        {
            Contract.Requires(capacity >= 0);
            Contract.Requires(comparer != null);

            m_comparer = comparer;
            m_items = new T[capacity];
        }

        public PriorityQueue(IEnumerable<T> source)
          : this(source, Comparer<T>.Default)
        {
        }

        public PriorityQueue(IEnumerable<T> source, IComparer<T> comparer)
        {
            Contract.Requires(source != null);
            Contract.Requires(comparer != null);

            m_comparer = comparer;
            // In most cases queue that is created out of sequence
            // of items will be emptied step by step rather than
            // new items added and thus initially the queue is
            // not expanded but rather left full
            m_items = source.ToArray();
            m_count = m_items.Length;
            // Restore heap order
            FixWhole();
        }

        public int Capacity
        {
            get { return m_items.Length; }
        }

        public int Count
        {
            get { return m_count; }
        }

        public void Enqueue(T e)
        {
            m_items[m_count++] = e;
            // Restore heap if it was broken
            FixUp(m_count - 1);
            // Once items count reaches half of the queue capacity
            // it is doubled
            if (m_count >= m_items.Length / 2)
            {
                Expand(m_items.Length * 2);
            }
        }

        public T Dequeue()
        {
            //Contract.Requires<InvalidOperationException>(m_count > 0);
            if (m_count <= 0)
                return default(T);

            var e = m_items[0];
            m_items[0] = m_items[--m_count];
            // Restore heap if it was broken
            FixDown(0);
            // Once items count reaches one eighth  of the queue
            // capacity it is reduced to half so that items
            // still occupy one fourth (if it is reduced when
            // count reaches one fourth after reduce items will
            // occupy half of queue capacity and next enqueued item
            // will require queue expand)
            if (m_count <= m_items.Length / 8)
            {
                Expand(m_items.Length / 2);
            }

            return e;
        }

        public T Peek()
        {
            //Contract.Requires<InvalidOperationException>(m_count > 0);
            if (m_count <= 0)
                return default(T);

            return m_items[0];
        }

        private void FixWhole()
        {
            // Using bottom-up heap construction method enforce
            // heap property
            for (int k = m_items.Length / 2 - 1; k >= 0; k--)
            {
                FixDown(k);
            }
        }

        private void FixUp(int i)
        {
            // Make sure that starting with i-th node up to the root
            // the tree satisfies the heap property: if B is a child
            // node of A, then key(A) ≤ key(B)
            for (int c = i, p = Parent(c); c > 0; c = p, p = Parent(p))
            {
                if (Compare(m_items[p], m_items[c]) < 0)
                {
                    break;
                }
                Swap(m_items, c, p);
            }
        }

        private void FixDown(int i)
        {
            // Make sure that starting with i-th node down to the leaf
            // the tree satisfies the heap property: if B is a child
            // node of A, then key(A) ≤ key(B)
            for (int p = i, c = FirstChild(p); c < m_count; p = c, c = FirstChild(c))
            {
                if (c + 1 < m_count && Compare(m_items[c + 1], m_items[c]) < 0)
                {
                    c++;
                }
                if (Compare(m_items[p], m_items[c]) < 0)
                {
                    break;
                }
                Swap(m_items, p, c);
            }
        }

        private static int Parent(int i)
        {
            return (i - 1) / 2;
        }

        private static int FirstChild(int i)
        {
            return i * 2 + 1;
        }

        private int Compare(T a, T b)
        {
            return m_comparer.Compare(a, b);
        }

        private void Expand(int capacity)
        {
            Array.Resize(ref m_items, capacity);
        }

        private static void Swap(T[] arr, int i, int j)
        {
            var t = arr[i];
            arr[i] = arr[j];
            arr[j] = t;
        }
    }

}
