using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoRowsLoadedEventArgs : EventArgs
    {
        public ChoRowsLoadedEventArgs(long rowsLoaded)
        {
            RowsLoaded = rowsLoaded;
        }

        public bool Abort { get; set; }
        public long RowsLoaded { get; }
    }
}
