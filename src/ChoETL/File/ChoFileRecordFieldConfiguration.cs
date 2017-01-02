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

        internal readonly List<object> Converters = new List<object>();
        
        public ChoFileRecordFieldConfiguration(string name, ChoFileRecordFieldAttribute attr = null) : base(name, attr)
        {
            FillChar = ' ';
            FieldValueJustification = ChoFieldValueJustification.Left;
            FieldValueTrimOption = ChoFieldValueTrimOption.Trim;
            Truncate = true;
            IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any;

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

        public void AddConverter(IValueConverter converter)
        {
            if (converter == null) return;
            Converters.Add(converter);
        }

        public void AddConverter(TypeConverter converter)
        {
            if (converter == null) return;
            Converters.Add(converter);
        }

        public void RemoveConverter(IValueConverter converter)
        {
            if (converter == null) return;
            if (Converters.Contains(converter))
                Converters.Remove(converter);
        }

        public void RemoveConverter(TypeConverter converter)
        {
            if (converter == null) return;
            if (Converters.Contains(converter))
                Converters.Remove(converter);
        }
    }
}
