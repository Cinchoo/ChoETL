using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoKVPRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        public ChoKVPRecordFieldConfiguration(string name) : this(name, null)
        {
        }

        internal ChoKVPRecordFieldConfiguration(string name, ChoKVPRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
		{
            FieldName = name;
            if (attr != null)
            {
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name : attr.FieldName;
            }
        }

        internal void Validate(ChoKVPRecordConfiguration config)
        {
            try
            {
                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;
                if (FillChar != null)
                {
                    if (FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                    if (config.Separator.Contains(FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FillChar, config.Separator));
                    if (config.EOLDelimiter.Contains(FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FillChar, config.EOLDelimiter));
                }
                if (config.Comments != null)
                {
                    if ((from comm in config.Comments
                         where comm.Contains(FillChar.ToNString(' '))
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                    if ((from comm in config.Comments
                         where comm.Contains(config.Separator)
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains Delimiter. Not allowed.");
                    if ((from comm in config.Comments
                         where comm.Contains(config.EOLDelimiter)
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains EOLDelimiter. Not allowed.");
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
