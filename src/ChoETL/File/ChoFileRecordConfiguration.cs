using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoFileRecordConfiguration : ChoRecordConfiguration
    {
        [DataMember]
        public int BufferSize
        {
            get;
            set;
        }
        [DataMember]
        public string[] Comments
        {
            get;
            set;
        }
        [DataMember]
        public string CultureName
        {
            get { return Culture.Name; }
            set
            {
                Culture = value != null ? new CultureInfo(value) : CultureInfo.CurrentCulture;
            }
        }
        [ChoIgnoreMember]
        public CultureInfo Culture
        {
            get;
            set;
        }
        [DataMember]
        public string EOLDelimiter
        {
            get;
            set;
        }
        [DataMember]
        public bool MayContainEOLInData
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreEmptyLine
        {
            get;
            set;
        }
        [DataMember]
        public bool ColumnCountStrict
        {
            get;
            set;
        }
        [DataMember]
        public bool ColumnOrderStrict
        {
            get;
            set;
        }
        [DataMember]
        public char QuoteChar
        {
            get;
            set;
        }
        [DataMember]
        public bool QuoteAllFields
        {
            get;
            set;
        }
        [DataMember]
        public ChoStringSplitOptions StringSplitOptions
        {
            get;
            set;
        }
        [DataMember]
        public int EncodingPage
        {
            get { return Encoding.CodePage; }
            set { Encoding = Encoding.GetEncoding(value); }
        }

        public Encoding Encoding
        {
            get;
            set;
        }

        internal ChoFileRecordConfiguration(Type recordType = null) : base(recordType)
        {
            BufferSize = 4096;
            Comments = null; // new string[] { "#", "//" };
            Culture = CultureInfo.CurrentCulture;
            EOLDelimiter = Environment.NewLine;
            IgnoreEmptyLine = false;
            ColumnCountStrict = false;
            ColumnOrderStrict = false;
            QuoteChar = '"';
            QuoteAllFields = false;
            StringSplitOptions = ChoStringSplitOptions.None;
            Encoding = Encoding.UTF8;
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoFileRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                if (recObjAttr.BufferSize > 0)
                    BufferSize = recObjAttr.BufferSize;
                if (recObjAttr.Comments.IsNullOrWhiteSpace())
                    Comments = new string[] { };
                else
                    Comments = recObjAttr.Comments.SplitNTrim(',');
                Culture = recObjAttr.Culture;
                EOLDelimiter = recObjAttr.EOLDelimiter;
                IgnoreEmptyLine = recObjAttr.IgnoreEmptyLine;
                ColumnCountStrict = recObjAttr.ColumnCountStrict;
                ColumnOrderStrict = recObjAttr.ColumnOrderStrict;
                QuoteChar = recObjAttr.QuoteChar;
                QuoteAllFields = recObjAttr.QuoteAllFields;
                StringSplitOptions = recObjAttr.StringSplitOptions;
                Encoding = recObjAttr.Encoding.CastTo<Encoding>(Encoding);
            }
        }

        public virtual void Validate(object state)
        {
            if (EOLDelimiter.IsNullOrEmpty())
                throw new ChoRecordConfigurationException("EOLDelimiter can't be null or empty.");
            if (QuoteChar == ChoCharEx.NUL)
                throw new ChoRecordConfigurationException("Invalid '{0}' quote character specified.".FormatString(QuoteChar));
            if (EOLDelimiter.Contains(QuoteChar))
                throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(QuoteChar, EOLDelimiter));
            if (Comments != null)
            {
                if (Comments.Contains(EOLDelimiter))
                    throw new ChoRecordConfigurationException("One of the Comments contains EOLDelimiter. Not allowed.");
                else if (Comments.Where(c => c.IsNullOrWhiteSpace()).Any())
                    throw new ChoRecordConfigurationException("One of the Comments contains Whitespace characters. Not allowed.");
            }
        }
    }
}
