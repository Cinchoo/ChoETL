using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoQueuedTaskScheduler
    {
        private readonly Queue<Action> _queue = new Queue<Action>();

        public void Enqueue(Action action)
        {
            if (action == null) return;

            _queue.Enqueue(action);
        }

        public void Run()
        {
            while (_queue.Count > 0)
                _queue.Dequeue()();
        }
    }
}
