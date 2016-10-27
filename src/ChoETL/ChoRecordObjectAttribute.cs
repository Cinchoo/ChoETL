using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordObjectAttribute : ChoObjectAttribute
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
        public bool ThrowAndStopOnMissingField
        {
            get;
            set;
        }

        public ChoRecordObjectAttribute()
        {
            ErrorMode = ChoErrorMode.ThrowAndStop;
            ThrowAndStopOnMissingField = true;
        }
    }
}