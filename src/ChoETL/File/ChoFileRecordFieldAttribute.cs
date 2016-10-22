using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public abstract class ChoFileRecordFieldAttribute : ChoRecordFieldAttribute
    {
        public char FillChar
        {
            get;
            set;
        }
        public ChoFieldValueJustification FieldValueJustification
        {
            get;
            set;
        }
        public ChoFieldValueTrimOption FieldValueTrimOption
        {
            get;
            set;
        }
        public bool Truncate
        {
            get;
            set;
        }
        internal int? SizeInternal;
        public int Size
        {
            get { return SizeInternal.CastTo<int>(0); }
            set { SizeInternal = value; }
        }
        internal bool? QuoteFieldInternal;
        public bool QuoteField
        {
            get { return QuoteFieldInternal.CastTo<bool>(false); }
            set { QuoteFieldInternal = value; }
        }

        public ChoFileRecordFieldAttribute()
        {
            FillChar = ' ';
            FieldValueJustification = ChoFieldValueJustification.Left;
            FieldValueTrimOption = ChoFieldValueTrimOption.Trim;
            Truncate = true;
            IgnoreFieldValueMode = ChoIgnoreFieldValueMode.All;
        }
    }
}
