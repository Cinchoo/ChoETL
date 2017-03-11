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
    public class ChoKVPReader<T> : IDisposable, IEnumerable<T>
        where T : class
    {
        private StreamReader _streamReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        internal TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;

        public ChoKVPRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoKVPReader(string filePath, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoKVPReader(StreamReader streamReader, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoKVPReader(Stream inStream, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
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
                Configuration = new ChoKVPRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoKVPReader<T> LoadText(string inputText, ChoKVPRecordConfiguration configuration = null)
        {
            var r = new ChoKVPReader<T>(inputText.ToStream(), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoKVPRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoKVPRecordReader reader = new ChoKVPRecordReader(recType, configuration);
            reader.TraceSwitch = ChoETLFramework.TraceSwitchOff;
            return reader.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoKVPRecordReader reader = new ChoKVPRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            reader.RowsLoaded += NotifyRowsLoaded;
            var e = reader.AsEnumerable(_streamReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoKVPRecordReader reader = new ChoKVPRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            reader.LoadSchema(_streamReader);
            reader.RowsLoaded += NotifyRowsLoaded;
            var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.KVPRecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
            return dr;
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }

        private void NotifyRowsLoaded(object sender, ChoRowsLoadedEventArgs e)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
            {
                if (!e.IsFinal)
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " records loaded.");
                else
                    Console.WriteLine("Total " + e.RowsLoaded.ToString("#,##0") + " records loaded.");
            }
            else
                rowsLoadedEvent(this, e);
        }

        #region Fluent API

        public ChoKVPReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoKVPReader<T> WithDelimiter(string delimiter)
        {
            Configuration.Seperator = delimiter;
            return this;
        }

        public ChoKVPReader<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoKVPReader<T> WithFields(params string[] fieldsNames)
        {
            if (!fieldsNames.IsNullOrEmpty())
            {
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        Configuration.KVPRecordFieldConfigurations.Clear();
                        _clearFields = true;
                        Configuration.ColumnOrderStrict = true;
                    }

                    Configuration.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration(fn.Trim()));
                }

            }

            return this;
        }

        public ChoKVPReader<T> WithField(string fieldName, Type fieldType, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            if (!fieldName.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.KVPRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                Configuration.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration(fieldName.Trim()) { FieldType = fieldType, QuoteField = quoteField, FieldValueTrimOption = fieldValueTrimOption });
            }

            return this;
        }

        public ChoKVPReader<T> WithField(string fieldName, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim)
        {
            return WithField(fieldName, null, quoteField, fieldValueTrimOption);
        }

        public ChoKVPReader<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoKVPReader<T> ColumnOrderStrict(bool flag = true)
        {
            Configuration.ColumnOrderStrict = flag;
            return this;
        }

        #endregion Fluent API
    }

    public class ChoKVPReader : ChoKVPReader<ExpandoObject>
    {
        public ChoKVPReader(string filePath, ChoKVPRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoKVPReader(StreamReader streamReader, ChoKVPRecordConfiguration configuration = null)
            : base(streamReader, configuration)
        {
        }
        public ChoKVPReader(Stream inStream, ChoKVPRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
