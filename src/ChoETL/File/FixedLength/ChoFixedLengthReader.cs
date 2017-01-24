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
    public class ChoFixedLengthReader<T> : IDisposable, IEnumerable<T>
        where T : class
    {
        private StreamReader _streamReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;

        public ChoFixedLengthRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoFixedLengthReader(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoFixedLengthReader(StreamReader streamReader, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoFixedLengthReader(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
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
                Configuration = new ChoFixedLengthRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoFixedLengthRecordReader reader = new ChoFixedLengthRecordReader(typeof(T), Configuration);
            var e = reader.AsEnumerable(_streamReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoFixedLengthRecordReader reader = new ChoFixedLengthRecordReader(typeof(T), Configuration);
            reader.LoadSchema(_streamReader);

            var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.RecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
            return dr;
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }

        public static ChoFixedLengthReader<T> LoadText(string inputText, ChoFixedLengthRecordConfiguration configuration = null)
        {
            var r = new ChoFixedLengthReader<T>(inputText.ToStream(), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        #region Fluent API

        public ChoFixedLengthReader<T> WithRecordLength(int length)
        {
            Configuration.RecordLength = length;
            return this;
        }


        public ChoFixedLengthReader<T> WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoFixedLengthReader<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoFixedLengthReader<T> WithField(string fieldName, int startIndex, int size, Type fieldType, bool? quoteField = null)
        {
            if (!fieldName.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.RecordFieldConfigurations.Clear();
                    _clearFields = true;
                }
                if (fieldType == null)
                    fieldType = typeof(string);

                Configuration.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration(fieldName.Trim(), startIndex, size) { FieldType = fieldType, QuoteField = quoteField });
            }

            return this;
        }

        public ChoFixedLengthReader<T> WithField(string fieldName, int startIndex, int size, bool? quoteField = null)
        {
            return WithField(fieldName, startIndex, size, typeof(string), quoteField);
        }

        #endregion Fluent API
    }

    public class ChoFixedLengthReader : ChoFixedLengthReader<ExpandoObject>
    {
        public ChoFixedLengthReader(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoFixedLengthReader(StreamReader streamReader, ChoFixedLengthRecordConfiguration configuration = null)
            : base(streamReader, configuration)
        {
        }
        public ChoFixedLengthReader(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
