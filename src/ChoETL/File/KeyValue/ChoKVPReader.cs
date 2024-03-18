using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoBaseKVPReader : ChoReader, IChoCustomKVPReader
    {
        public event EventHandler<ChoKVPEventArgs> ToKVP;
        public bool HasCustomKVPSubscribed
        {
            get
            {
                EventHandler<ChoKVPEventArgs> eh = ToKVP;
                return (eh != null);
            }
        }

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
        private Lazy<TextReader> _textReader;
        private IEnumerable<string> _lines;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoKVPRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoKVPReader(StringBuilder sb, ChoKVPRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

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

            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoKVPReader(TextReader textReader, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = new Lazy<TextReader>(() => textReader);
        }

        internal ChoKVPReader(IEnumerable<string> lines, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(lines, "Lines");

            Configuration = configuration;
            Init();

            _lines = lines;
        }

        public ChoKVPReader(Stream inStream, ChoKVPRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
            //_closeStreamOnDispose = true;
        }

        public ChoKVPReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoKVPReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => textReader);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoKVPReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }

            //_closeStreamOnDispose = true;

            return this;
        }

        public void Close()
        {
            Dispose();
        }

        public T Read()
        {
            CheckDisposed();
            if (_enumerator.Value.MoveNext())
                return _enumerator.Value.Current;
            else
                return default(T);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_textReader != null)
                {
                    _textReader.Value.Dispose();
                    _textReader = null;
                }
            }

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;

            _closeStreamOnDispose = false;

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            _isDisposed = false;
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());

            var recordType = typeof(T).ResolveRecordType();
            if (Configuration == null)
                Configuration = new ChoKVPRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;
            Configuration.IsDynamicObjectInternal = Configuration.RecordTypeInternal.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        public static ChoKVPReader<T> LoadText(string inputText, Encoding encoding = null, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoKVPReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoKVPReader<T> LoadText(string inputText, ChoKVPRecordConfiguration config, TraceSwitch traceSwitch = null)
        {
            return LoadText(inputText, null, config, traceSwitch);
        }

        public static ChoKVPReader<T> LoadLines(IEnumerable<string> inputLines, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoKVPReader<T>(inputLines, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoKVPRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null, ChoReader parent = null)
        {
            ChoKVPRecordReader rr = new ChoKVPRecordReader(recType, configuration);
            rr.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;
            return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            ChoKVPRecordReader rr = new ChoKVPRecordReader(typeof(T), Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
            var e = _lines != null ? rr.AsEnumerable(_lines).GetEnumerator() : rr.AsEnumerable(_textReader.Value).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                ++_recordNumber;
                return e.MoveNext();
            }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            return AsDataReader(null);
        }

        private IDataReader AsDataReader(Action<IDictionary<string, Type>> membersDiscovered)
        {
            CheckDisposed();
            ChoKVPRecordReader rr = new ChoKVPRecordReader(typeof(T), Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
            var dr = new ChoEnumerableDataReader(_lines != null ? rr.AsEnumerable(_lines) : rr.AsEnumerable(_textReader.Value), rr);
            return dr;
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Locale = Configuration.Culture;
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

        public override bool TryValidate(object target, ICollection<ValidationResult> validationResults)
        {
            ChoObjectValidationMode prevObjValidationMode = Configuration.ObjectValidationMode;

            if (Configuration.ObjectValidationMode == ChoObjectValidationMode.Off)
                Configuration.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel;

            try
            {
                T rec = default(T);
                while ((rec = Read()) != null)
                {

                }
                return IsValid;
            }
            finally
            {
                Configuration.ObjectValidationMode = prevObjValidationMode;
            }
        }

        public void AddBcpColumnMappings(SqlBulkCopy bcp)
        {
            foreach (var fn in Configuration.KVPRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null || columnMappings.Count == 0)
                columnMappings = Configuration.KVPRecordFieldConfigurations.Select(fc => fc.FieldName)
                    .ToDictionary(fn => fn, fn => fn);

            AsDataReader((d) =>
            {
                if (columnMappings == null || columnMappings.Count == 0)
                {
                    columnMappings = new Dictionary<string, string>();
                    foreach (var key in d.Keys)
                    {
                        columnMappings.Add(key, key);
                    }
                }
            }).Bcp(connectionString, tableName, batchSize, notifyAfter, timeoutInSeconds,
                rowsCopied, columnMappings, copyOptions);
        }
        public void Bcp(SqlConnection connection, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default,
            SqlTransaction transaction = null)
        {
            if (columnMappings == null || columnMappings.Count == 0)
                columnMappings = Configuration.KVPRecordFieldConfigurations.Select(fc => fc.FieldName)
                    .ToDictionary(fn => fn, fn => fn);

            AsDataReader((d) =>
            {
                if (columnMappings == null || columnMappings.Count == 0)
                {
                    columnMappings = new Dictionary<string, string>();
                    foreach (var key in d.Keys)
                    {
                        columnMappings.Add(key, key);
                    }
                }
            }).Bcp(connection, tableName, batchSize, notifyAfter, timeoutInSeconds,
                rowsCopied, columnMappings, copyOptions);
        }

        #region Fluent API

        public ChoKVPReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoKVPReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoKVPReader<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoKVPReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

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

        public ChoKVPReader<T> ClearFields()
        {
            Configuration.KVPRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoKVPReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoKVPReader<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                string fnTrim = null;
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }
                fnTrim = fieldName.NTrim();
                if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.KVPRecordFieldConfigurations.Remove(Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoKVPReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoKVPReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoKVPRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;

                    if (!_clearFields)
                    {
                        ClearFields();
                        Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                        Configuration.ColumnOrderStrict = true;
                    }

                    fnTrim = fn.NTrim();
                    if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.KVPRecordFieldConfigurations.Remove(fc);
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoKVPRecordFieldConfiguration(fnTrim) { FieldName = fn };
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.KVPRecordFieldConfigurations.Add(nfc);
                }

            }

            return this;
        }

        public ChoKVPReader<T> WithField<TField>(Expression<Func<T, TField>> field, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoKVPReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null)
        {
            return WithField(name, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter,
                defaultValue, fallbackValue, altFieldNames, null, formatText, nullValue);
        }

        private ChoKVPReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }
                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;

                string fnTrim = name.NTrim();
                ChoKVPRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (Configuration.KVPRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = Configuration.KVPRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    Configuration.KVPRecordFieldConfigurations.Remove(fc);
                }
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoKVPRecordFieldConfiguration(fnTrim)
                {
                    FieldType = fieldType,
                    QuoteField = quoteField,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    NullValue = nullValue
                };
                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : fullyQualifiedMemberName;
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName);
                    nfc.PropertyDescriptorInternal = pd;
                    nfc.DeclaringMemberInternal = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                Configuration.KVPRecordFieldConfigurations.Add(nfc);
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

        ~ChoKVPReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoKVPReader : ChoKVPReader<dynamic>
    {
        public ChoKVPReader(StringBuilder sb, ChoKVPRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }

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

        public static IEnumerable<dynamic> DeserializeText(string inputText, Encoding encoding = null, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoKVPReader<dynamic>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoKVPReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoKVPReader<dynamic>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoKVPReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoKVPReader<dynamic>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoKVPReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoKVPReader<dynamic>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoKVPRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoKVPReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
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
        bool HasCustomKVPSubscribed { get; }
    }

}
