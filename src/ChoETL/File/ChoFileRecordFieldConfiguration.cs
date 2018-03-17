using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoFileRecordFieldConfiguration : ChoRecordFieldConfiguration
    {
        [DataMember]
        public string FieldName
        {
            get;
            set;
        }
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
        [DataMember]
        public string NullValue
        {
            get;
            set;
        }

        public ChoFileRecordFieldConfiguration(string name, ChoFileRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
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

                if (Size == null && otherAttrs != null)
                {
                    StringLengthAttribute slAttr = otherAttrs.OfType<StringLengthAttribute>().FirstOrDefault();
                    if (slAttr != null && slAttr.MaximumLength > 0)
                    {
                        Size = slAttr.MaximumLength;
                    }
                }
                DisplayAttribute dpAttr = otherAttrs.OfType<DisplayAttribute>().FirstOrDefault();
                if (dpAttr != null)
                {
                    if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                        FieldName = dpAttr.ShortName;
                    else if (!dpAttr.Name.IsNullOrWhiteSpace())
                        FieldName = dpAttr.Name;
                }

                QuoteField = attr.QuoteFieldInternal;
                NullValue = attr.NullValue;
            }
        }
    }
}
