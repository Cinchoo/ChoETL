using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoFileRecordConfiguration : ChoRecordConfiguration
    {
        protected HashSet<string> IgnoreFields
        {
            get;
            set;
        } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public bool AllowReturnPartialLoadedRecs { get; set; }
        public bool? ThrowExceptionIfDynamicPropNotExists
        {
            get;
            set;
        }
        public bool AddEOLDelimiterAtEOF
        {
            get;
            set;
        }
        public bool Append
        {
            get;
            set;
        }
        internal bool TurnOffMemoryMappedFile
        {
            get;
            set;
        }

        internal bool LiteParsing
        {
            get;
            set;
        }
        private Dictionary<string, Type> _knownTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, Type> KnownTypes
        {
            get { return _knownTypes; }
            set { _knownTypes = value; }
        }

        public string KnownTypeDiscriminator
        {
            get;
            set;
        }
        private Func<object, Type> _recordTypeSelector = null;
        public Func<object, Type> RecordTypeSelector
        {
            get { return _recordTypeSelector; }
            set { if (value == null) return; _recordTypeSelector = value; }
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
        public bool QuoteLeadingAndTrailingSpaces
        {
            get;
            set;
        }
        //[DataMember]
        //public char? NestedKeySeparator
        //{
        //    //get;
        //    //set;
        //    get { return NestedKeySeparator; }
        //    set { NestedKeySeparator = value; }
        //}
        [DataMember]
        public char? NestedKeySeparator
        {
            get;
            set;
        }
        public bool ConvertToFlattenObject
        {
            get;
            set;
        }
        public bool ConvertToNestedObject
        {
            get;
            set;
        }
        public bool AllowNestedConversion
        {
            get;
            set;
        } = true;
        public int MaxNestedConversionArraySize
        {
            get;
            set;
        } = 100;

        public string ArrayValueNamePrefix
        {
            get;
            set;
        }
        public int? ArrayValueNameStartIndex
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
        public bool IgnoreRootDictionaryFieldPrefix
        {
            get;
            set;
        }
        public bool IgnoreParentPropertyNamePrefix
        {
            get { return IgnoreDictionaryFieldPrefix; }
            set { IgnoreDictionaryFieldPrefix = value; }
        }
        [DataMember]
        public char? ArrayIndexSeparator
        {
            get;
            set;
        }
        [DataMember]
        public char? ArrayEndIndexSeparator
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
        protected bool RecordTypeMapped
        {
            get;
            set;
        }
        internal bool RecordTypeMappedInternal
        {
            get => RecordTypeMapped;
            set => RecordTypeMapped = value;
        }

        [DataMember]
        public ChoNullValueHandling NullValueHandling
        {
            get;
            set;
        }
        public bool UseNestedKeyFormat { get; set; }
        public bool TurnOffContractResolverState { get; set; }

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
        public bool? MayHaveQuotedFields
        {
            get { return QuoteAllFields; }
            set { QuoteAllFields = value; }
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
        [DataMember]
        public string EncodingName
        {
            get { return Encoding != null ? Encoding.EncodingName : null; }
            set
            {
                if (value != null)
                {
                    //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    Encoding = Encoding.GetEncoding(value);
                }
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
            BufferSize = 1024 * 1024;
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
        protected void ClearFields()
        {
            IgnoredFields.Clear();
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoFileRecordObjectAttribute recObjAttr = recordType != null ? ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType) : null;
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
            else
            {
            }

            LoadKnownTypes(recordType);
        }

        private bool _knownTypeInitialized = false;
        protected void LoadKnownTypes(Type recordType)
        {
            if (_knownTypeInitialized)
                return;

            _knownTypeInitialized = true;
            if (recordType == null)
                return;

            if (_knownTypes == null)
                _knownTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

            _knownTypes = ChoTypeDescriptor.GetTypeAttributes<ChoKnownTypeAttribute>(recordType).Where(a => a.Type != null && !a.Value.IsNullOrWhiteSpace())
                .GroupBy(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.First().Type, _knownTypes.Comparer == null ? StringComparer.InvariantCultureIgnoreCase : _knownTypes.Comparer);

            var kta = ChoTypeDescriptor.GetTypeAttribute<ChoKnownTypeDiscriminatorAttribute>(recordType);
            if (kta != null && !kta.Discriminator.IsNullOrWhiteSpace())
                KnownTypeDiscriminator = kta.Discriminator.Trim();
        }

        protected string GetArrayIndexSeparator()
        {
            return ArrayIndexSeparator == null || ArrayIndexSeparator == ChoCharEx.NUL ?
                (ChoETLSettings.ArrayIndexSeparator == ChoCharEx.NUL ? String.Empty : ChoETLSettings.ArrayIndexSeparator.ToNString())
                : ArrayIndexSeparator.Value.ToNString();
        }

        protected char GetArrayIndexSeparatorChar()
        {
            return ArrayIndexSeparator == null || ArrayIndexSeparator == ChoCharEx.NUL ?
                (ChoETLSettings.ArrayIndexSeparator == ChoCharEx.NUL ? '_' : ChoETLSettings.ArrayIndexSeparator)
                : ArrayIndexSeparator.Value;
        }
        internal string GetArrayIndexSeparatorInternal()
        {
            return GetArrayIndexSeparator();
        }
        internal char GetArrayIndexSeparatorCharInternal()
        {
            return GetArrayIndexSeparatorChar();
        }

        protected override void Validate(object state)
        {
            base.Validate(state);

            if (EOLDelimiter.IsNullOrEmpty())
                throw new ChoRecordConfigurationException("EOLDelimiter can't be null or empty.");
            if (QuoteChar == ChoCharEx.NUL)
                throw new ChoRecordConfigurationException("Invalid '{0}' quote character specified.".FormatString(QuoteChar));
            if (EOLDelimiter.Contains(QuoteChar))
                throw new ChoRecordConfigurationException("QuoteChar [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(QuoteChar, EOLDelimiter));
            if (NestedKeySeparator != null)
            {
                if (NestedKeySeparator.Value == ChoCharEx.NUL)
                    throw new ChoRecordConfigurationException("Invalid '{0}' nested column separator specified.".FormatString(NestedKeySeparator));
                if (NestedKeySeparator.Value == QuoteChar)
                    throw new ChoRecordConfigurationException("Nested column separator [{0}] can't be quote character [{1}]".FormatString(NestedKeySeparator, QuoteChar));
                if (EOLDelimiter.Contains(NestedKeySeparator.Value))
                    throw new ChoRecordConfigurationException("Nested column separator [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(NestedKeySeparator, EOLDelimiter));
            }
            if (ArrayIndexSeparator != null)
            {
                //if (ArrayIndexSeparator.Value == ChoCharEx.NUL)
                //    throw new ChoRecordConfigurationException("Invalid '{0}' array index separator specified.".FormatString(ArrayIndexSeparator));
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

        protected Encoding GetEncoding(Stream inStream)
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
        internal Encoding GetEncodingInternal(Stream inStream)
        {
            return GetEncoding(inStream);
        }

        protected Encoding GetEncoding(string fileName)
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
        internal Encoding GetEncodingInternal(string fileName)
        {
            return GetEncoding(fileName);
        }
        protected virtual bool ContainsRecordConfigForType(Type rt)
        {
            throw new NotSupportedException();
        }
        internal bool ContainsRecordConfigForTypeInternal(Type rt)
        {
            return ContainsRecordConfigForType(rt);
        }
        protected virtual ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            throw new NotSupportedException();
        }
        internal ChoRecordFieldConfiguration[] GetRecordConfigForTypeInternal(Type rt)
        {
            return GetRecordConfigForType(rt);
        }
        protected virtual Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            throw new NotSupportedException();
        }
        internal Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForTypeInternal(Type rt)
        {
            return GetRecordConfigDictionaryForType(rt);
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
            fconfig.NestedKeySeparator = NestedKeySeparator;
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
