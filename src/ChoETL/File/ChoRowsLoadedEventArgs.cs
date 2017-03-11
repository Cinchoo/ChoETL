using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoRowsLoadedEventArgs : EventArgs
    {
        public ChoRowsLoadedEventArgs(long rowsLoaded, bool isFinal = false)
        {
            RowsLoaded = rowsLoaded;
            IsFinal = isFinal;
        }

        public bool Abort { get; set; }
        public long RowsLoaded { get; }
        public bool IsFinal { get; }
    }
}
