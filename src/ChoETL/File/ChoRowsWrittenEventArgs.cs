using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoRowsWrittenEventArgs : EventArgs
    {
        public ChoRowsWrittenEventArgs(long rowsWritten)
        {
            RowsWritten = rowsWritten;
        }

        public bool Abort { get; set; }
        public long RowsWritten { get; }
    }
}
