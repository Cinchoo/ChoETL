using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoExternalSorter<T>
    {
        private readonly IComparer<T> _comparer;
        private readonly int _capacity;
        private readonly int _mergeCount;

        protected ChoExternalSorter(IComparer<T> comparer, int capacity = 10000, int mergeCount = 10)
        {
            _comparer = comparer;
            _capacity = capacity;
            _mergeCount = mergeCount;
        }

        public IEnumerable<T> Sort(IEnumerable<T> unsorted)
        {
            var runs = Distribute(unsorted);
            return Merge(runs);
        }

        // Sorts unsorted file and returns sorted file name
        //public string Sort(string unsorted)
        //{
        //    var runs = Distribute(unsorted);
        //    return Merge(runs);
        //}

        // Write run to disk and return created file name
        protected abstract string Write(IEnumerable<T> run);
        // Read run from file with given name
        protected abstract IEnumerable<T> Read(string name);

        // Merge step in this implementation is simpler than 
        // the one used in polyphase merge sort - it doesn't
        // take into account distribution over devices
        private IEnumerable<T> Merge(IEnumerable<string> runs)
        {
            var queue = new Queue<string>(runs);
            var runsToMerge = new List<string>(_mergeCount);
            // Until single run is left do merge
            while (queue.Count > 1)
            {
                // Priority queue must not contain records more than 
                // required
                var count = _mergeCount;
                while (queue.Count > 0 && count-- > 0)
                    runsToMerge.Add(queue.Dequeue());
                // Perform n-way merge on selected runs where n is 
                // equal to number of physical devices with 
                // distributed runs but in our case we do not take 
                // into account them and thus n is equal to capacity
                var merged = runsToMerge.Select(Read).OrderedMerge(_comparer);
                queue.Enqueue(Write(merged));

                runsToMerge.Clear();
            }
            // Last run represents source file sorted
            return Read(queue.Dequeue());
        }

        // Distributes unsorted file into several sorted chunks
        // called runs (run is a sequence of records that are 
        // in correct relative order)
        private IEnumerable<string> Distribute(IEnumerable<T> unsorted)
        {
            var source = unsorted; // Read(unsorted);
            using (var enumerator = source.GetEnumerator())
            {
                var curr = new PriorityQueue<T>(_comparer);
                var next = new PriorityQueue<T>(_comparer);
                // Prefill priority queue to capacity which is used 
                // to create runs
                while (curr.Count < _capacity && enumerator.MoveNext())
                    curr.Enqueue(enumerator.Current);
                // Until unsorted source and priority queues are 
                // exhausted
                while (curr.Count > 0)
                {
                    // Create next run and write it to disk
                    var sorted = CreateRun(enumerator, curr, next);
                    var run = Write(sorted);

                    yield return run;

                    Swap(ref curr, ref next);
                }
            }
        }

        private IEnumerable<T> CreateRun(IEnumerator<T> enumerator, PriorityQueue<T> curr, PriorityQueue<T> next)
        {
            while (curr.Count > 0)
            {
                var min = curr.Dequeue();
                yield return min;
                // Trying to move run to an end enumerator will 
                // result in returning false and thus current 
                // queue will simply be emptied step by step
                if (!enumerator.MoveNext())
                    continue;

                // Check if current run can be extended with 
                // next element from unsorted source
                if (_comparer.Compare(enumerator.Current, min) < 0)
                {
                    // As current element is less than min in 
                    // current run it may as well be less than 
                    // elements that are already in the current 
                    // run and thus from this element goes into 
                    // next run
                    next.Enqueue(enumerator.Current);
                }
                else
                {
                    // Extend current run
                    curr.Enqueue(enumerator.Current);
                }
            }
        }

        private static void Swap<U>(ref U a, ref U b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }
    }
}
