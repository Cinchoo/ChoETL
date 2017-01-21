using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public abstract class ChoRecordFieldConfiguration
    {
        public string Name
        {
            get;
            private set;
        }
        public ChoErrorMode? ErrorMode
        {
            get;
            set;
        }
        public ChoIgnoreFieldValueMode? IgnoreFieldValueMode
        {
            get;
            set;
        }

        public Type FieldType
        {
            get;
            set;
        }

        public ValidationAttribute[] Validators
        {
            get;
            set;
        }

        private object _defaultValue;
        internal bool IsDefaultValueSpecified;
        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                _defaultValue = value;
                IsDefaultValueSpecified = true;
            }
        }

        private object _fallbackValue;
        internal bool IsFallbackValueSpecified;
        public object FallbackValue
        {
            get { return _fallbackValue; }
            set
            {
                _fallbackValue = value;
                IsFallbackValueSpecified = true;
            }
        }

        internal readonly List<object> Converters = new List<object>();

        public ChoRecordFieldConfiguration(string name, ChoRecordFieldAttribute attr = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            Name = name;
            FieldType = typeof(string);

            if (attr != null)
            {
                ErrorMode = attr.ErrorModeInternal;
                IgnoreFieldValueMode = attr.IgnoreFieldValueModeInternal;
                FieldType = attr.FieldType;
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
