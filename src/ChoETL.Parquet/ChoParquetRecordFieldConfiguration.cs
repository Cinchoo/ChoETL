using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoParquetRecordFieldConfiguration : ChoFileRecordFieldConfiguration
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
        string name;
        public new string Name
        {
            get { return name != null ? name : base.Name; }
            internal set { name = value; }
        }
        public new bool IsDefaultValueSpecified { get; internal set; }
        public new bool IsFallbackValueSpecified { get; internal set; }
        public int Priority
        {
            get;
            set;
        }

        private int _fieldPosition = 0;
        [DataMember]
        public int FieldPosition
        {
            get { return _fieldPosition; }
            set
            {
                if (Priority == 0)
                    Priority = value;

                _fieldPosition = value;
            }
        }

        internal string[] AltFieldNamesArray = new string[] { };
        [DataMember]
        public string AltFieldNames
        {
            get;
            set;
        }

        public char ItemSeparator
        {
            get;
            set;
        }

        public char KeyValueSeparator
        {
            get;
            set;
        }

        public ChoParquetRecordFieldConfiguration(string name) : this(name, null)
        {
        }

        public ChoParquetRecordFieldConfiguration(string name, int position) : this(name, null)
        {
            FieldPosition = position;
        }

        internal ChoParquetRecordFieldConfiguration(string name, ChoParquetRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            DeclaringMemberInternal = FieldName = name;
            if (attr != null)
            {
                FieldPosition = attr.FieldPosition;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name : attr.FieldName;
                AltFieldNames = attr.AltFieldNames.IsNullOrWhiteSpace() ? AltFieldNames : attr.AltFieldNames;
            }
        }

        internal void Validate(ChoParquetRecordConfiguration config)
        {
            try
            {
                if (!AltFieldNames.IsNullOrWhiteSpace())
                    AltFieldNamesArray = AltFieldNames.SplitNTrim();

                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;
                if (FieldPosition <= 0)
                    throw new ChoRecordConfigurationException("Invalid '{0}' field position specified. Must be > 0.".FormatString(FieldPosition));
                if (FillChar != null)
                {
                    if (FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                    //if (config.EOLDelimiter.Contains(FillChar.Value))
                    //    throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FillChar, config.EOLDelimiter));
                }
                //if (config.Comments != null)
                //{
                //    if ((from comm in config.Comments
                //         where comm.Contains(FillChar.ToNString(' '))
                //         select comm).Any())
                //        throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                //    if ((from comm in config.Comments
                //         where comm.Contains(config.EOLDelimiter)
                //         select comm).Any())
                //        throw new ChoRecordConfigurationException("One of the Comments contains EOLDelimiter. Not allowed.");
                //}

                if (Size != null && Size.Value <= 0)
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (ErrorMode == null)
                    ErrorMode = config.ErrorMode; // config.ErrorMode;
                if (IgnoreFieldValueMode == null)
                    IgnoreFieldValueMode = config.IgnoreFieldValueMode;
                //if (QuoteField == null)
                //    QuoteField = config.QuoteAllFields;
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

        internal ChoFieldValueTrimOption GetFieldValueTrimOptionForReadInternal(Type fieldType, ChoFieldValueTrimOption? recordLevelFieldValueTrimOption)
        {
            return GetFieldValueTrimOptionForRead(fieldType, recordLevelFieldValueTrimOption);
        }
    }
}
