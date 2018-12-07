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
        public bool IgnoreCase
        {
            get;
            set;
        }
        public bool IgnoreHeader
        {
            get;
            set;
        }
        public long HeaderLineAt
        {
            get;
            set;
        }
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

        public bool IgnoreColumnsWithEmptyHeader
        {
            get;
            set;
        }

        internal bool? QuoteAllInternal;
        public bool QuoteAll
        {
            get { throw new NotSupportedException(); }
            set { QuoteAllInternal = value; }
        }

        public ChoFileHeaderAttribute()
        {
            IgnoreCase = true;
            IgnoreHeader = false;
        }
    }

}
