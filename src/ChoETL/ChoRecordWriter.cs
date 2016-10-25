using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordWriter
    {
        public readonly Type RecordType;

        public ChoRecordWriter(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            RecordType = recordType;
        }

        public abstract IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null);
    }
}
