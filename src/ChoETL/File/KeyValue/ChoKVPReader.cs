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
    public abstract class ChoBaseKVPReader : ChoReader, IChoCustomKVPReader
    {
        public event EventHandler<ChoKVPEventArgs> ToKVP;

        internal KeyValuePair<string, string>? RaiseToKVP(string recText)
        {
            EventHandler<ChoKVPEventArgs> eh = ToKVP;
            if (eh == null)
                return null;

            ChoKVPEventArgs e = new ChoKVPEventArgs() { RecText = recText };
            eh(this, e);
            return e.KVP;
        }
    }

    public class ChoKVPReader<T> : ChoBaseKVPReader, IDisposable, IEnumerable<T>
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

        public ChoKVPRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoKVPReader(ChoKVPRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoKVPReader(string filePath, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoKVPReader(TextReader textReader, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = textReader;
        }

        public ChoKVPReader(Stream inStream, ChoKVPRecordConfiguration configuration = null)
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

        public ChoKVPReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoKVPReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = textReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoKVPReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public void Close()
        {
            Dispose();
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
                {
                    _textReader.Dispose();
                    _textReader = null;
                }
            }

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;

            _closeStreamOnDispose = false;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoKVPRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoKVPReader<T> LoadText(string inputText, Encoding encoding = null, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoKVPReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoKVPRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            ChoKVPRecordReader rr = new ChoKVPRecordReader(recType, configuration);
            rr.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;
            return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoKVPRecordReader rr = new ChoKVPRecordReader(typeof(T), Configuration);
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
            ChoKVPRecordReader rr = new ChoKVPRecordReader(typeof(T), Configuration);
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

        public int Fill(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader());

            return dt.Rows.Count;
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

        #region Fluent API

        public ChoKVPReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoKVPReader<T> WithDelimiter(string delimiter)
        {
            Configuration.Separator = delimiter;
            return this;
        }

        public ChoKVPReader<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoKVPReader<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                string fnTrim = null;
                if (!_clearFields)
                {
                    Configuration.KVPRecordFieldConfigurations.Clear();
                    _clearFields = true;
                    Configuration.MapRecordFields(Configuration.RecordType);
                }
                fnTrim = fieldName.NTrim();
                if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.KVPRecordFieldConfigurations.Remove(Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
            }

            return this;
        }

        public ChoKVPReader<T> WithFields(params string[] fieldsNames)
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
                        Configuration.KVPRecordFieldConfigurations.Clear();
                        _clearFields = true;
                        Configuration.MapRecordFields(Configuration.RecordType);
                        Configuration.ColumnOrderStrict = true;
                    }

                    fnTrim = fn.NTrim();
                    if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                        Configuration.KVPRecordFieldConfigurations.Remove(Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    Configuration.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration(fnTrim) { FieldName = fn });
                }

            }

            return this;
        }

        public ChoKVPReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.KVPRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }
                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;

                string fnTrim = name.NTrim();
                if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.KVPRecordFieldConfigurations.Remove(Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());

                Configuration.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration(fnTrim) { FieldType = fieldType, QuoteField = quoteField, FieldValueTrimOption = fieldValueTrimOption, FieldName = fieldName,
                    ValueConverter = valueConverter,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue
                });
            }

            return this;
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

        public ChoKVPReader<T> Configure(Action<ChoKVPRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoKVPReader<T> Setup(Action<ChoKVPReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API
    }

    public class ChoKVPReader : ChoKVPReader<dynamic>
    {
        public ChoKVPReader(string filePath, ChoKVPRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoKVPReader(TextReader textReader, ChoKVPRecordConfiguration configuration = null)
            : base(textReader, configuration)
        {
        }
        public ChoKVPReader(Stream inStream, ChoKVPRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
    public class ChoKVPEventArgs : EventArgs
    {
        public string RecText
        {
            get;
            internal set;
        }

        public KeyValuePair<string, string>? KVP
        {
            get;
            internal set;
        }
    }

    public interface IChoCustomKVPReader
    {
        event EventHandler<ChoKVPEventArgs> ToKVP;
    }

}
