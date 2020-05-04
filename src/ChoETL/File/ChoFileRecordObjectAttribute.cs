using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoFileRecordObjectAttribute : ChoRecordObjectAttribute
    {
        public string EOLDelimiter
        {
            get;
            set;
        }
        private string _cultureName;
        public string CultureName
        {
            get { return _cultureName; }
            set
            {
                _cultureName = value;
                Culture = _cultureName.IsNullOrEmpty() ? CultureInfo.CurrentCulture : new CultureInfo(_cultureName);
            }
        }
        internal CultureInfo Culture
        {
            get;
            private set;
        }
        public bool IgnoreEmptyLine
        {
            get;
            set;
        }
        public string Comments
        {
            get;
            set;
        }
        public char QuoteChar
        {
            get;
            set;
        }
        internal bool? QuoteAllFieldsInternal = null;
        public bool QuoteAllFields
        {
            get { return QuoteAllFieldsInternal == null ? false : QuoteAllFieldsInternal.Value; }
            set { QuoteAllFieldsInternal = value; }
        }
        public ChoStringSplitOptions StringSplitOptions
        {
            get;
            set;
        }
        public string Encoding
        {
            get;
            set;
        }
        public bool ColumnCountStrict
        {
            get;
            set;
        }
        public bool ColumnOrderStrict
        {
            get;
            set;
        }
        public int BufferSize
        {
            get;
            set;
        }
        public string NullValue
        {
            get;
            set;
        }

        public ChoFileRecordObjectAttribute()
        {
            EOLDelimiter = Environment.NewLine;
            BufferSize = 2048;
            Comments = "#, //";
            Culture = CultureInfo.CurrentCulture;
            EOLDelimiter = Environment.NewLine;
            IgnoreEmptyLine = false;
            ColumnCountStrict = false;
            ColumnOrderStrict = false;
            QuoteChar = '"';
            StringSplitOptions = ChoStringSplitOptions.None;
            Encoding = "UTF-8";
        }
    }
}
