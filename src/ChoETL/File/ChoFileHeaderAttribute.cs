using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public abstract class ChoFileHeaderAttribute : Attribute
    {
        public bool IgnoreCase => true;

        internal char? FillCharInternal;
        public char FillChar
        {
            get { throw new NotSupportedException(); }
            set { FillCharInternal = value; }
        }

        internal ChoFieldValueJustification? JustificationInternal;
        public ChoFieldValueJustification Justification
        {
            get { throw new NotSupportedException(); }
            set { JustificationInternal = value; }
        }

        internal ChoFieldValueTrimOption? TrimOptionInternal;
        public ChoFieldValueTrimOption TrimOption
        {
            get { throw new NotSupportedException(); }
            set { TrimOptionInternal = value; }
        }

        internal bool? TruncateInternal;
        public bool Truncate
        {
            get { throw new NotSupportedException(); }
            set { TruncateInternal = value; }
        }

        public ChoFileHeaderAttribute()
        {
        }
    }

}
