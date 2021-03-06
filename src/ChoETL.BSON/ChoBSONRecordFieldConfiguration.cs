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
    public class ChoBSONRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        string name;
        public new string Name
        {
            get { return name != null ? name : base.Name; }
            internal set { name = value; }
        }
        public new bool IsDefaultValueSpecified { get; internal set; }
        public new bool IsFallbackValueSpecified { get; internal set; }

        internal PropertyDescriptor PropertyDescriptor
        {
            get;
            set;
        }

        public ChoBSONRecordFieldConfiguration(string name) : this(name, null)
        {
        }

        internal ChoBSONRecordFieldConfiguration(string name, ChoBSONRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            DeclaringMember = FieldName = name;
            if (attr != null)
            {
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name : attr.FieldName;
            }
        }

        internal void Validate(ChoBSONRecordConfiguration config)
        {
            try
            {
                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;

                if (Size != null && Size.Value <= 0)
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (ErrorMode == null)
                    ErrorMode = config.ErrorMode; // config.ErrorMode;
                if (IgnoreFieldValueMode == null)
                    IgnoreFieldValueMode = config.IgnoreFieldValueMode;
                if (QuoteField == null)
                    QuoteField = config.QuoteAllFields;
                if (NullValue == null)
                    NullValue = config.NullValue;
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

            if (fieldValue == null)
                return (IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null;
            else if (fieldValue == DBNull.Value)
                return (IgnoreFieldValueMode & ChoIgnoreFieldValueMode.DBNull) == ChoIgnoreFieldValueMode.DBNull;
            else if (fieldValue is string)
            {
                string strValue = fieldValue as string;
                if (String.IsNullOrEmpty(strValue))
                    return (IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Empty) == ChoIgnoreFieldValueMode.Empty;
                else if (String.IsNullOrWhiteSpace(strValue))
                    return (IgnoreFieldValueMode & ChoIgnoreFieldValueMode.WhiteSpace) == ChoIgnoreFieldValueMode.WhiteSpace;
            }

            return false;
        }

    }
}
