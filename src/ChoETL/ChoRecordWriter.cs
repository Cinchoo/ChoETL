using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordWriter
    {
        public readonly Type RecordType;
        public TraceSwitch TraceSwitch;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        static ChoRecordWriter()
        {
            ChoETLFramework.Initialize();
        }

        public ChoRecordWriter(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            if (typeof(ICollection).IsAssignableFrom(recordType))
                throw new ChoReaderException("Invalid recordtype passed.");

            RecordType = recordType;
            TraceSwitch = ChoETLFramework.TraceSwitch;
        }

        protected bool RaisedRowsWritten(long rowsWritten)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                return false;

            var ea = new ChoRowsWrittenEventArgs(rowsWritten);
            rowsWrittenEvent(this, ea);
            return ea.Abort;
        }

        public abstract IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null);

		protected object GetDeclaringRecord(string declaringMember, object rec)
		{
			return ChoType.GetDeclaringRecord(declaringMember, rec);
		}
	}
}
