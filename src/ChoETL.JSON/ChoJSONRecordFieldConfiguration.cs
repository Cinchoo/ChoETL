using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace ChoETL
{
    [DataContract]
    public class ChoJSONRecordFieldConfiguration : ChoFileRecordFieldConfiguration, IChoJSONRecordFieldConfiguration
    {
        public static new bool? QuoteField
        {
            get;
            set;
        }
        internal PropertyDescriptor PropertyDescriptorInternal
        {
            get => PropertyDescriptor;
            set => PropertyDescriptor = value;
        }
        internal object[] PropConverterParamsInternal
        {
            get => PropConverterParams;
            set => PropConverterParams = value;
        }
        internal object[] PropConvertersInternal
        {
            get => PropConverters;
            set => PropConverters = value;
        }
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
        internal string DeclaringMemberInternal
        {
            get => DeclaringMember;
            set => DeclaringMember = value;
        }
        [DataMember]
        public string JSONPath
        {
            get;
            set;
        }

        public bool? IsArray
        {
            get;
            set;
        }
        internal bool ComplexJPathUsed
        {
            get;
            set;
        }

        public bool? UseJSONSerialization
        {
            get;
            set;
        }
        private Func<object, Type> _fieldTypeSelector = null;
        public Func<object, Type> FieldTypeSelector
        {
            get { return _fieldTypeSelector; }
            set { if (value == null) return; _fieldTypeSelector = value; }
        }
        public IContractResolver ContractResolver { get; set; }
        public NullValueHandling? NullValueHandling { get; set; }
        PropertyDescriptor IChoJSONRecordFieldConfiguration.PD { get => PDInternal; set => PDInternal = value; }
        string IChoJSONRecordFieldConfiguration.DeclaringMember { get => DeclaringMember; set => DeclaringMember = value; }

        public ChoJSONRecordFieldConfiguration(string name, string jsonPath = null) : this(name, (ChoJSONRecordFieldAttribute)null)
        {
            JSONPath = jsonPath;
        }

        internal ChoJSONRecordFieldConfiguration(string name, ChoJSONRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            //IsArray = false;
            FieldName = name;
            if (attr != null)
            {
                Order = attr.Order;
                JSONPath = attr.JSONPath;
                UseJSONSerialization = attr.UseJSONSerializationInternal;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name.NTrim() : attr.FieldName.NTrim();
            }
            if (otherAttrs != null)
            {
                var sa = otherAttrs.OfType<ChoSourceTypeAttribute>().FirstOrDefault();
                if (sa != null)
                    SourceType = sa.Type;

                StringLengthAttribute slAttr = otherAttrs.OfType<StringLengthAttribute>().FirstOrDefault();
                if (slAttr != null && slAttr.MaximumLength > 0)
                    Size = slAttr.MaximumLength;
                ChoUseJSONSerializationAttribute sAttr = otherAttrs.OfType<ChoUseJSONSerializationAttribute>().FirstOrDefault();
                if (sAttr != null)
                    UseJSONSerialization = sAttr.Flag;
                ChoJSONPathAttribute jpAttr = otherAttrs.OfType<ChoJSONPathAttribute>().FirstOrDefault();
                if (jpAttr != null)
                    JSONPath = jpAttr.JSONPath;

                JsonPropertyAttribute jAttr = otherAttrs.OfType<JsonPropertyAttribute>().FirstOrDefault();
                if (jAttr != null && !jAttr.PropertyName.IsNullOrWhiteSpace())
                {
                    FieldName = jAttr.PropertyName;
                    JSONPath = jAttr.PropertyName;
                    Order = jAttr.Order;
                }
                else
                {
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

        internal JsonConverter GetJsonConverterIfAny()
        {
            if (!KnownTypeDiscriminator.IsNullOrWhiteSpace() && KnownTypes.Count > 0)
            {
                return new ChoKnownTypeConverter(FieldType.GetItemType(), KnownTypeDiscriminator, KnownTypes, FieldTypeSelector);
            }

            return null;
        }

        internal void Validate(ChoJSONRecordConfiguration config)
        {
            try
            {
                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;

                //if (JSONPath.IsNullOrWhiteSpace())
                //    throw new ChoRecordConfigurationException("Missing XPath.");
                if (FillChar != null)
                {
                    if (FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                }

                if (Size != null && Size.Value <= 0)
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (ErrorMode == null)
                    ErrorMode = config.ErrorMode; // config.ErrorMode;
                if (IgnoreFieldValueMode == null)
                    IgnoreFieldValueMode = config.IgnoreFieldValueMode;
                //if (QuoteField == null)
                //    QuoteField = config.QuoteAllFields;
            }
            catch (Exception ex)
            {
                throw new ChoRecordConfigurationException("Invalid configuration found at '{0}' field.".FormatString(Name), ex);
            }
        }

        internal bool IgnoreFieldValue(object fieldValue)
        {
            if (IgnoreFieldValueMode == null)
                return false; // fieldValue == null;

            if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null && fieldValue == null)
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.DBNull) == ChoIgnoreFieldValueMode.DBNull && fieldValue == DBNull.Value)
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Empty) == ChoIgnoreFieldValueMode.Empty && fieldValue is string && ((string)fieldValue).IsEmpty())
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.WhiteSpace) == ChoIgnoreFieldValueMode.WhiteSpace && fieldValue is string && ((string)fieldValue).IsNullOrWhiteSpace())
                return true;

            return false;
        }

        object[] IChoJSONRecordFieldConfiguration.GetConverters()
        {
            return GetConverters();
        }
        internal ChoFieldValueTrimOption GetFieldValueTrimOptionInternal(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            return GetFieldValueTrimOption(fieldType, recordLevelFieldValueTrimOption);
        }
        internal ChoFieldValueTrimOption GetFieldValueTrimOptionForReadInternal(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            return GetFieldValueTrimOptionForRead(fieldType, recordLevelFieldValueTrimOption);
        }
        internal bool HasConvertersInternal()
        {
            return HasConverters();
        }
        internal bool IsDefaultValueSpecifiedInternal
        {
            get { return IsDefaultValueSpecified; }
            set { IsDefaultValueSpecified = value; }
        }
        internal bool IsFallbackValueSpecifiedInternal
        {
            get { return IsFallbackValueSpecified; }
            set { IsFallbackValueSpecified = value; }
        }
        object[] IChoJSONRecordFieldConfiguration.PropConverters { get => PropConverters; set => PropConverters = value; }
        object[] IChoJSONRecordFieldConfiguration.PropConverterParams { get => PropConverterParams; set => PropConverterParams = value; }
    }
}
