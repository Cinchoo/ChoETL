using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordConfiguration
    {
        public ChoErrorMode ErrorMode
        {
            get;
            set;
        }
        public ChoIgnoreFieldValueMode IgnoreFieldValueMode
        {
            get;
            set;
        }

        public ChoRecordConfiguration(Type recordType = null)
        {
            ErrorMode = ChoErrorMode.ThrowAndStop;
        }
    }
}
