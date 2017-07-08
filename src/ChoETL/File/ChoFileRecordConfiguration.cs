using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
        public string Comment
        {
            set
            {
                if (!value.IsNullOrWhiteSpace())
                    Comments = new string[] { value };
            }
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
        public bool EscapeQuoteAndDelimiter
        {
            get;
            set;
        }

        internal string BackslashQuote = @"\""";
        internal string DoubleQuoteChar = @"""""";
        private char _quoteChar = '"';
        [DataMember]
        public char QuoteChar
        {
            get { return _quoteChar; }
            set
            {
                if (_quoteChar != '\0')
                {
                    _quoteChar = value;
                    DoubleQuoteChar = "{0}{0}".FormatString(_quoteChar);
                }
            }
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
            get { return Encoding != null ? Encoding.CodePage : 0; }
            set
            {
                if (value > 0)
                    Encoding = Encoding.GetEncoding(value);
            }
        }

        private Encoding _encoding;
        public Encoding Encoding
        {
            get { return _encoding != null ? _encoding : Encoding.UTF8; }
            set { _encoding = value; }
        }

        protected ChoFileRecordConfiguration(Type recordType = null) : base(recordType)
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
            //Encoding = Encoding.UTF8;
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
                if (!recObjAttr.Encoding.IsNullOrWhiteSpace())
                    Encoding = Encoding.GetEncoding(recObjAttr.Encoding);
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

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

        public Encoding GetEncoding(Stream inStream)
        {
            if (_encoding == null)
            {
                try
                {
                    ChoETLLog.Info("Determining file encoding...");
                    Encoding = ChoFile.GetEncodingFromStream(inStream);
                    ChoETLLog.Info("Found {0} encoding in file.".FormatString(Encoding));
                }
                catch (Exception ex)
                {
                    Encoding = Encoding.UTF8;
                    ChoETLLog.Error("Error finding encoding in file. Default to UTF8.");
                    ChoETLLog.Error(ex.Message);
                }
            }

            return Encoding;
        }

        public Encoding GetEncoding(string fileName)
        {
            if (_encoding == null)
            {
                try
                {
                    ChoETLLog.Info("Determining '{0}' file encoding...".FormatString(fileName));
                    Encoding = ChoFile.GetEncodingFromFile(fileName);
                    ChoETLLog.Info("Found '{1}' encoding in '{0}' file.".FormatString(fileName, Encoding));
                }
                catch (Exception ex)
                {
                    Encoding = Encoding.UTF8;
                    ChoETLLog.Error("Error finding encoding in '{0}' file. Default to UTF8.".FormatString(fileName));
                    ChoETLLog.Error(ex.Message);
                }
            }

            return Encoding;
        }
    }
}
