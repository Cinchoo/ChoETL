using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoKVPRecordObjectAttribute : ChoFileRecordObjectAttribute
    {
        public string Separator
        {
            get;
            private set;
        }
        public string RecordStart
        {
            get;
            set;
        }
        public string RecordEnd
        {
            get;
            set;
        }
        public char[] LineContinuationChars
        {
            get;
            set;
        }

        public ChoKVPRecordObjectAttribute(string delimiter = null)
        {
            Separator = delimiter;
            LineContinuationChars = new char[] { ' ', '\t' };
        }
    }
}