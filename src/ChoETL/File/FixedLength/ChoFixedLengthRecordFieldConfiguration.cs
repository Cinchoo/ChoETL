using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoFixedLengthRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        [DataMember]
        public int StartIndex
        {
            get;
            set;
        }
        internal string[] AltFieldNamesArray = new string[] { };
        [DataMember]
        public string AltFieldNames
        {
            get;
            set;
        }

        public ChoFixedLengthRecordFieldConfiguration(string name, int startIndex, int size) : this(name, null)
        {
            StartIndex = startIndex;
            Size = size;
        }

        internal ChoFixedLengthRecordFieldConfiguration(string name, ChoFixedLengthRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            FieldName = name;
            if (attr != null)
            {
                StartIndex = attr.StartIndex;
                Size = attr.Size;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name : attr.FieldName;
                AltFieldNames = attr.AltFieldNames.IsNullOrWhiteSpace() ? AltFieldNames : attr.AltFieldNames;
            }
        }

        internal void Validate(ChoFixedLengthRecordConfiguration config)
        {
            try
            {
                if (!AltFieldNames.IsNullOrWhiteSpace())
                    AltFieldNamesArray = AltFieldNames.SplitNTrim();

                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;
                if (StartIndex < 0)
                    throw new ChoRecordConfigurationException("StartIndex must be > 0.");
                if (StartIndex == 0 && (Size == null || Size.Value < 0))
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (FillChar != null)
                {
                    if (FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                    if (config.EOLDelimiter.Contains(FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(FillChar, config.EOLDelimiter));
                }
                if (config.Comments != null)
                {
                    if ((from comm in config.Comments
                         where comm.Contains(FillChar.ToNString(' '))
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                    if ((from comm in config.Comments
                         where comm.Contains(config.EOLDelimiter)
                         select comm).Any())
                        throw new ChoRecordConfigurationException("One of the Comments contains EOLDelimiter. Not allowed.");
                }
                if (StartIndex == 0 && (Size == null || Size.Value < 0))
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
    }
}
