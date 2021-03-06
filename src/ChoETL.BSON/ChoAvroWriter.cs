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

namespace ChoETL
{
    public class ChoAvroWriter<T> : ChoWriter, IDisposable
    {
        private Lazy<StreamWriter> _streamWriter;
        private IAvroWriter<T> _avroWriter;
        private bool _closeStreamOnDispose = false;
        private ChoAvroRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoBSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoAvroWriter(ChoBSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoAvroWriter(string filePath, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new Lazy<StreamWriter>(() => new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoAvroWriter(StreamWriter streamWriter, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = new Lazy<StreamWriter>(() => streamWriter);
        }

        public ChoAvroWriter(IAvroWriter<T> avroWriter, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(avroWriter, "AvroWriter");

            Configuration = configuration;
            Init();

            _avroWriter = avroWriter;
        }

        public ChoAvroWriter(Stream inStream, ChoBSONRecordConfiguration configuration = null)
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

            if (_streamWriter != null)
                _writer.Dispose(_streamWriter.Value);

            _isDisposed = true;
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

            if (_avroWriter != null)
                _avroWriter.Dispose();

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoBSONRecordConfiguration(recordType);

            _writer = new ChoAvroRecordWriter(recordType, Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            if (_streamWriter != null)
                _avroWriter = Create(_streamWriter.Value);

            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo<T>(_avroWriter, records.OfType<object>()).Loop();
        }

        private IAvroWriter<T> Create(StreamReader sr)
        {
            if (Configuration != null)
            {
                if (Configuration.Codec != null)
                    return AvroContainer.CreateWriter<T>(sr.BaseStream, Configuration.JsonSerializerSettings, Configuration.Codec);
                else
                    return AvroContainer.CreateWriter<T>(sr.BaseStream, Configuration.JsonSerializerSettings, Codec.Null);
            }
            else
                return AvroContainer.CreateWriter<T>(sr.BaseStream, Codec.Null);
        }

        private IAvroWriter<T> Create(StreamWriter sw)
        {
            if (Configuration != null)
            {
                if (Configuration.CodecFactory != null)
                    return AvroContainer.CreateWriter<T>(sw.BaseStream, Configuration.JsonSerializerSettings, Configuration.Codec);
                else
                    return AvroContainer.CreateWriter<T>(sw.BaseStream, Configuration.JsonSerializerSettings, Codec.Deflate);
            }
            else
                return AvroContainer.CreateWriter<T>(sw.BaseStream, Codec.Deflate);
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

            if (_streamWriter != null)
                _avroWriter = Create(_streamWriter.Value);

            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            if (record is ArrayList)
            {
                _writer.WriteTo<T>(_avroWriter, ((IEnumerable)record).AsTypedEnumerable<object>()).Loop();
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                _writer.WriteTo<T>(_avroWriter, ((IEnumerable)record).AsTypedEnumerable<object>()).Loop();
            }
            else
                _writer.WriteTo<T>(_avroWriter, new object[] { record }).Loop();
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

        public ChoAvroWriter<T> AvroSerializerSettings(Action<AvroSerializerSettings> action)
        {
            action?.Invoke(Configuration.JsonSerializerSettings);
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
                    Configuration.MapRecordFields(Configuration.RecordType);
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
                ChoBSONRecordFieldConfiguration fc = null;
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
                    if (Configuration.AvroRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.AvroRecordFieldConfigurations.Remove(Configuration.AvroRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoBSONRecordFieldConfiguration(fnTrim) { FieldName = fn };
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
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

        public ChoAvroWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoBSONRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoAvroWriter<T> WithField(string name, Action<ChoBSONRecordFieldConfigurationMap> mapper)
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
                    Configuration.MapRecordFields(Configuration.RecordType);
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

        public ChoAvroWriter<T> Configure(Action<ChoBSONRecordConfiguration> action)
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
            throw new NotSupportedException();

            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

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

                    Configuration.AvroRecordFieldConfigurations.Add(new ChoBSONRecordFieldConfiguration(colName) { FieldType = colType });
                }
            }

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.AvroRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, dr[fc.Name]);
                }

                Write(expando);
            }
        }

        public void Write(DataTable dt)
        {
            throw new NotSupportedException();
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            DataTable schemaTable = dt;
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

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

                    Configuration.AvroRecordFieldConfigurations.Add(new ChoBSONRecordFieldConfiguration(colName) { FieldType = colType });
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.AvroRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
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

    public static class ChoAvroWriter
    {
        public static byte[] SerializeAll<T>(IEnumerable<T> records, ChoBSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
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

        public static byte[] Serialize<T>(T record, ChoBSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
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
