using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoFileRecordFieldConfiguration : ChoRecordFieldConfiguration
    {
        [DataMember]
        public char? FillChar
        {
            get;
            set;
        }
        [DataMember]
        public ChoFieldValueJustification? FieldValueJustification
        {
            get;
            set;
        }
        [DataMember]
        public ChoFieldValueTrimOption? FieldValueTrimOption
        {
            get;
            set;
        }
        [DataMember]
        public bool Truncate
        {
            get;
            set;
        }
        [DataMember]
        public int? Size
        {
            get;
            set;
        }
        [DataMember]
        public bool? QuoteField
        {
            get;
            set;
        }

        public ChoFileRecordFieldConfiguration(string name, ChoFileRecordFieldAttribute attr = null) : base(name, attr)
        {
            Truncate = true;
            IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any;

            if (attr != null)
            {
                FillChar = attr.FillCharInternal;
                FieldValueJustification = attr.FieldValueJustificationInternal;
                FieldValueTrimOption = attr.FieldValueTrimOptionInternal;
                Truncate = attr.Truncate;
                Size = attr.SizeInternal;
                QuoteField = attr.QuoteFieldInternal;
            }
        }
    }
}
