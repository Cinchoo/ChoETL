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

            _textWriter = new Lazy<TextWriter>(() => new StreamWriter(filePath, false, Configuration.Encoding, Configuration.BufferSize));
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

            _writer.Dispose();

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
                Configuration.RecordType = recordType;

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
            _writer.WriteTo(_textWriter.Value, records).Loop();
        }

        public void Write(T record)
        {
            if (record is DataTable)
            {
                Write(record as DataTable);
                return;
            }
            else if (record is IDataReader)
            {
                Write(record as IDataReader);
                return;
            }

            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            if (record is ArrayList)
            {
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop();
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop();
            }
            else
                _writer.WriteTo(_textWriter.Value, new T[] { record } ).Loop();
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
                    Configuration.MapRecordFields(Configuration.RecordType);
                }
                fnTrim = fieldName.NTrim();
                if (Configuration.FixedLengthRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.FixedLengthRecordFieldConfigurations.Remove(Configuration.FixedLengthRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoFixedLengthWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoFixedLengthRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoFixedLengthWriter<T> WithField(string name, Action<ChoFixedLengthRecordFieldConfigurationMap> mapper)
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

        public ChoFixedLengthWriter<T> WithField<TField>(Expression<Func<T, TField>> field, int startIndex, int size, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), startIndex, size, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification,
                    truncate, fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, Type fieldType = null, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null)
        {
            return WithField(name, startIndex, size, fieldType, quoteField, fillChar, fieldValueJustification,
                truncate, fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, null, formatText, nullValue);
        }

        private ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, Type fieldType = null, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string fullyQualifiedMemberName = null, string formatText = null, string nullValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
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
                    NullValue = nullValue
                };

                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : fullyQualifiedMemberName;
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName);
                    nfc.PropertyDescriptor = pd;
                    nfc.DeclaringMember = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

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

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            Configuration.UseNestedKeyFormat = false;

            //int ordinal = 0;
            if (Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
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
                    Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            var ordinals = Configuration.FixedLengthRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in ordinals)
                {
                    expandoDic.Add(fc.Key, fc.Value == -1 ? null : dr[fc.Value]);
                }

                Write(expando);
            }
        }

        public void Write(DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            DataTable schemaTable = dt;
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            Configuration.UseNestedKeyFormat = false;

            if (Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
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
                    Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.FixedLengthRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
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
