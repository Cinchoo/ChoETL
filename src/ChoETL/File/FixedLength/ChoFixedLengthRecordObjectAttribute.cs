using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoFixedLengthRecordObjectAttribute : ChoFileRecordObjectAttribute
    {
        public int RecordLength
        {
            get;
            private set;
        }

        public ChoFixedLengthRecordObjectAttribute(int recordLength = 0)
        {
            RecordLength = recordLength;
        }
    }
}