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
    public class ChoCSVReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSanitizableReader, IChoMultiLineHeaderReader
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
        public event EventHandler<ChoMapColumnEventArgs> MapColumn;
        public event EventHandler<ChoEmptyLineEventArgs> EmptyLineFound;
        public event EventHandler<ChoSanitizeLineEventArgs> SanitizeLine;
        public event EventHandler<ChoMultiLineHeaderEventArgs> MultiLineHeader;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVReader(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

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

            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoCSVReader(TextReader textReader, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = new Lazy<TextReader>(() => textReader);
        }

        internal ChoCSVReader(IEnumerable<string> lines, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(lines, "Lines");

            Configuration = configuration;
            Init();

            _lines = lines;
        }

        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
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
                        return new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
            //_closeStreamOnDispose = true;
        }

        public ChoCSVReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoCSVReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => textReader);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoCSVReader<T> Load(Stream inStream)
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
                        return new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
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
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());

            var recordType = typeof(T).ResolveRecordType();
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(recordType);
            else
                Configuration.RecordType = recordType;
            Configuration.IsDynamicObject = Configuration.RecordType.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        public static ChoCSVReader<T> LoadText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoCSVReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoCSVReader<T> LoadText(string inputText, ChoCSVRecordConfiguration config, TraceSwitch traceSwitch = null)
        {
            return LoadText(inputText, null, config, traceSwitch);
        }

        public static ChoCSVReader<T> LoadLines(IEnumerable<string> inputLines, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoCSVReader<T>(inputLines, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null, ChoReader parent = null)
        {
            ChoCSVRecordReader rr = new ChoCSVRecordReader(recType, configuration);
            rr.Reader = parent;
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
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
            var e = _lines != null ? rr.AsEnumerable(_lines).GetEnumerator() : rr.AsEnumerable(_textReader.Value).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
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
            ChoCSVRecordReader rr = new ChoCSVRecordReader(typeof(T), Configuration);
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

        public override bool HasMapColumnSubscribed
        {
            get { return false; }
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

        public override bool RaiseReportEmptyLine(long lineNo)
        {
            EventHandler<ChoEmptyLineEventArgs> emptyLineFound = EmptyLineFound;
            if (emptyLineFound == null)
            {
                if (Configuration.IgnoreEmptyLine)
                    return true;
                else
                    return false;

                //throw new ChoParserException("Empty line found at [{0}] location.".FormatString(lineNo));
            }

            var ea = new ChoEmptyLineEventArgs(lineNo);
            emptyLineFound(this, ea);
            return ea.Continue;
        }

        public string RaiseSanitizeLine(long lineNo, string line)
        {
            EventHandler<ChoSanitizeLineEventArgs> sanitizeLine = SanitizeLine;
            if (sanitizeLine == null)
                return line;

            var ea = new ChoSanitizeLineEventArgs(lineNo, line);
            sanitizeLine(this, ea);
            return ea.Line;
        }

        public bool RaiseMultiLineHeader(long lineNo, string line)
        {
            EventHandler<ChoMultiLineHeaderEventArgs> multiLineHeader = MultiLineHeader;
            if (multiLineHeader == null)
                return false;

            var ea = new ChoMultiLineHeaderEventArgs(lineNo, line);
            multiLineHeader(this, ea);
            return ea.IsHeader;
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
            foreach (var fn in Configuration.CSVRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null || columnMappings.Count == 0)
                columnMappings = Configuration.CSVRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.CSVRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoCSVReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoCSVReader<T> AutoDetectDelimiter(bool flag = true)
        {
            Configuration.AutoDetectDelimiter = flag;
            return this;
        }

        public ChoCSVReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoCSVReader<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoCSVReader<T> AutoIncrementDuplicateColumnNames(bool allColumns = false, bool flag = true)
        {
            Configuration.AutoIncrementDuplicateColumnNames = flag;
            Configuration.AutoIncrementAllDuplicateColumnNames = allColumns;
            return this;
        }

        public ChoCSVReader<T> AutoIncrementDuplicateColumnNames(int startIndex, bool allColumns = false)
        {
            Configuration.AutoIncrementDuplicateColumnNames = true;
            Configuration.AutoIncrementStartIndex = startIndex;
            Configuration.AutoIncrementAllDuplicateColumnNames = allColumns;
            return this;
        }

        public ChoCSVReader<T> AutoArrayDiscovery(bool flag = true)
        {
            Configuration.AutoArrayDiscovery = flag;
            return this;
        }

        public ChoCSVReader<T> ArrayIndexSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid array index separator passed.");

            Configuration.ArrayIndexSeparator = value;
            Configuration.AllowNestedArrayConversion = true;
            return this;
        }

        public ChoCSVReader<T> NestedColumnSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid nested column separator passed.");

            Configuration.NestedColumnSeparator = value;
            return this;
        }

        public ChoCSVReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoCSVReader<T> WithCustomTextSelector(Func<string, string> textSelector)
        {
            Configuration.CustomTextSelecter = textSelector;
            return this;
        }

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

        public ChoCSVReader<T> HasExcelSeparator(bool? value)
        {
            Configuration.HasExcelSeparator = value;
            return this;
        }

        public ChoCSVReader<T> WithEOLDelimiter(string delimiter)
        {
            Configuration.EOLDelimiter = delimiter;
            return this;
        }

        public ChoCSVReader<T> MayContainEOLInData(bool value)
        {
            Configuration.MayContainEOLInData = value;
            return this;
        }

        public ChoCSVReader<T> IgnoreHeader()
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = true;

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

        public ChoCSVReader<T> ConfigureHeader(Action<ChoCSVFileHeaderConfiguration> action)
        {
            if (action != null)
                action(Configuration.FileHeaderConfiguration);

            return this;
        }

        public ChoCSVReader<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoCSVReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoCSVReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoCSVReader<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                string fnTrim = null;
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }
                fnTrim = fieldName.NTrim();
                if (Configuration.CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.CSVRecordFieldConfigurations.Remove(Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoCSVReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoCSVReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                PropertyDescriptor pd = null;
                ChoCSVRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        ClearFields();
                        Configuration.MapRecordFields(Configuration.RecordType);
                        //Configuration.ColumnOrderStrict = true;
                    }

                    fnTrim = fn.NTrim();
                    if (Configuration.CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.CSVRecordFieldConfigurations.Remove(Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoCSVRecordFieldConfiguration(fnTrim, ++maxFieldPos) { FieldName = fn };
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoCSVReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, int? position,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoCSVRecordFieldConfigurationMap<T>> setup)
        {
            Configuration.Map(field, setup);
            return this;
        }

        public ChoCSVReader<T> WithField(string name, Action<ChoCSVRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }

                Configuration.Map(name, mapper);
            }
            return this;
        }

        public ChoCSVReader<T> WithField<TField>(Expression<Func<T, TField>> field, 
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVReader<T> WithField<TField>(Expression<Func<T, TField>> field, int? position, 
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null,
            bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, 
                valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null, 
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            return WithField(name, null, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter, 
                valueSelector, headerSelector,
                defaultValue, fallbackValue, altFieldNames, formatText, optional, nullValue, excelField, subRecordType);
        }

        public ChoCSVReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null, 
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            return WithField(name, position, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter, valueSelector, headerSelector,
                defaultValue, fallbackValue, altFieldNames, null, formatText, optional, nullValue, excelField, subRecordType);
        }

        private ChoCSVReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null, 
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, 
            string fullyQualifiedMemberName = null, string formatText = null, bool optional = false,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }

                Configuration.WithField(name, position, fieldType, quoteField, fieldValueTrimOption, fieldName,
                    valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames, fullyQualifiedMemberName, formatText, optional,
                    nullValue, excelField, typeof(T), subRecordType);
            }

            return this;
        }

        public ChoCSVReader<T> Index<TField>(Expression<Func<T, TField>> field, int minumum, int maximum)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordType);
            }

            Configuration.IndexMap(field, minumum, maximum, null);
            return this;
        }

        public ChoCSVReader<T> DictionaryKeys<TField>(Expression<Func<T, TField>> field, params string[] keys)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordType);
            }

            Configuration.DictionaryMap(field, keys, null);
            return this;
        }

        public ChoCSVReader<T> IgnoreEmptyLine(bool flag = true)
        {
            Configuration.IgnoreEmptyLine = flag;
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

        public ChoCSVReader<T> ThrowAndStopOnMissingCSVColumn(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingCSVColumn = flag;
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

        public ChoCSVReader<T> MapRecordFields<TClass>()
        {
            MapRecordFields(typeof(TClass));
            return this;
        }

        public ChoCSVReader<T> MapRecordFields(params Type[] recordTypes)
        {
            Configuration.RecordTypeMapped = true;
            if (recordTypes != null)
            {
                foreach (var t in recordTypes)
                {
                    if (t == null)
                        continue;

                    //if (!typeof(T).IsAssignableFrom(t))
                    //	throw new ChoParserException("Incompatible [{0}] record type passed.".FormatString(t.FullName));

                    Configuration.RecordTypeConfiguration.RegisterType(t);
                }
            }

            Configuration.MapRecordFields(recordTypes);
            return this;
        }

        public ChoCSVReader<T> WithCustomRecordTypeCodeExtractor(Func<string, string> recordTypeCodeExtractor)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordTypeCodeExtractor = recordTypeCodeExtractor;
            return this;
        }

        public ChoCSVReader<T> WithCustomRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        public ChoCSVReader<T> WithRecordSelector(int fieldPosition, Type defaultRecordType = null, params Type[] recordTypes)
        {
            Configuration.SupportsMultiRecordTypes = true;

            Configuration.RecordTypeConfiguration.Position = fieldPosition;
            if (defaultRecordType != null && !typeof(T).IsAssignableFrom(defaultRecordType))
                throw new ChoParserException("Incompatible [{0}] record type passed.".FormatString(defaultRecordType.FullName));
            Configuration.RecordTypeConfiguration.DefaultRecordType = defaultRecordType;

            if (recordTypes != null)
            {
                foreach (var t in recordTypes)
                {
                    if (t == null)
                        continue;

                    //if (!typeof(T).IsDynamicType() && !typeof(T).IsAssignableFrom(t))
                    //	throw new ChoParserException("Incompatible [{0}] record type passed.".FormatString(t.FullName));

                    Configuration.RecordTypeConfiguration.RegisterType(t);
                }
            }

            //Configuration.RecordTypeMapped = true;
            //Configuration.MapRecordFields(ChoArray.Combine<Type>(new Type[] { defaultRecordType }, recordTypes));
            return this;
        }

        public ChoCSVReader<T> WithMaxScanRows(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoCSVReader<T> WithComments(params string[] comments)
        {
            Configuration.Comments = comments;
            return this;
        }

        public ChoCSVReader<T> HeaderLineAt(long value, bool ignoreHeader = false)
        {
            Configuration.FileHeaderConfiguration.HeaderLineAt = value;
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = ignoreHeader;
            return this;
        }

        public ChoCSVReader<T> IgnoreCase(bool value)
        {
            Configuration.FileHeaderConfiguration.IgnoreCase = value;
            return this;
        }

        #endregion Fluent API

        ~ChoCSVReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoCSVReader : ChoCSVReader<dynamic>
    {
        public ChoCSVReader(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }

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

        public static IEnumerable<dynamic> DeserializeText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoCSVReader<dynamic>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoCSVReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoCSVReader<dynamic>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoCSVReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoCSVReader<dynamic>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoCSVReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoCSVReader<dynamic>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoCSVReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.ToArray();
        }
    }

    internal interface IChoSanitizableReader
    {
        string RaiseSanitizeLine(long lineNo, string line);
    }

    internal interface IChoMultiLineHeaderReader
    {
        bool RaiseMultiLineHeader(long lineNo, string line);
    }
}
