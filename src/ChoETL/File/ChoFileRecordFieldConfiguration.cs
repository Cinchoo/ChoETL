using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public abstract class ChoFileRecordFieldConfiguration : ChoRecordFieldConfiguration
    {
        public char? FillChar
        {
            get;
            set;
        }
        public ChoFieldValueJustification? FieldValueJustification
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
            FieldValueTrimOption = ChoFieldValueTrimOption.Trim;
            Truncate = true;
            IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any;

            if (attr != null)
            {
                FillChar = attr.FillCharInternal;
                FieldValueJustification = attr.FieldValueJustificationInternal;
                FieldValueTrimOption = attr.FieldValueTrimOption;
                Truncate = attr.Truncate;
                Size = attr.SizeInternal;
                QuoteField = attr.QuoteFieldInternal;
            }
        }
    }
}
