using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        public int FieldPosition
        {
            get;
            set;
        }

        public string FieldName
        {
            get;
            set;
        }

        public ChoCSVRecordFieldConfiguration(string name, ChoCSVRecordFieldAttribute attr = null) : base(name, attr)
        {
            if (attr != null)
            {
                FieldPosition = attr.FieldPosition;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name : attr.FieldName;
            }
        }

        internal void Validate(ChoCSVRecordConfiguration config)
        {
            try
            {
                if (FieldPosition < 0)
                    throw new ChoRecordConfigurationException("Invalid '{0}' field position specified. Must be > 0.".FormatString(FieldPosition));
                if (FillChar == ChoCharEx.NUL)
                    throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                if (config.Delimiter.Contains(FillChar))
                    throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FillChar, config.Delimiter));
                if (config.EOLDelimiter.Contains(FillChar))
                    throw new ChoRecordConfigurationException("FillChar [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(FillChar, config.EOLDelimiter));
                if ((from comm in config.Comments
                     where comm.Contains(FillChar.ToString())
                     select comm).Any())
                    throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");

                if (Size != null && Size.Value <= 0)
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (ErrorMode == null)
                    ErrorMode = config.ErrorMode;
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
