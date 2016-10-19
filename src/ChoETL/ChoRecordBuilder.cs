using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordBuilder
    {
        public readonly Type RecordType;

        public ChoRecordBuilder(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            RecordType = recordType;
        }

        public abstract IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null);
    }
}
