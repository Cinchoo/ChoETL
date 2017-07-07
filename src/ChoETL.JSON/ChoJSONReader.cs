using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ChoJSONReader<T> : ChoReader, IDisposable, IEnumerable<T>
        where T : class
    {
        private TextReader _textReader;
        private JsonTextReader _JSONReader;
        private IEnumerable<JToken> _jObjects;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _JSONReader = new JsonTextReader(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(TextReader txtReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(txtReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = txtReader;
        }

        public ChoJSONReader(JsonTextReader JSONReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(JSONReader, "JSONReader");

            Configuration = configuration;
            Init();

            _JSONReader = JSONReader;
        }

        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(jObjects, "JObjects");

            Configuration = configuration;
            Init();
            _jObjects = jObjects;
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
            {
                if (_textReader != null)
                    _textReader.Dispose();
                if (_JSONReader != null)
                    _JSONReader.Close();
            }

            System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoJSONReader<T> LoadText(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(inputText.ToStream(encoding), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoJSONReader<T> LoadJTokens(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(jObjects, configuration);
            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoJSONRecordReader rr = new ChoJSONRecordReader(recType, configuration);
            rr.TraceSwitch = ChoETLFramework.TraceSwitchOff;
            return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = new JsonTextReader(_textReader);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                var e = rr.AsEnumerable(_JSONReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                var e = rr.AsEnumerable(_jObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = new JsonTextReader(_textReader);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_JSONReader), rr);
                return dr;
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_jObjects), rr);
                return dr;
            }
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

        public ChoJSONReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONReader<T> WithJSONPath(string jsonPath)
        {
            Configuration.JSONPath = jsonPath;
            return this;
        }

        public ChoJSONReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        Configuration.JSONRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }
                    fnTrim = fn.NTrim();
                    Configuration.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fnTrim, (string)null));
                }

            }

            return this;
        }

        public ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.JSONRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                string fnTrim = name.NTrim();
                fieldType = fieldType == null ? typeof(string) : fieldType;
                jsonPath = jsonPath.IsNullOrWhiteSpace() ? null : jsonPath;

                Configuration.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fnTrim, jsonPath) { FieldType = fieldType, FieldValueTrimOption = fieldValueTrimOption, FieldName = fieldName });
            }

            return this;
        }

        public ChoJSONReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoJSONReader<T> Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        #endregion Fluent API
    }

    public class ChoJSONReader : ChoJSONReader<dynamic>
    {
        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoJSONReader(TextReader txtReader, ChoJSONRecordConfiguration configuration = null)
            : base(txtReader, configuration)
        {
        }
        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
