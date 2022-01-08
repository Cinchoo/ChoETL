using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoRowsWrittenEventArgs : EventArgs
    {
        public ChoRowsWrittenEventArgs(long rowsWritten, bool isFinal = false)
        {
            RowsWritten = rowsWritten;
            IsFinal = isFinal;
        }

        public bool Abort { get; set; }
        public long RowsWritten { get; }
        public bool IsFinal { get; }
    }
}
