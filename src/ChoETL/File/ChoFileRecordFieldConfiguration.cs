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
        public bool Optional
        {
            get;
            set;
        }
        public int Order
        {
            get;
            set;
        }
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
        public int? ArrayIndex
        {
            get;
            set;
        }
        public string DictKey
        {
            get;
            set;
        }
        public ChoFileRecordFieldConfiguration(string name, ChoFileRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            Truncate = true;
            //IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any;

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
                DisplayFormatAttribute dfAttr = otherAttrs.OfType<DisplayFormatAttribute>().FirstOrDefault();
                if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                {
                    FormatText = dfAttr.DataFormatString;
                }
                if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                {
                    NullValue = dfAttr.NullDisplayText;
                }
                else
                    NullValue = attr.NullValue;

                QuoteField = attr.QuoteFieldInternal;
            }
        }

        public ChoFieldValueTrimOption GetFieldValueTrimOptionForRead(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            ChoFieldValueTrimOption? fieldValueTrimOption = FieldValueTrimOption;

            if (fieldValueTrimOption != null)
                return fieldValueTrimOption.Value;
            else
                return recordLevelFieldValueTrimOption != null ? recordLevelFieldValueTrimOption.Value : ChoFieldValueTrimOption.Trim;
        }

        public ChoFieldValueTrimOption GetFieldValueTrimOption(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            ChoFieldValueTrimOption? fieldValueTrimOption = FieldValueTrimOption;

            if (fieldValueTrimOption != null)
                return fieldValueTrimOption.Value;

            if (recordLevelFieldValueTrimOption != null)
                return recordLevelFieldValueTrimOption.Value;

            if (fieldType == typeof(int)
              || fieldType == typeof(uint)
              || fieldType == typeof(long)
              || fieldType == typeof(ulong)
              || fieldType == typeof(short)
              || fieldType == typeof(ushort)
              || fieldType == typeof(byte)
              || fieldType == typeof(sbyte)
              || fieldType == typeof(float)
              || fieldType == typeof(double)
              || fieldType == typeof(decimal)
              || fieldType == typeof(Single)
              )
            {
                return ChoFieldValueTrimOption.TrimStart;
            }
            else
                return ChoFieldValueTrimOption.TrimEnd;
        }

    }
}
