using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoFileRecordConfiguration : ChoRecordConfiguration
    {
        public int BufferSize
        {
            get;
            set;
        }
        public string[] Comments
        {
            get;
            set;
        }
        public CultureInfo Culture
        {
            get;
            set;
        }
        public string EOLDelimiter
        {
            get;
            set;
        }
        public bool IgnoreEmptyLine
        {
            get;
            set;
        }
        public bool ColumnCountStrict
        {
            get;
            set;
        }
        public char QuoteChar
        {
            get;
            set;
        }
        public bool QuoteAllFields
        {
            get;
            set;
        }
        public ChoStringSplitOptions StringSplitOptions
        {
            get;
            set;
        }
        public Encoding Encoding
        {
            get;
            set;
        }

        public ChoFileRecordConfiguration(Type recordType = null)
        {
            BufferSize = 2048;
            Comments = new string[] { "#", "//" };
            Culture = CultureInfo.CurrentCulture;
            EOLDelimiter = Environment.NewLine;
            IgnoreEmptyLine = false;
            ColumnCountStrict = false;
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
            if (Comments.Contains(EOLDelimiter))
                throw new ChoRecordConfigurationException("One of the Comments contains EOLDelimiter. Not allowed.");
        }
    }
}
