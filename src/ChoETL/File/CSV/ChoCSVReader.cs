using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVReader<T> : IDisposable, IEnumerable<T>
        where T : class
    {
        private StreamReader _streamReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        internal TraceSwitch TraceSwitch;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            TraceSwitch = ChoETLFramework.TraceSwitch;
            Configuration = configuration;

            Init();

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVReader(StreamReader streamReader, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamReader = new StreamReader(inStream, Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public T Read()
        {
            if (_enumerator.Value.MoveNext())
                return _enumerator.Value.Current;
            else
                return default(T);
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _streamReader.Dispose();

            System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoCSVReader<T> LoadText(string inputText, ChoCSVRecordConfiguration configuration = null)
        {
            var r = new ChoCSVReader<T>(inputText.ToStream(), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoCSVRecordReader reader = new ChoCSVRecordReader(recType, configuration);
            reader.TraceSwitch = ChoETLFramework.TraceSwitchOff;
            return reader.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoCSVRecordReader reader = new ChoCSVRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            var e = reader.AsEnumerable(_streamReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoCSVRecordReader reader = new ChoCSVRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            reader.LoadSchema(_streamReader);

            var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.CSVRecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
            return dr;
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }

        #region Fluent API

        public ChoCSVReader<T> WithDelimiter(string delimiter)
        {
            Configuration.Delimiter = delimiter;
            return this;
        }

        public ChoCSVReader<T> WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoCSVReader<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoCSVReader<T> WithFields(params string[] fieldsNames)
        {
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        Configuration.CSVRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fn.Trim(), ++maxFieldPos));
                }

            }

            return this;
        }

        public ChoCSVReader<T> WithField(string fieldName, Type fieldType, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
            return WithField(fieldName, ++maxFieldPos, fieldType, quoteField, fieldValueTrimOption);

            return this;
        }

        public ChoCSVReader<T> WithField(string fieldName, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            return WithField(fieldName, typeof(string), quoteField, fieldValueTrimOption);
        }

        public ChoCSVReader<T> WithField(string fieldName, int position, Type fieldType, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            if (!fieldName.IsNullOrEmpty())
            {
                if (fieldType == null)
                    fieldType = typeof(string);

                if (!_clearFields)
                {
                    Configuration.CSVRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fieldName.Trim(), position) { FieldType = fieldType, QuoteField = quoteField, FieldValueTrimOption = fieldValueTrimOption });
            }

            return this;
        }

        public ChoCSVReader<T> WithField(string fieldName, int position, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            return WithField(fieldName, position, typeof(string), quoteField, fieldValueTrimOption);
        }

        #endregion Fluent API
    }

    public class ChoCSVReader : ChoCSVReader<ExpandoObject>
    {
        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVReader(StreamReader streamReader, ChoCSVRecordConfiguration configuration = null)
            : base(streamReader, configuration)
        {
        }
        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
