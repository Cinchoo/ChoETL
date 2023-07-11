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
            get { return _ignoreFieldValueModeInternal == null ? ChoIgnoreFieldValueMode.None : _ignoreFieldValueModeInternal.Value; }
            set { _ignoreFieldValueModeInternal = value; }
        }
        private ChoIgnoreFieldValueMode? _ignoreFieldValueModeInternal = null;
        internal ChoIgnoreFieldValueMode? IgnoreFieldValueModeInternal
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