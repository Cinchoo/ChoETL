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
        public int Order
        {
            get;
            set;
        }
        public string FieldName
        {
            get;
            set;
        }
        internal char? FillCharInternal;
        public char FillChar
        {
            get { return FillCharInternal.CastTo<char>(); }
            set { FillCharInternal = value; }
        }
        internal ChoFieldValueJustification? FieldValueJustificationInternal;
        public ChoFieldValueJustification FieldValueJustification
        {
            get { return FieldValueJustificationInternal.CastTo<ChoFieldValueJustification>(); }
            set { FieldValueJustificationInternal = value; }
        }
        internal ChoFieldValueTrimOption? FieldValueTrimOptionInternal;
        public ChoFieldValueTrimOption FieldValueTrimOption
        {
            get { return FieldValueTrimOptionInternal.CastTo<ChoFieldValueTrimOption>(); }
            set { FieldValueTrimOptionInternal = value; }
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
        public string NullValue
        {
            get;
            set;
        }

        public ChoFileRecordFieldAttribute()
        {
            Truncate = true;
            IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any;
        }
    }
}
