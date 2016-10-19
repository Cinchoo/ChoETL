namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

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
}
