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
        private Func<object, Type> _recordSelector = null;
        public Func<object, Type> RecordSelector
        {
            get { return _recordSelector; }
            set { if (value == null) return; _recordSelector = value; }
        }
        private Func<string, string> _recordTypeCodeExtractor = null;
        public Func<string, string> RecordTypeCodeExtractor
        {
            get { return _recordTypeCodeExtractor; }
            set { _recordTypeCodeExtractor = value; }
        }

        [DataMember]
        public bool IgnoreIfNoRecordTypeFound
        {
            get;
            set;
        }
        [DataMember]
        public int MaxScanRows
        {
            get;
            set;
        }
        public bool AutoDiscoverFieldTypes
        {
            get;
            set;
        }
        [DataMember]
        public int BufferSize
        {
            get;
            set;
        }
        public bool? DetectEncodingFromByteOrderMarks
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
        private int _maxLineSize = 32768;
        public int MaxLineSize
        {
            get { return _maxLineSize; }
            set
            {
                if (value > 32768)
                    _maxLineSize = value;
            }
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
        [DataMember]
        public char? NestedColumnSeparator
        {
            get;
            set;
        }
        [DataMember]
        public bool AllowNestedArrayConversion
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreDictionaryFieldPrefix
        {
            get;
            set;
        }
        [DataMember]
        public char? ArrayIndexSeparator
        {
            get;
            set;
        }
        [DataMember]
        public char? ArrayValueSeparator
        {
            get;
            set;
        }
        public bool SupportsMultiRecordTypes
        {
            get;
            set;
        }
        public bool RecordTypeMapped
        {
            get;
            set;
        }
        [DataMember]
        public ChoNullValueHandling NullValueHandling
        {
            get;
            set;
        }
        public bool UseNestedKeyFormat { get; set; }

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
        public char? QuoteEscapeChar
        {
            get;
            set;
        }

        [DataMember]
        public bool? QuoteAllFields
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

        private Encoding _defaultEncoding = Encoding.Default; // new UTF8Encoding(false);
        private Encoding _encoding;
        public Encoding Encoding
        {
            get { return _encoding != null ? _encoding : _defaultEncoding; }
            set { _encoding = value; }
        }
        [DataMember]
        public string NullValue
        {
            get;
            set;
        }
        public ChoFieldValueTrimOption? FieldValueTrimOption
        {
            get;
            set;
        }

        protected ChoFileRecordConfiguration(Type recordType = null) : base(recordType)
        {
            UseNestedKeyFormat = true;
            AutoDiscoverFieldTypes = true;
            MaxScanRows = 0;
            IgnoreIfNoRecordTypeFound = true;
            BufferSize = 4096;
            Comments = null; // new string[] { "#", "//" };
            Culture = CultureInfo.CurrentCulture;
            EOLDelimiter = Environment.NewLine;
            IgnoreEmptyLine = false;
            ColumnCountStrict = false;
            ColumnOrderStrict = false;
            QuoteChar = '"';
            //QuoteAllFields = false;
            StringSplitOptions = ChoStringSplitOptions.None;
            //Encoding = Encoding.UTF8;
            if (QuoteEscapeChar == null)
                QuoteEscapeChar = '\0';
            //DetectEncodingFromByteOrderMarks = true;
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            if (recordType == null)
                return;

            ChoFileRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                AutoDiscoverFieldTypes = true;
                MaxScanRows = 0;
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
                QuoteAllFields = recObjAttr.QuoteAllFieldsInternal;
                StringSplitOptions = recObjAttr.StringSplitOptions;
                if (!recObjAttr.Encoding.IsNullOrWhiteSpace())
                    Encoding = Encoding.GetEncoding(recObjAttr.Encoding);
                NullValue = recObjAttr.NullValue;
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
            if (NestedColumnSeparator != null)
            {
                if (NestedColumnSeparator.Value == ChoCharEx.NUL)
                    throw new ChoRecordConfigurationException("Invalid '{0}' nested column separator specified.".FormatString(NestedColumnSeparator));
                if (NestedColumnSeparator.Value == QuoteChar)
                    throw new ChoRecordConfigurationException("Nested column separator [{0}] can't be quote character [{1}]".FormatString(NestedColumnSeparator, QuoteChar));
                if (EOLDelimiter.Contains(NestedColumnSeparator.Value))
                    throw new ChoRecordConfigurationException("Nested column separator [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(NestedColumnSeparator, EOLDelimiter));
            }
            if (ArrayIndexSeparator != null)
            {
                if (ArrayIndexSeparator.Value == ChoCharEx.NUL)
                    throw new ChoRecordConfigurationException("Invalid '{0}' array index separator specified.".FormatString(ArrayIndexSeparator));
                if (ArrayIndexSeparator.Value == QuoteChar)
                    throw new ChoRecordConfigurationException("Array index separator [{0}] can't be quote character [{1}]".FormatString(ArrayIndexSeparator, QuoteChar));
                if (EOLDelimiter.Contains(ArrayIndexSeparator.Value))
                    throw new ChoRecordConfigurationException("Array index separator [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(ArrayIndexSeparator, EOLDelimiter));
            }
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
                    Encoding = _defaultEncoding;
                    ChoETLLog.Error("Error finding encoding in file. Default to UTF8.");
                    ChoETLLog.Error(ex.Message);
                }
                finally
                {
                    try
                    {
                        inStream.Position = 0;
                    }
                    catch { }
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
                    Encoding = _defaultEncoding;
                    ChoETLLog.Error("Error finding encoding in '{0}' file. Default to UTF8.".FormatString(fileName));
                    ChoETLLog.Error(ex.Message);
                }
            }

            return Encoding;
        }

        public virtual bool ContainsRecordConfigForType(Type rt)
        {
            throw new NotSupportedException();
        }

        public virtual ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            throw new NotSupportedException();
        }

        public virtual Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            throw new NotSupportedException();
        }

        protected override void Clone(ChoRecordConfiguration config)
        {
            base.Clone(config);

            if (!(config is ChoFileRecordConfiguration))
                return;

            ChoFileRecordConfiguration fconfig = config as ChoFileRecordConfiguration;
            fconfig.MaxScanRows = MaxScanRows;
            fconfig.BufferSize = BufferSize;
            fconfig.Comments = Comments;
            fconfig.CultureName = CultureName;
            fconfig.Culture = Culture;
            fconfig.EOLDelimiter = EOLDelimiter;
            fconfig.MayContainEOLInData = MayContainEOLInData;
            fconfig.IgnoreEmptyLine = IgnoreEmptyLine;
            fconfig.ColumnCountStrict = ColumnCountStrict;
            fconfig.ColumnOrderStrict = ColumnOrderStrict;
            fconfig.EscapeQuoteAndDelimiter = EscapeQuoteAndDelimiter;
            fconfig.NestedColumnSeparator = NestedColumnSeparator;
            fconfig.QuoteChar = QuoteChar;
            fconfig.BackslashQuote = BackslashQuote;
            fconfig.DoubleQuoteChar = DoubleQuoteChar;
            fconfig.QuoteAllFields = QuoteAllFields;
            fconfig.StringSplitOptions = StringSplitOptions;
            fconfig.EncodingPage = EncodingPage;
            fconfig.NullValue = NullValue;
        }
    }
}
