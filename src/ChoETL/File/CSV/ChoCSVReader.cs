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
    public class ChoCSVReader<T> : ChoReader, IDisposable, IEnumerable<T>
        where T : class
    {
        private TextReader _textReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoMapColumnEventArgs> MapColumn;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVReader(ChoCSVRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVReader(TextReader textReader, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = textReader;
        }

        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
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
                _textReader.Dispose();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);
            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        public IEnumerable<T> DeserializeText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoCSVReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(string filePath, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoCSVReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(TextReader textReader, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoCSVReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(Stream inStream, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoCSVReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static ChoCSVReader<T> LoadText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoCSVReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            ChoCSVRecordReader rr = new ChoCSVRecordReader(recType, configuration);
            rr.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;
            return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoCSVRecordReader rr = new ChoCSVRecordReader(typeof(T), Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            var e = rr.AsEnumerable(_textReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoCSVRecordReader rr = new ChoCSVRecordReader(typeof(T), Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_textReader), rr);
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
                    ChoETLLog.Info(e.RowsLoaded.ToString("#,##0") + " records loaded.");
                else
                    ChoETLLog.Info("Total " + e.RowsLoaded.ToString("#,##0") + " records loaded.");
            }
            else
                rowsLoadedEvent(this, e);
        }

        public override bool RaiseMapColumn(int colPos, string colName, out string newColName)
        {
            newColName = null;
            EventHandler<ChoMapColumnEventArgs> mapColumn = MapColumn;
            if (mapColumn == null)
            {
                var fc = Configuration.CSVRecordFieldConfigurations.Where(c => c.AltFieldNamesArray.Contains(colName)).FirstOrDefault();
                if (fc != null)
                {
                    newColName = fc.FieldName;
                    return true;
                }
                return false;
            }

            var ea = new ChoMapColumnEventArgs(colPos, colName);
            mapColumn(this, ea);
            if (ea.Resolved)
                newColName = ea.NewColName;

            return ea.Resolved;
        }

        #region Fluent API

        public ChoCSVReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoCSVReader<T> WithDelimiter(string delimiter)
        {
            Configuration.Delimiter = delimiter;
            return this;
        }

        public ChoCSVReader<T> WithEOLDelimiter(string delimiter)
        {
            Configuration.EOLDelimiter = delimiter;
            return this;
        }

        public ChoCSVReader<T> WithFirstLineHeader(bool ignoreHeader = false)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

            return this;
        }
        public ChoCSVReader<T> WithHeaderLineAt(int pos = 1, bool ignoreHeader = false)
        {
            Configuration.FileHeaderConfiguration.HeaderLineAt = pos;
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = ignoreHeader;

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
                        //Configuration.ColumnOrderStrict = true;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fn, ++maxFieldPos) { FieldName = fn });
                }
            }

            return this;
        }

        public ChoCSVReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null)
        {
            int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
            return WithField(name, ++maxFieldPos, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter, defaultValue, fallbackValue, altFieldNames);
        }

        public ChoCSVReader<T> WithField(string name, int position, Type fieldType = null, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.CSVRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }
                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;

                Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(name, position) { FieldType = fieldType, QuoteField = quoteField, FieldValueTrimOption = fieldValueTrimOption, FieldName = fieldName,
                    ValueConverter = valueConverter, DefaultValue = defaultValue, FallbackValue = fallbackValue, AltFieldNames = altFieldNames
                });
            }

            return this;
        }

        public ChoCSVReader<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoCSVReader<T> ColumnOrderStrict(bool flag = true)
        {
            Configuration.ColumnOrderStrict = flag;
            return this;
        }

        public ChoCSVReader<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoCSVReader<T> Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoCSVReader<T> Setup(Action<ChoCSVReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API
    }

    public class ChoCSVReader : ChoCSVReader<dynamic>
    {
        public ChoCSVReader(ChoCSVRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVReader(TextReader textReader, ChoCSVRecordConfiguration configuration = null)
            : base(textReader, configuration)
        {
        }
        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
