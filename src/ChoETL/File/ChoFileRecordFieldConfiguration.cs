using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
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
        public new Type FieldType
        {
            get { return base.FieldType; }
            set
            {
                if (base.FieldType != value)
                {
                    base.FieldType = value;
                    LoadKnownTypes(value);
                }
            }
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
        public List<int> NestedArrayIndex
        {
            get;
            set;
        } = new List<int>();
        private Dictionary<string, Type> _knownTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, Type> KnownTypes
        {
            get { return _knownTypes; }
            set { _knownTypes = value; }
        }

        public string KnownTypeDiscriminator
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
            if (otherAttrs != null)
            {
                var sa = otherAttrs.OfType<ChoSourceTypeAttribute>().FirstOrDefault();
                if (sa != null)
                    SourceType = sa.Type;

                StringLengthAttribute slAttr = otherAttrs.OfType<StringLengthAttribute>().FirstOrDefault();
                if (slAttr != null && slAttr.MaximumLength > 0)
                    Size = slAttr.MaximumLength;
                DisplayNameAttribute dnAttr = otherAttrs.OfType<DisplayNameAttribute>().FirstOrDefault();
                if (dnAttr != null && !dnAttr.DisplayName.IsNullOrWhiteSpace())
                {
                    FieldName = dnAttr.DisplayName.Trim();
                }
                else
                {
                    DisplayAttribute dpAttr = otherAttrs.OfType<DisplayAttribute>().FirstOrDefault();
                    if (dpAttr != null)
                    {
                        if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                            FieldName = dpAttr.ShortName;
                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                            FieldName = dpAttr.Name;

                        Order = dpAttr.Order;
                    }
                    else
                    {
                        ColumnAttribute clAttr = otherAttrs.OfType<ColumnAttribute>().FirstOrDefault();
                        if (clAttr != null)
                        {
                            Order = clAttr.Order;
                            if (!clAttr.Name.IsNullOrWhiteSpace())
                                FieldName = clAttr.Name;
                        }
                    }
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
            }
        }

        private bool _knownTypeInitialized = false;
        protected virtual void LoadKnownTypes(Type recordType)
        {
            if (_knownTypeInitialized)
                return;

            //_knownTypeInitialized = true;
            if (recordType == null)
                return;

            recordType = recordType.GetItemType();

            if (_knownTypes == null)
                _knownTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

            _knownTypes = ChoTypeDescriptor.GetTypeAttributes<ChoKnownTypeAttribute>(recordType).Where(a => a.Type != null && !a.Value.IsNullOrWhiteSpace())
                .GroupBy(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.First().Type, _knownTypes.Comparer == null ? StringComparer.InvariantCultureIgnoreCase : _knownTypes.Comparer);

            var kta = ChoTypeDescriptor.GetTypeAttribute<ChoKnownTypeDiscriminatorAttribute>(recordType);
            if (kta != null && !kta.Discriminator.IsNullOrWhiteSpace())
                KnownTypeDiscriminator = kta.Discriminator.Trim();
        }

        internal ChoFieldValueTrimOption GetFieldValueTrimOptionForReadInternal(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            return GetFieldValueTrimOptionForRead(fieldType, recordLevelFieldValueTrimOption);
        }
        protected ChoFieldValueTrimOption GetFieldValueTrimOptionForRead(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            ChoFieldValueTrimOption? fieldValueTrimOption = FieldValueTrimOption;

            if (fieldValueTrimOption != null)
                return fieldValueTrimOption.Value;
            else
                return recordLevelFieldValueTrimOption != null ? recordLevelFieldValueTrimOption.Value : ChoFieldValueTrimOption.Trim;
        }

        protected ChoFieldValueTrimOption GetFieldValueTrimOption(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
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

        internal ChoFieldValueTrimOption GetFieldValueTrimOptionInternal(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            return GetFieldValueTrimOption(fieldType, recordLevelFieldValueTrimOption);
        }
    }
}
