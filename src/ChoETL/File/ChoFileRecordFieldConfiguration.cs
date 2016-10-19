using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoFileRecordFieldConfiguration : ChoRecordFieldConfiguration
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
        public int? Size
        {
            get;
            set;
        }
        public bool? QuoteField
        {
            get;
            set;
        }

        public ChoFileRecordFieldConfiguration(string name, ChoFileRecordFieldAttribute attr = null) : base(name, attr)
        {
            if (attr != null)
            {
                FillChar = attr.FillChar;
                FieldValueJustification = attr.FieldValueJustification;
                FieldValueTrimOption = attr.FieldValueTrimOption;
                Truncate = attr.Truncate;
                Size = attr.SizeInternal;
                QuoteField = attr.QuoteFieldInternal;
            }
        }
    }
}
