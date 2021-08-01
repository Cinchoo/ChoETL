using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [DataContract]
    public abstract class ChoRecordFieldConfiguration
    {
        public Type SourceType
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            internal set;
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
        private Type _fieldType;
        [DataMember]
        public Type FieldType
        {
            get { return SourceType == null ? _fieldType : SourceType; }
            set { _fieldType = value; }
        }
        [DataMember]
        public bool? IsNullable
        {
            get;
            set;
        }
        [DataMember]
        public string FormatText
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
        public Func<dynamic, object> ValueSelector
        {
            get;
            set;
        }
        public Func<string> HeaderSelector
        {
            get;
            set;
        }
        public Func<object, object> CustomSerializer
        {
            get;
            set;
        }
        public Func<object, object> ItemConverter
        {
            get;
            set;
        }
        public Func<object, Type> ItemRecordTypeSelector
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public bool IsDefaultValueSpecified
        {
            get;
            internal set;
        }

        private object _defaultValue;
        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                _defaultValue = value;
                IsDefaultValueSpecified = _defaultValue != null;
            }
        }

        [IgnoreDataMember]
        public bool IsFallbackValueSpecified
        {
            get;
            internal set;
        }

        private object _fallbackValue;
        public object FallbackValue
        {
            get { return _fallbackValue; }
            set
            {
                _fallbackValue = value;
                IsFallbackValueSpecified = _fallbackValue != null;
            }
        }

        public string DeclaringMember
        {
            get;
            set;
        }
        internal PropertyDescriptor PropertyDescriptor
        {
            get;
            set;
        }

        internal readonly List<object> Converters = new List<object>();
        internal readonly List<object> ItemConverters = new List<object>();
        internal readonly List<object> KeyConverters = new List<object>();
        internal readonly List<object> ValueConverters = new List<object>();
        public PropertyInfo PI { get; set; }
        public PropertyDescriptor PD { get; set; }
        public object[] PropConverters;
        public object[] PropConverterParams;
        public object PropCustomSerializer;
        public object PropCustomSerializerParams;

        public ChoRecordFieldConfiguration(string name, ChoRecordFieldAttribute attr = null, Attribute[] otherAttrs = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            Name = name.NTrim();
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                Initialize();

            //FieldType = typeof(string);

            if (attr != null)
            {
                ErrorMode = attr.ErrorModeInternal;
                IgnoreFieldValueMode = attr.IgnoreFieldValueModeInternal;
                FieldType = attr.FieldType;
                IsNullable = attr.IsNullableInternal;
                FormatText = attr.FormatText;
            }
        }

        private void Initialize()
        {
            Name = Name.NTrim().FixName();
        }

        public object[] GetConverters()
        {
            if (PropConverters.IsNullOrEmpty())
                return PropConverters;
            else if (Converters != null)
                return Converters.ToArray();
            else
                return null;
        }

        public bool HasConverters()
        {
            return (Converters != null && Converters.Count > 0)
                || (PropConverters != null && PropConverters.Length > 0)
                || ValueConverter != null;
        }

#if !NETSTANDARD2_0
        public void AddConverter(IValueConverter converter)
        {
            if (converter == null) return;
            Converters.Add(converter);
        }
#endif
        public void AddConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            Converters.Add(converter);
        }

        public void AddConverter(TypeConverter converter)
        {
            if (converter == null) return;
            Converters.Add(converter);
        }

        public void RemoveConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            if (Converters.Contains(converter))
                Converters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void RemoveConverter(IValueConverter converter)
        {
            if (converter == null) return;
            if (Converters.Contains(converter))
                Converters.Remove(converter);
        }
#endif
        public void RemoveConverter(TypeConverter converter)
        {
            if (converter == null) return;
            if (Converters.Contains(converter))
                Converters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void AddItemConverter(IValueConverter converter)
        {
            if (converter == null) return;
            ItemConverters.Add(converter);
        }
#endif
        public void AddItemConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            ItemConverters.Add(converter);
        }

        public void AddItemConverter(TypeConverter converter)
        {
            if (converter == null) return;
            ItemConverters.Add(converter);
        }

        public void RemoveItemConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            if (ItemConverters.Contains(converter))
                ItemConverters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void RemoveItemConverter(IValueConverter converter)
        {
            if (converter == null) return;
            if (ItemConverters.Contains(converter))
                ItemConverters.Remove(converter);
        }
#endif
        public void RemoveItemConverter(TypeConverter converter)
        {
            if (converter == null) return;
            if (ItemConverters.Contains(converter))
                ItemConverters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void AddKeyConverter(IValueConverter converter)
        {
            if (converter == null) return;
            KeyConverters.Add(converter);
        }
#endif
        public void AddKeyConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            KeyConverters.Add(converter);
        }

        public void AddKeyConverter(TypeConverter converter)
        {
            if (converter == null) return;
            KeyConverters.Add(converter);
        }

        public void RemoveKeyConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            if (KeyConverters.Contains(converter))
                KeyConverters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void RemoveKeyConverter(IValueConverter converter)
        {
            if (converter == null) return;
            if (KeyConverters.Contains(converter))
                KeyConverters.Remove(converter);
        }
#endif
        public void RemoveKeyConverter(TypeConverter converter)
        {
            if (converter == null) return;
            if (KeyConverters.Contains(converter))
                KeyConverters.Remove(converter);
        }

    #if !NETSTANDARD2_0
        public void AddValueConverter(IValueConverter converter)
        {
            if (converter == null) return;
            ValueConverters.Add(converter);
        }
#endif
        public void AddValueConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            ValueConverters.Add(converter);
        }

        public void AddValueConverter(TypeConverter converter)
        {
            if (converter == null) return;
            ValueConverters.Add(converter);
        }

        public void RemoveValueConverter(IChoValueConverter converter)
        {
            if (converter == null) return;
            if (ValueConverters.Contains(converter))
                ValueConverters.Remove(converter);
        }

#if !NETSTANDARD2_0
        public void RemoveValueConverter(IValueConverter converter)
        {
            if (converter == null) return;
            if (ValueConverters.Contains(converter))
                ValueConverters.Remove(converter);
        }
#endif
        public void RemoveValueConverter(TypeConverter converter)
        {
            if (converter == null) return;
            if (ValueConverters.Contains(converter))
                ValueConverters.Remove(converter);
        }
    }
}
