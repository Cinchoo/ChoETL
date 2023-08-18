using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
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

namespace ChoETL
{
    public class ChoAvroWriter<T> : ChoWriter, IDisposable
    {
        private Lazy<StreamWriter> _streamWriter;
        private object _avroWriter;
        private bool _closeStreamOnDispose = false;
        private ChoAvroRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;
        internal object AvroSerializer = null;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoAvroRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoAvroWriter(ChoAvroRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoAvroWriter(string filePath, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new Lazy<StreamWriter>(() => new StreamWriter(filePath, Configuration.Append, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoAvroWriter(IAvroWriter<T> avroWriter, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(avroWriter, "AvroWriter");

            Configuration = configuration;
            Init();

            _avroWriter = avroWriter;
        }

        protected ChoAvroWriter(IAvroWriter<Dictionary<string, object>> avroWriter, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(avroWriter, "AvroWriter");

            Configuration = configuration;
            Init();

            _avroWriter = avroWriter;
        }

        public ChoAvroWriter(StreamWriter streamWriter, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = new Lazy<StreamWriter>(() => streamWriter);
        }

        public ChoAvroWriter(Stream inStream, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _streamWriter = new Lazy<StreamWriter>(() => new StreamWriter(inStream));
            else
                _streamWriter = new Lazy<StreamWriter>(() => new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize));
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
            if (_streamWriter != null)
                _streamWriter.Value.Flush();
        }

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            if (_writer != null)
                _writer.Dispose<T>();

            _isDisposed = true;
            try
            {
                if (_closeStreamOnDispose)
                {
                    if (_streamWriter != null)
                        _streamWriter.Value.Dispose();
                }
                else
                {
                    if (_streamWriter != null)
                        _streamWriter.Value.Flush();
                }
            }
            catch { }
            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoAvroRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            _writer = new ChoAvroRecordWriter(recordType, Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;

            if (_avroWriter != null)
            {
                if (typeof(T) == typeof(object))
                    _writer.WriteTo<Dictionary<string, object>>(_avroWriter, records.OfType<object>()).Loop();
                else
                    _writer.WriteTo<T>(_avroWriter, records.OfType<object>()).Loop();
            }
            else
            {
                if (typeof(T) == typeof(object))
                    _writer.WriteTo<Dictionary<string, object>>(_streamWriter.Value, records.OfType<object>()).Loop();
                else
                    _writer.WriteTo<T>(_streamWriter.Value, records.OfType<object>()).Loop();
            }
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

            if (typeof(T) == typeof(object))
                _writer.WriteTo<Dictionary<string, object>>(_streamWriter.Value, new object[] { record }).Loop();
            else
                _writer.WriteTo<T>(_streamWriter.Value, new object[] { record }).Loop();
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

        public ChoAvroWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoAvroWriter<T> AvroSerializerSettings(Action<AvroSerializerSettings> action)
        {
            action?.Invoke(Configuration.AvroSerializerSettings);
            return this;
        }

        public ChoAvroWriter<T> KnownTypes(params Type[] types)
        {
            if (types != null)
                Configuration.KnownTypes = types.ToList();

            return this;
        }

        public ChoAvroWriter<T> UseAvroSerializer(bool flag = true)
        {
            Configuration.UseAvroSerializer = flag;
            return this;
        }

        public virtual ChoAvroWriter<T> WithAvroSerializer(IAvroSerializer<T> avroSerializer)
        {
            AvroSerializer = avroSerializer;
            Configuration.UseAvroSerializer = true;
            return this;
        }

        public ChoAvroWriter<T> WithRecordSchema(string schema)
        {
            Configuration.RecordSchema = schema;
            return this;
        }

        public ChoAvroWriter<T> WithCode(Codec codec)
        {
            Configuration.Codec = codec;
            return this;
        }

        public ChoAvroWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoAvroWriter<T> NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoAvroWriter<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoAvroWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoAvroWriter<T> IgnoreField(string fieldName)
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
                if (Configuration.AvroRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.AvroRecordFieldConfigurations.Remove(Configuration.AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoAvroWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field, (string)null);
            }
            return this;
        }

        public ChoAvroWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoAvroRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        ClearFields();
                        Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                    }

                    fnTrim = fn.NTrim();
                    if (Configuration.AvroRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.AvroRecordFieldConfigurations.Remove(Configuration.AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoAvroRecordFieldConfiguration(fnTrim) { FieldName = fn };
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.AvroRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoAvroWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoAvroRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoAvroWriter<T> WithField(string name, Action<ChoAvroRecordFieldConfigurationMap> mapper)
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

        public ChoAvroWriter<T> WithField<TField>(Expression<Func<T, TField>> field, string fieldName)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null);
        }

        public ChoAvroWriter<T> WithField(string name)
        {
            return WithField(name, null);
        }

        private ChoAvroWriter<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null, char? fillChar = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            bool? truncate = null, string fieldName = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string fullyQualifiedMemberName = null, string formatText = null, string nullValue = null,
            Type subRecordType = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }

                Configuration.WithField(name, position, fieldType, quoteField, null, fieldName,
                    valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, null, fullyQualifiedMemberName, formatText,
                    nullValue, typeof(T), subRecordType, fieldValueJustification);
            }

