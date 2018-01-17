using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoRecordFieldConfiguration
    {
        [DataMember]
        public string Name
        {
            get;
            private set;
        }
        [DataMember]
        public ChoErrorMode? ErrorMode
        {
            get;
            set;
        }
        [DataMember]
        public ChoIgnoreFieldValueMode? IgnoreFieldValueMode
        {
            get;
            set;
        }
        [DataMember]
        public string FieldTypeName
        {
            get { return FieldType != null ? FieldType.FullName : null; }
            set { FieldType = value != null ? Type.GetType(value) : null; }
        }

        public Type FieldType
        {
            get;
            set;
        }

        public bool IsNullable
        {
            get;
            set;
        }

        public ValidationAttribute[] Validators
        {
            get;
            set;
        }
        public Func<object, object> ValueConverter
        {
            get;
            set;
        }
        public Func<object, object> ItemConverter
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool IsDefaultValueSpecified;

        private object _defaultValue;
        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                _defaultValue = value;
                IsDefaultValueSpecified = true;
            }
        }

        [IgnoreDataMember]
        public bool IsFallbackValueSpecified;

        private object _fallbackValue;
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
        internal PropertyInfo PI;
        internal PropertyDescriptor PD;
        public object[] PropConverters;
        public object[] PropConverterParams;

        public ChoRecordFieldConfiguration(string name, ChoRecordFieldAttribute attr = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            Name = Name.NTrim();
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                Initialize();

            //FieldType = typeof(string);

            if (attr != null)
            {
                ErrorMode = attr.ErrorModeInternal;
                IgnoreFieldValueMode = attr.IgnoreFieldValueModeInternal;
                FieldType = attr.FieldType;
            }
        }

        private void Initialize()
        {
            Name = Name.NTrim().ToValidVariableName();
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
