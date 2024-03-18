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
        public Type ReflectedType
        {
            get;
            set;
        }
        public Func<object, bool> Validator
        {
            get;
            set;
        }
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
            get { return _fieldType; }
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
        public Func<object, object> ValueConverterBack
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
        public string FieldTypeDiscriminator
        {
            get;
            set;
        }
        public string ItemTypeDiscriminator
        {
            get;
            set;
        }
        [IgnoreDataMember]
        protected bool IsDefaultValueSpecified
        {
            get;
            set;
        }
        internal bool IsDefaultValueSpecifiedInternal
        {
            get { return IsDefaultValueSpecified; }
            set { IsDefaultValueSpecified = value; }
        }
        private object _defaultValue;
        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                _defaultValue = value;
                IsDefaultValueSpecifiedInternal = _defaultValue != null;
            }
        }

        internal bool IsFallbackValueSpecifiedInternal
        {
            get { return IsFallbackValueSpecified; }
            set { IsFallbackValueSpecified = value; }
        }
        [IgnoreDataMember]
        protected bool IsFallbackValueSpecified
        {
            get;
            set;
        }

        private object _fallbackValue;
        public object FallbackValue
        {
            get { return _fallbackValue; }
            set
            {
                _fallbackValue = value;
                IsFallbackValueSpecifiedInternal = _fallbackValue != null;
            }
        }

        protected string DeclaringMember
        {
            get;
            set;
        }
        internal string DeclaringMemberInternal
        {
            get => DeclaringMember;
            set => DeclaringMember = value;
        }
        protected PropertyDescriptor PropertyDescriptor
        {
            get;
            set;
        }
        internal PropertyDescriptor PropertyDescriptorInternal
        {
            get => PropertyDescriptor;
            set => PropertyDescriptor = value;
        }

        internal readonly List<object> Converters = new List<object>();
        internal readonly List<object> ItemConverters = new List<object>();
        internal readonly List<object> KeyConverters = new List<object>();
        internal readonly List<object> ValueConverters = new List<object>();
        protected PropertyInfo PI { get; set; }
        protected PropertyDescriptor PD { get; set; }
        internal PropertyInfo PIInternal
        {
            get => PI;
            set => PI = value;
        }
        internal PropertyDescriptor PDInternal
        {
            get => PD;
            set => PD = value;
        }
        protected object[] PropConverters
        {
            get;
            set;
        }
        internal object[] PropConvertersInternal
        {
            get => PropConverters;
            set => PropConverters = value;
        }
        protected object[] PropConverterParams
        {
            get;
            set;
        }
        internal object[] PropConverterParamsInternal
        {
            get => PropConverterParams;
            set => PropConverterParams = value;
        }
        public object PropCustomSerializer { get; set; }
        public object PropCustomSerializerParams { get; set; }

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

        protected object[] GetConverters()
        {
            if (PropConvertersInternal.IsNullOrEmpty())
                return PropConvertersInternal;
            else if (Converters != null)
                return Converters.ToArray();
            else
                return null;
        }

        internal bool HasConvertersInternal()
        {
            return HasConverters();
        }
        protected bool HasConverters()
        {
            return (Converters != null && Converters.Count > 0)
                || (PropConvertersInternal != null && PropConvertersInternal.Length > 0)
                || ValueConverter != null;
        }
        internal Type GetSourceTypeFromConvertersIfAny()
        {
            Type srcType = null;
            if (PropConvertersInternal != null)
            {
                foreach (var c in PropConvertersInternal.Where(c1 => c1 != null))
                {
                    var attr = ChoType.GetCustomAttribute<ChoSourceTypeAttribute>(c.GetType(), true);
                    if (attr != null)
                    {
                        srcType = attr.Type;
                        if (srcType != null)
                            return srcType;
                    }
                }
            }

            if (Converters != null)
            {
                foreach (var c in Converters.Where(c1 => c1 != null))
                {
                    var attr = c.GetType().GetCustomAttribute(typeof(ChoSourceTypeAttribute)) as ChoSourceTypeAttribute;
                    if (attr != null)
                    {
                        srcType = attr.Type;
                        if (srcType != null)
                            return srcType;
                    }
                }
            }

            return null;
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