            return this;
        }

        public ChoAvroWriter<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoAvroWriter<T> Configure(Action<ChoAvroRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoAvroWriter<T> Setup(Action<ChoAvroWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoAvroWriter<T> MapRecordFields<TClass>()
        {
            Configuration.MapRecordFields<TClass>();
            return this;
        }

        public ChoAvroWriter<T> MapRecordFields(Type recordType)
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

            Configuration.UseNestedKeyFormat = false;

            if (Configuration.AvroRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    Configuration.AvroRecordFieldConfigurations.Add(new ChoAvroRecordFieldConfiguration(colName) { FieldType = colType });
                }
            }

            var ordinals = Configuration.AvroRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
            while (dr.Read())
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in ordinals)
                {
                    expandoDic.Add(fc.Key, fc.Value == -1 ? null : dr[fc.Value]);
                }

                if (Configuration.IsDynamicObjectInternal)
                    Write(expando);
                else
                {
                    Write((T)ChoObjectEx.ConvertToObject<T>(expando));
                }
            }
        }

        public void Write(DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            DataTable schemaTable = dt;

            int ordinal = 0;
            if (Configuration.AvroRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    Configuration.AvroRecordFieldConfigurations.Add(new ChoAvroRecordFieldConfiguration(colName) { FieldType = colType });
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in Configuration.AvroRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                if (Configuration.IsDynamicObjectInternal)
                    Write(expando);
                else
                {
                    Write((T)ChoObjectEx.ConvertToObject<T>(expando));
                }
            }
        }

        ~ChoAvroWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }

    }

    public class ChoAvroWriter : ChoAvroWriter<dynamic>
    {
        public ChoAvroWriter(ChoAvroRecordConfiguration configuration = null)
    : base(configuration)
        {

        }
        public ChoAvroWriter(string filePath, ChoAvroRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {
        }

        public ChoAvroWriter(IAvroWriter<Dictionary<string, object>> avroWriter, ChoAvroRecordConfiguration configuration = null)
            : base(avroWriter, configuration)
        {
        }

        public ChoAvroWriter(StreamWriter streamWriter, ChoAvroRecordConfiguration configuration = null)
            : base(streamWriter, configuration)
        {

        }
        public ChoAvroWriter(Stream inStream, ChoAvroRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        #region Fluent API

        public override ChoAvroWriter<dynamic> WithAvroSerializer(IAvroSerializer<dynamic> avroSerializer)
        {
            throw new NotSupportedException("Use WithAvroSerializer(IAvroSerializer<Dictionary<string, object>> avroSerializer) instead.");
        }

        public ChoAvroWriter<dynamic> WithAvroSerializer(IAvroSerializer<Dictionary<string, object>> avroSerializer)
        {
            AvroSerializer = avroSerializer;
            Configuration.UseAvroSerializer = true;
            return this;
        }

        #endregion Fluent API

        public static byte[] SerializeAll(IEnumerable<dynamic> records, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoAvroWriter(writer))
                {
                    w.Write(records);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] SerializeAll<T>(IEnumerable<T> records, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoAvroWriter<T>(writer))
                {
                    w.Write(records);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] Serialize(dynamic record, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoAvroWriter(writer))
                {
                    w.Write(record);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] Serialize<T>(T record, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoAvroWriter<T>(writer))
                {
                    w.Write(record);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
}
