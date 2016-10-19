using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoMapReduce
    {
        public static Task<TResult> ForEachParallel<TInput, TResult>(this IEnumerable<TInput> items,
            Func<TInput, TResult> map,
            Func<IEnumerable<TResult>, TResult> reduce, int maxNoOfThreads = Int32.MaxValue)
        {
            if (items == null) { throw new ArgumentNullException("items"); }
            if (map == null) { throw new ArgumentNullException("map"); }
            if (reduce == null) { throw new ArgumentNullException("reduce"); }

            return Task<TResult>.Factory.StartNew(() =>
            {
                List<Task<TResult>> tasks = new List<Task<TResult>>();

                SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxNoOfThreads);
                foreach (TInput item in items)
                {
                    Task<TResult> t = Task<TResult>.Factory.StartNew(item2 =>
                    {
                        concurrencySemaphore.Wait();
                        return map((TInput)item2);
                    },
                        item,
                        TaskCreationOptions.None | TaskCreationOptions.AttachedToParent);

                    t.ContinueWith((t1) => concurrencySemaphore.Release());
                    tasks.Add(t);
                }

                List<TResult> results = new List<TResult>();
                foreach (Task<TResult> task in tasks)
                {
                    results.Add(task.Result);
                }

                return reduce(results.ToArray());
            });
        }

    }
}
