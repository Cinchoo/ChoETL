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
    public class ChoCSVWriter<T> : ChoWriter, IDisposable
        where T : class
    {
        private Lazy<TextWriter> _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoCSVRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
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

        public ChoCSVWriter(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoCSVWriter(ChoCSVRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new Lazy<TextWriter>(() => new StreamWriter(filePath, false, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoCSVWriter(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = new Lazy<TextWriter>(() => textWriter);
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
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
                Configuration = new ChoCSVRecordConfiguration(recordType);
            else
                Configuration.RecordType = recordType;

            _writer = new ChoCSVRecordWriter(recordType, Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void WriteComment(string commentText, bool silent = true)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteComment(_textWriter.Value, commentText, silent);
        }

        public void WriteFields(params object[] fieldValues)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteFields(_textWriter.Value, fieldValues);
        }

        public void WriteHeader(params string[] fieldNames)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteHeader(_textWriter.Value, fieldNames);
        }

        public void WriteCustomHeader(string header)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteCustomHeader(_textWriter.Value, header);
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
                _writer.WriteTo(_textWriter.Value, new T[] { record }).Loop();
        }

        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            if (records == null) return null;

            if (typeof(DataTable).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder csv = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    configuration = configuration == null ? new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                    using (var w = new ChoCSVWriter(csv, configuration))
                        w.Write(dt);
                }

                return csv.ToString();
            }
            else if (typeof(IDataReader).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder csv = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    configuration = configuration == null ? new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                    using (var w = new ChoCSVWriter(csv, configuration))
                        w.Write(dt);
                }

                return csv.ToString();
            }

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        public static string ToText<TRec>(TRec record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch);
        }

        internal static string ToText(object rec, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            if (rec is DataTable)
            {
                StringBuilder csv = new StringBuilder();
                configuration = configuration == null ? new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                using (var w = new ChoCSVWriter(csv, configuration))
                    w.Write(rec as DataTable);
                return csv.ToString();
            }
            else if (rec is IDataReader)
            {
                StringBuilder csv = new StringBuilder();
                configuration = configuration == null ? new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()) : configuration;
                using (var w = new ChoCSVWriter(csv, configuration))
                    w.Write(rec as IDataReader);
                return csv.ToString();
            }

            ChoCSVRecordWriter writer = new ChoCSVRecordWriter(rec.GetType(), configuration);
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

        public ChoCSVWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoCSVWriter<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoCSVWriter<T> ArrayIndexSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid array index separator passed.");

            Configuration.ArrayIndexSeparator = value;
            return this;
        }

        public ChoCSVWriter<T> NestedColumnSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid nested column separator passed.");

            Configuration.NestedColumnSeparator = value;
            return this;
        }

        public ChoCSVWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoCSVWriter<T> UseNestedKeyFormat(bool flag = true)
        {
            Configuration.UseNestedKeyFormat = flag;
            return this;
        }

        public ChoCSVWriter<T> WithMaxScanRows(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoCSVWriter<T> NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoCSVWriter<T> WithDelimiter(string delimiter)
        {
            Configuration.Delimiter = delimiter;
            return this;
        }

        public ChoCSVWriter<T> HasExcelSeparator(bool? value)
        {
            Configuration.HasExcelSeparator = value;
            return this;
        }

        public ChoCSVWriter<T> WithEOLDelimiter(string delimiter)
        {
            Configuration.EOLDelimiter = delimiter;
            return this;
        }

        public ChoCSVWriter<T> WithFirstLineHeader(bool ignoreHeader = false)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = ignoreHeader;
            return this;
        }

        public ChoCSVWriter<T> ConfigureHeader(Action<ChoCSVFileHeaderConfiguration> action)
        {
            if (action != null)
                action(Configuration.FileHeaderConfiguration);

            return this;
        }

        public ChoCSVWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoCSVWriter<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoCSVWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoCSVWriter<T> IgnoreField(string fieldName)
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

        public ChoCSVWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoCSVWriter<T> WithFields(params string[] fieldsNames)
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

        public ChoCSVWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, int? position, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField, field.GetReflectedType());
        }

        public ChoCSVWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoCSVRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoCSVWriter<T> WithField(string name, Action<ChoCSVRecordFieldConfigurationMap> mapper)
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

        public ChoCSVWriter<T> WithField<TField>(Expression<Func<T, TField>> field, int position, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField);
        }

        public ChoCSVWriter<T> WithField<TField>(Expression<Func<T, TField>> field, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, optional, nullValue, excelField);
        }

        public ChoCSVWriter<T> WithField(string name, Type fieldType = null, bool? quoteField = null, char? fillChar = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false)
        {
            return WithField(name, null, fieldType, quoteField, fillChar, fieldValueJustification,
                truncate, fieldName, valueConverter, valueSelector, headerSelector,
                defaultValue, fallbackValue, null, formatText, optional, nullValue, excelField);
        }

        private ChoCSVWriter<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null, char? fillChar = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            bool? truncate = null, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string fullyQualifiedMemberName = null, string formatText = null, bool optional = false, string nullValue = null,
            bool excelField = false, Type subRecordType = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }

                Configuration.WithField(name, position, fieldType, quoteField, null, fieldName,
                    valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, null, fullyQualifiedMemberName, formatText, optional,
                    nullValue, excelField, typeof(T), subRecordType, fieldValueJustification);
            }

            return this;
        }

        public ChoCSVWriter<T> Index<TField>(Expression<Func<T, TField>> field, int minumum, int maximum)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordType);
            }

            Configuration.IndexMap(field, minumum, maximum, null);
            return this;
        }

        public ChoCSVWriter<T> DictionaryKeys<TField>(Expression<Func<T, TField>> field, params string[] keys)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordType);
            }

            Configuration.DictionaryMap(field, keys, null);
            return this;
        }

        public ChoCSVWriter<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoCSVWriter<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoCSVWriter<T> Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoCSVWriter<T> Setup(Action<ChoCSVWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoCSVWriter<T> MapRecordFields<TClass>()
        {
            Configuration.MapRecordFields<TClass>();
            return this;
        }

        public ChoCSVWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
                Configuration.MapRecordFields(recordType);

            return this;
        }

        public ChoCSVWriter<T> WithComments(params string[] comments)
        {
            Configuration.Comments = comments;
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

            int ordinal = 0;
            if (Configuration.CSVRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            var ordinals = Configuration.CSVRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
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

            int ordinal = 0;
            if (Configuration.CSVRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.CSVRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
            }
        }

        ~ChoCSVWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }

    }

    public class ChoCSVWriter : ChoCSVWriter<dynamic>
    {
        public ChoCSVWriter(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoCSVWriter(ChoCSVRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {
        }
        public ChoCSVWriter(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll<dynamic>(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToTextAll<T>(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText<dynamic>(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToText<T>(record, configuration, traceSwitch);
        }
    }
}
