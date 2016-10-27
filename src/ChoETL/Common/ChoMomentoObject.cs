using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoMomentoObject<T> : IDisposable
    {
        private T _state;
        private Action<T> _revert;

        public ChoMomentoObject(T state, Func<T> capture, Action<T> revert)
        {
            ChoGuard.ArgumentNotNull(state, "State");
            ChoGuard.ArgumentNotNull(capture, "Capture");
            ChoGuard.ArgumentNotNull(revert, "Revert");

            _state = capture();
            revert(state);
            _revert = revert;
        }

        protected void Dispose(bool finalize)
        {
            _revert(_state);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        ~ChoMomentoObject()
        {
            Dispose(true);
        }
    }
}
