using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoFixedLengthWriter<T> : ChoWriter, IDisposable
        where T : class
    {
        private Lazy<TextWriter> _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoFixedLengthRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoFixedLengthRecordConfiguration Configuration
        {
            get;
            private set;
        }
        
        public ChoFixedLengthWriter(StringBuilder sb, ChoFixedLengthRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoFixedLengthWriter(ChoFixedLengthRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoFixedLengthWriter(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new Lazy<TextWriter>(() => new StreamWriter(filePath, Configuration.Append, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoFixedLengthWriter(TextWriter textWriter, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = new Lazy<TextWriter>(() => textWriter);
        }

        public ChoFixedLengthWriter(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _textWriter = new Lazy<TextWriter>(() => new StreamWriter(inStream));
            else
                _textWriter = new Lazy<TextWriter>(() => new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize));
            //_closeStreamOnDispose = true;
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public void Flush()
        {
            if (_textWriter != null)
                _textWriter.Value.Flush();
        }

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            _writer?.Dispose();

            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_textWriter != null)
                    _textWriter.Value.Dispose();
            }
            else
            {
                if (_textWriter != null)
                    _textWriter.Value.Flush();
            }

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            var recordType = typeof(T).ResolveRecordType();
            if (Configuration == null)
                Configuration = new ChoFixedLengthRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            _writer = new ChoFixedLengthRecordWriter(recordType, Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void WriteComment(string commentText, bool silent = true)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteComment(_textWriter.Value, commentText, silent);
        }

        public void WriteHeader(string header)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteHeader(_textWriter.Value, header);
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo(_textWriter.Value, records).Loop(() => ++_recordNumber);
        }

        public void Write(T record)
        {
            if (record is DataTable)
                throw new ChoParserException("Invalid data passed.");
            else if (record is IDataReader)
                throw new ChoParserException("Invalid data passed.");
            //else if (!(record is IDictionary<string, object>))
            //    throw new ChoParserException("Invalid data passed.");

            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            if (record is ArrayList)
            {
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop(() => ++_recordNumber);
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop(() => ++_recordNumber);
            }
            else
                _writer.WriteTo(_textWriter.Value, new T[] { record }).Loop(() => ++_recordNumber);
        }

        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            if (records == null) return null;

            if (typeof(DataTable).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder text = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    configuration = configuration == null ? new ChoFixedLengthRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                    using (var w = new ChoFixedLengthWriter(text, configuration))
                        w.Write(dt);
                }

                return text.ToString();
            }
            else if (typeof(IDataReader).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder text = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    configuration = configuration == null ? new ChoFixedLengthRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                    using (var w = new ChoFixedLengthWriter(text, configuration))
                        w.Write(dt);
                }

                return text.ToString();
            }

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        public static string ToText<TRec>(TRec record, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch);
        }

        internal static string ToText(object rec, ChoFixedLengthRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            if (rec is DataTable)
            {
                StringBuilder text = new StringBuilder();
                configuration = configuration == null ? new ChoFixedLengthRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                using (var w = new ChoFixedLengthWriter(text, configuration))
                    w.Write(rec as DataTable);
                return text.ToString();
            }
            else if (rec is IDataReader)
            {
                StringBuilder text = new StringBuilder();
                configuration = configuration == null ? new ChoFixedLengthRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                using (var w = new ChoFixedLengthWriter(text, configuration))
                    w.Write(rec as IDataReader);
                return text.ToString();
            }

            ChoFixedLengthRecordWriter writer = new ChoFixedLengthRecordWriter(rec.GetType(), configuration);
            writer.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var sw = new StreamWriter(stream, configuration.Encoding, configuration.BufferSize))
            {
                writer.WriteTo(sw, new object[] { rec }).Loop();
                sw.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        private void NotifyRowsWritten(object sender, ChoRowsWrittenEventArgs e)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                Console.WriteLine(e.RowsWritten.ToString("#,##0") + " records written.");
            else
                rowsWrittenEvent(this, e);
        }

        #region Fluent API

        public ChoFixedLengthWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoFixedLengthWriter<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoFixedLengthWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoFixedLengthWriter<T> WithMaxScanRows(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoFixedLengthWriter<T> NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoFixedLengthWriter<T> WithRecordLength(int length)
        {
            Configuration.RecordLength = length;
            return this;
        }

        public ChoFixedLengthWriter<T> WithFirstLineHeader()
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            return this;
        }

        public ChoFixedLengthWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoFixedLengthWriter<T> WithEOLDelimiter(string delimiter)
        {
            Configuration.EOLDelimiter = delimiter;
            return this;
        }

        public ChoFixedLengthWriter<T> ConfigureHeader(Action<ChoFixedLengthFileHeaderConfiguration> action)
        {
            if (action != null)
                action(Configuration.FileHeaderConfiguration);

            return this;
        }

        public ChoFixedLengthWriter<T> ClearFields()
        {
            Configuration.FixedLengthRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoFixedLengthWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoFixedLengthWriter<T> IgnoreField(string fieldName)
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
                if (Configuration.FixedLengthRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.FixedLengthRecordFieldConfigurations.Remove(Configuration.FixedLengthRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoFixedLengthWriter<T> WithField<TField>(Expression<Func<T, TField>> field, int startIndex, int size, Func<TField> expr)
        {
            WithField(field.GetMemberName(), startIndex, size, expr: new Func<object>(() => (object)expr()));
            return this;
        }

        public ChoFixedLengthWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoFixedLengthRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoFixedLengthWriter<T> WithField(string fieldName, int startIndex, int size, Func<object> expr)
        {
            WithField(fieldName, startIndex, size, fullyQualifiedMemberName: null, expr: expr);
            return this;
        }

        public ChoFixedLengthWriter<T> WithField(string name, Action<ChoFixedLengthRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }

                Configuration.Map(name, mapper);
            }
            return this;
        }

        public ChoFixedLengthWriter<T> WithField<TField>(Expression<Func<T, TField>> field, int startIndex, int size, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<object> expr = null,
            IChoValueConverter propertyConverter = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), startIndex, size, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification,
                    truncate, fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), 
                    formatText, nullValue, expr, propertyConverter);
        }

        public ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, Type fieldType = null, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<object> expr = null,
            IChoValueConverter propertyConverter = null)
        {
            return WithField(name, startIndex, size, fieldType, quoteField, fillChar, fieldValueJustification,
                truncate, fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, null, formatText, 
                nullValue, expr, propertyConverter);
        }

        private ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, Type fieldType = null, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string fullyQualifiedMemberName = null, string formatText = null, string nullValue = null, Func<object> expr = null,
            IChoValueConverter propertyConverter = null)
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
                ChoFixedLengthRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (Configuration.FixedLengthRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = Configuration.FixedLengthRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    Configuration.FixedLengthRecordFieldConfigurations.Remove(fc);
                }
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoFixedLengthRecordFieldConfiguration(name.Trim(), startIndex, size)
                {
                    FieldType = fieldType,
                    QuoteField = quoteField,
                    FillChar = fillChar,
                    FieldValueJustification = fieldValueJustification,
                    Truncate = truncate,
                    FieldName = fieldName.IsNullOrWhiteSpace() ? name : fieldName,
                    ValueConverter = valueConverter,
                    ValueSelector = valueSelector,
                    HeaderSelector = headerSelector,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    NullValue = nullValue,
                    Expr = expr,
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
                if (propertyConverter != null)
                    nfc.AddConverter(propertyConverter);

                Configuration.FixedLengthRecordFieldConfigurations.Add(nfc);
            }

            return this;
        }

        public ChoFixedLengthWriter<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoFixedLengthWriter<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoFixedLengthWriter<T> Configure(Action<ChoFixedLengthRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoFixedLengthWriter<T> Setup(Action<ChoFixedLengthWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoFixedLengthWriter<T> MapRecordFields<T1>()
        {
            Configuration.MapRecordFields<T1>();
            return this;
        }

        public ChoFixedLengthWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
                Configuration.MapRecordFields(recordType);

            return this;
        }

        #endregion Fluent API

        public static void Write(StringBuilder sb, IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            using (var w = new ChoFixedLengthWriter(sb).WithFirstLineHeader())
            {
                Write(w, dr);
            }
        }

        public static void Write(string filePath, IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            using (var w = new ChoFixedLengthWriter(filePath).WithFirstLineHeader())
            {
                Write(w, dr);
            }
        }

        public static void Write(TextWriter textWriter, IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            using (var w = new ChoFixedLengthWriter(textWriter).WithFirstLineHeader())
            {
                Write(w, dr);
            }
        }

        public static void Write(Stream inStream, IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            using (var w = new ChoFixedLengthWriter(inStream).WithFirstLineHeader())
            {
                Write(w, dr);
            }
        }

        public static void Write(ChoFixedLengthWriter<dynamic> w, IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(w, "Writer");
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();

            w.Configuration.UseNestedKeyFormat = false;
            w.Configuration.FixedLengthRecordFieldConfigurations.Clear();

            //int ordinal = 0;
            if (w.Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                int startIndex = 0;
                int fieldLength = 0;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(colType);
                    var obj = new ChoFixedLengthRecordFieldConfiguration(colName, startIndex, fieldLength);
                    w.Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            var ordinals = w.Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
            while (dr.Read())
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in ordinals)
                {
                    expandoDic.Add(fc.Key, fc.Value == -1 ? null : dr[fc.Value]);
                }

                w.Write(expando);
            }
        }
        public static void Write(StringBuilder sb, DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            using (var w = new ChoFixedLengthWriter(sb).WithFirstLineHeader())
            {
                Write(w, dt);
            }
        }

        public static void Write(string filePath, DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            using (var w = new ChoFixedLengthWriter(filePath).WithFirstLineHeader())
            {
                Write(w, dt);
            }
        }

        public static void Write(TextWriter textWriter, DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            using (var w = new ChoFixedLengthWriter(textWriter).WithFirstLineHeader())
            {
                Write(w, dt);
            }
        }

        public static void Write(Stream inStream, DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            using (var w = new ChoFixedLengthWriter(inStream).WithFirstLineHeader())
            {
                Write(w, dt);
            }
        }

        public static void Write(ChoFixedLengthWriter<dynamic> w, DataTable dt)
        {
            ChoGuard.ArgumentNotNull(w, "Writer");
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            DataTable schemaTable = dt;

            w.Configuration.UseNestedKeyFormat = false;

            if (w.Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                int startIndex = 0;
                int fieldLength = 0;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(colType);
                    var obj = new ChoFixedLengthRecordFieldConfiguration(colName, startIndex, fieldLength);
                    w.Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in w.Configuration.FixedLengthRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                w.Write(expando);
            }
        }

        ~ChoFixedLengthWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoFixedLengthWriter : ChoFixedLengthWriter<dynamic>
    {
        public ChoFixedLengthWriter(StringBuilder sb, ChoFixedLengthRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoFixedLengthWriter(ChoFixedLengthRecordConfiguration configuration = null)
           : base(configuration)
        {

        }
        public ChoFixedLengthWriter(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoFixedLengthWriter(TextWriter textWriter, ChoFixedLengthRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoFixedLengthWriter(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll<dynamic>(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToTextAll<T>(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText<dynamic>(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoFixedLengthRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToText<T>(record, configuration, traceSwitch);
        }
    }
}
