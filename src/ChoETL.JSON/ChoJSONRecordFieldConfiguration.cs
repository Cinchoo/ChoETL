using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ChoJSONRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
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
        internal PropertyDescriptor PropertyDescriptor
        {
            get;
            set;
        }
        private Func<JObject, Type> _fieldTypeSelector = null;
        public Func<JObject, Type> FieldTypeSelector
        {
            get { return _fieldTypeSelector; }
            set { if (value == null) return; _fieldTypeSelector = value; }
        }

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
                if (QuoteField == null)
                    QuoteField = config.QuoteAllFields;
            }
            catch (Exception ex)
            {
                throw new ChoRecordConfigurationException("Invalid configuration found at '{0}' field.".FormatString(Name), ex);
            }
        }

        internal bool IgnoreFieldValue(object fieldValue)
        {
            if (IgnoreFieldValueMode == null)
                return fieldValue == null;

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
    }
}
