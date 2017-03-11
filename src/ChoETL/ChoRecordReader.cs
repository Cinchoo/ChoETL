using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordReader
    {
        public readonly Type RecordType;
        internal TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;

        public ChoRecordReader(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            RecordType = recordType;
        }

        protected bool RaisedRowsLoaded(long rowsLoaded, bool isFinal = false)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
                return false;

            var ea = new ChoRowsLoadedEventArgs(rowsLoaded, isFinal);
            rowsLoadedEvent(this, ea);
            return ea.Abort;
        }

        public abstract IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null);
        public abstract void LoadSchema(object source);
    }
}
