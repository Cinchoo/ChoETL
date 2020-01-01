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
using System.Xml;

namespace ChoETL
{
    public class ChoJSONWriter<T> : ChoWriter, IChoSerializableWriter, IDisposable
        where T : class
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoJSONRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }
        
        public ChoJSONWriter(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoJSONWriter(ChoJSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoJSONWriter(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoJSONWriter(TextWriter textWriter, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoJSONWriter(Stream inStream, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            if (inStream is MemoryStream)
                _textWriter = new StreamWriter(inStream);
            else
                _textWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            //_closeStreamOnDispose = true;
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
            if (_writer != null)
                _writer.EndWrite(_textWriter);

            if (_closeStreamOnDispose)
            {
                if (_textWriter != null)
                    _textWriter.Dispose();
            }
            else
            {
                if (_textWriter != null)
                    _textWriter.Flush();
            }

            if (!finalize)
                GC.SuppressFinalize(this);
        }
        public void Close()
        {
            Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(typeof(T));

            _writer = new ChoJSONRecordWriter(typeof(T), Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo(_textWriter, records).Loop();
        }

        public void Write(T record)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            if (record != null && !record.GetType().IsSimple() && !record.GetType().IsDynamicType() && record is IList)
            {
                if (record is ArrayList)
                    _writer.ElementType = typeof(object);

                _writer.WriteTo(_textWriter, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop();
            }
            else if (record != null && (!record.GetType().IsDynamicType() && record is IDictionary))
            {
                if (Configuration.SingleElement == null)
                    Configuration.SingleElement = true;
                _writer.WriteTo(_textWriter, new T[] { record }).Loop();
            }
            else
            {
                if (Configuration.SingleElement == null) Configuration.SingleElement = true;
                _writer.WriteTo(_textWriter, new T[] { record }).Loop();
            }
        }

        public static string ToText<TRec>(TRec record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string jsonPath = null)
            where TRec : class
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration();

            configuration.IgnoreRootName = true;
            configuration.RootName = null;
            if (configuration.SingleElement == null) configuration.SingleElement = true;
            configuration.SupportMultipleContent = true;

            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch, jsonPath);
        }


        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string jsonPath = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Configuration.JSONPath = jsonPath;

                parser.Write(records);
                parser.Close();
                writer.Flush();
                
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        internal static string ToText(object rec, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            ChoJSONRecordWriter writer = new ChoJSONRecordWriter(rec.GetType(), configuration);
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

        public ChoJSONWriter<T> SupportMultipleContent(bool value = false)
        {
            Configuration.SupportMultipleContent = value;
            return this;
        }

        public ChoJSONWriter<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONWriter<T> ClearFields()
        {
            Configuration.JSONRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoJSONWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (field != null)
                return IgnoreField(field.GetFullyQualifiedMemberName());
            else
                return this;
        }

        public ChoJSONWriter<T> IgnoreField(string fieldName)
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
                if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoJSONWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> setup)
        {
            Configuration.MapRecordField(field.GetMemberName(), setup);
            return this;
        }

        public ChoJSONWriter<T> WithField(string name, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
                Configuration.MapRecordField(name, mapper);
            return this;
        }

        public ChoJSONWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoJSONWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoJSONRecordFieldConfiguration fc = null;
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
                    if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, (string)null);
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.JSONRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoJSONWriter<T> WithField<TField>(Expression<Func<T, TField>> field, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), field.GetPropertyType(), fieldValueTrimOption, fieldName, valueConverter,
                defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoJSONWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, string nullValue = null)
        {
            return WithField(name, fieldType, fieldValueTrimOption, fieldName, valueConverter,
                defaultValue, fallbackValue, null, formatText, nullValue);
        }

        private ChoJSONWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }

                string fnTrim = name.NTrim();
                ChoJSONRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    Configuration.JSONRecordFieldConfigurations.Remove(fc);
                }
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, (string)null)
                {
                    FieldType = fieldType,
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

                Configuration.JSONRecordFieldConfigurations.Add(nfc);
            }

            return this;
        }

        public ChoJSONWriter<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFields();
            Configuration.MapRecordFields(Configuration.RecordType);
            return this;
        }

        public ChoJSONWriter<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoJSONWriter<T> Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoJSONWriter<T> Setup(Action<ChoJSONWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoJSONWriter<T> MapRecordFields<T1>()
        {
            Configuration.MapRecordFields<T1>();
            return this;
        }

        public ChoJSONWriter<T> MapRecordFields(Type recordType)
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

            //int ordinal = 0;
            if (Configuration.JSONRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoJSONRecordFieldConfiguration(colName, jsonPath: null);
                    Configuration.JSONRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.JSONRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, dr[fc.Name]);
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

            if (Configuration.JSONRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoJSONRecordFieldConfiguration(colName, jsonPath: null);
                    Configuration.JSONRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }
            Configuration.RootName = dt.TableName.IsNullOrWhiteSpace() ? null : dt.TableName;

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.JSONRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name] == DBNull.Value ? null : row[fc.Name]);
                }

                Write(expando);
            }
        }

        public void Write(DataSet ds)
        {
            ChoGuard.ArgumentNotNull(ds, "DataSet");

            foreach (DataTable dt in ds.Tables)
            {
                Configuration.Reset();
                Configuration.RootName = ds.DataSetName.IsNullOrWhiteSpace() ? "Root" : ds.DataSetName;
                Write(dt);
            }
        }

        ~ChoJSONWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoJSONWriter : ChoJSONWriter<dynamic>
    {
        public ChoJSONWriter(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoJSONWriter(string filePath, ChoJSONRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoJSONWriter(TextWriter textWriter, ChoJSONRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoJSONWriter(Stream inStream, ChoJSONRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToTextAll<T>(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToText<T>(record, configuration, traceSwitch);
        }
    }
}
