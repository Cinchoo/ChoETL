using Parquet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Linq.Expressions;

namespace ChoETL
{
    public class ChoParquetWriter<T> : ChoWriter, IDisposable
    {
        private Lazy<StreamWriter> _streamWriter;
        private bool _closeStreamOnDispose = false;
        private ChoParquetRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoNewRowGroupEventArgs> NewRowGroup;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoParquetRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoParquetWriter(ChoParquetRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoParquetWriter(string filePath, ChoParquetRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new Lazy<StreamWriter>(() =>
            {
                return new StreamWriter(filePath, Configuration.Append, Configuration.Encoding, Configuration.BufferSize);
            });
            _closeStreamOnDispose = true;
        }

        public ChoParquetWriter(StreamWriter streamWriter, ChoParquetRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = new Lazy<StreamWriter>(() => streamWriter);
        }

        public ChoParquetWriter(Stream inStream, ChoParquetRecordConfiguration configuration = null)
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

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoParquetRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            _writer = new ChoParquetRecordWriter(recordType, Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
            _writer.NewRowGroup += OnNewRowGroup;
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.MembersDiscovered += MembersDiscovered;
            _writer.WriteTo(_streamWriter.Value, records.OfType<object>()).Loop();
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
            _writer.MembersDiscovered += MembersDiscovered;
            if (record is ArrayList)
            {
                _writer.WriteTo(_streamWriter.Value, ((IEnumerable)record).AsTypedEnumerable<object>()).Loop();
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                _writer.WriteTo(_streamWriter.Value, ((IEnumerable)record).AsTypedEnumerable<object>()).Loop();
            }
            else
                _writer.WriteTo(_streamWriter.Value, new object[] { record }).Loop();
        }

        private void NotifyRowsWritten(object sender, ChoRowsWrittenEventArgs e)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                Console.WriteLine(e.RowsWritten.ToString("#,##0") + " records written.");
            else
                rowsWrittenEvent(this, e);
        }

        private void OnNewRowGroup(object sender, ChoNewRowGroupEventArgs e)
        {
            EventHandler<ChoNewRowGroupEventArgs> newRowGroupEvent = NewRowGroup;
            if (newRowGroupEvent != null)
                newRowGroupEvent(this, e);
        }

        #region Fluent API

        public ChoParquetWriter<T> ParquetOptions(Action<ParquetOptions> action)
        {
            action?.Invoke(Configuration.ParquetOptions);
            return this;
        }

        public ChoParquetWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoParquetWriter<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoParquetWriter<T> ArrayIndexSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid array index separator passed.");

            Configuration.ArrayIndexSeparator = value;
            return this;
        }

        public ChoParquetWriter<T> NestedKeySeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid nested column separator passed.");

            Configuration.NestedKeySeparator = value;
            return this;
        }

        public ChoParquetWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoParquetWriter<T> UseNestedKeyFormat(bool flag = true)
        {
            Configuration.UseNestedKeyFormat = flag;
            return this;
        }

        public ChoParquetWriter<T> WithMaxScanRows(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoParquetWriter<T> NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoParquetWriter<T> OnRowsWritten(Action<object, ChoRowsWrittenEventArgs> rowsWritten)
        {
            RowsWritten += (o, e) => rowsWritten(o, e);
            return this;
        }

        //public ChoParquetWriter<T> WithEOLDelimiter(string delimiter)
        //{
        //    Configuration.EOLDelimiter = delimiter;
        //    return this;
        //}

        //public ChoParquetWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        //{
        //    Configuration.QuoteAllFields = flag;
        //    Configuration.QuoteChar = quoteChar;
        //    return this;
        //}

        public ChoParquetWriter<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoParquetWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoParquetWriter<T> IgnoreField(string fieldName)
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
                if (Configuration.ParquetRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.ParquetRecordFieldConfigurations.Remove(Configuration.ParquetRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoParquetWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoParquetWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = Configuration.ParquetRecordFieldConfigurations.Count > 0 ? Configuration.ParquetRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                PropertyDescriptor pd = null;
                ChoParquetRecordFieldConfiguration fc = null;
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
                    if (Configuration.ParquetRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.ParquetRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.ParquetRecordFieldConfigurations.Remove(Configuration.ParquetRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoParquetRecordFieldConfiguration(fnTrim, ++maxFieldPos) { FieldName = fn };
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.ParquetRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoParquetWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, int? position, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, string nullValue = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, nullValue, field.GetReflectedType());
        }

        public ChoParquetWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, string nullValue = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, nullValue, field.GetReflectedType());
        }

        public ChoParquetWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoParquetRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field.GetMemberName(), setup);
            return this;
        }

        public ChoParquetWriter<T> WithField(string name, Action<ChoParquetRecordFieldConfigurationMap> mapper)
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

        public ChoParquetWriter<T> WithField<TField>(Expression<Func<T, TField>> field, int position, Type fieldType = null, bool? quoteField = null,
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string formatText = null, string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, fieldType == null ? field.GetPropertyType() : fieldType, quoteField, fillChar, fieldValueJustification, truncate,
                fieldName, valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoParquetWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, bool? quoteField = null, 
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, 
            string formatText = null, string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, fieldType == null ? field.GetPropertyType() : fieldType, quoteField, fillChar, fieldValueJustification, truncate, 
                fieldName, valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoParquetWriter<T> WithField(string name, Type fieldType = null, bool? quoteField = null, char? fillChar = null, 
            ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, 
            string formatText = null, string nullValue = null)
        {
            return WithField(name, null, fieldType, quoteField, fillChar, fieldValueJustification,
                truncate, fieldName, valueConverter, valueSelector, customSerializer, headerSelector, 
                defaultValue, fallbackValue, null, formatText, nullValue);
        }

        private ChoParquetWriter<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null, char? fillChar = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            bool? truncate = null, string fieldName = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
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
                    valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, null, fullyQualifiedMemberName, formatText,
                    nullValue, typeof(T), subRecordType, fieldValueJustification);
            }

            return this;
        }

        public ChoParquetWriter<T> Index<TField>(Expression<Func<T, TField>> field, int minumum, int maximum)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }

            Configuration.IndexMap(field, minumum, maximum, null);
            return this;
        }

        public ChoParquetWriter<T> DictionaryKeys<TField>(Expression<Func<T, TField>> field, params string[] keys)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }

            Configuration.DictionaryMap(field, keys, null);
            return this;
        }

        public ChoParquetWriter<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoParquetWriter<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoParquetWriter<T> TreatDateTimeAsDateTimeOffset(bool flag = true, TimeSpan? offset = null)
        {
            Configuration.TreatDateTimeAsDateTimeOffset = flag;
            Configuration.DateTimeOffset = offset;
            return this;
        }

        public ChoParquetWriter<T> TreatDateTimeAsString(bool flag = true, string format = null)
        {
            Configuration.TreatDateTimeAsString = flag;
            if (format != null)
                Configuration.TypeConverterFormatSpec.DateTimeFormat = format;
            return this;
        }

        public ChoParquetWriter<T> Configure(Action<ChoParquetRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoParquetWriter<T> Setup(Action<ChoParquetWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoParquetWriter<T> MapRecordFields<TClass>()
        {
            Configuration.MapRecordFields<TClass>();
            return this;
        }

        public ChoParquetWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
                Configuration.MapRecordFields(recordType);

            return this;
        }

        //public ChoParquetWriter<T> WithComments(params string[] comments)
        //{
        //    Configuration.Comments = comments;
        //    return this;
        //}

        #endregion Fluent API

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();

            Configuration.UseNestedKeyFormat = false;

            int ordinal = 0;
            if (Configuration.ParquetRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    Configuration.ParquetRecordFieldConfigurations.Add(new ChoParquetRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            var ordinals = Configuration.ParquetRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
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
            if (Configuration.ParquetRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    Configuration.ParquetRecordFieldConfigurations.Add(new ChoParquetRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in Configuration.ParquetRecordFieldConfigurations)
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

        ~ChoParquetWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }

    }

    public class ChoParquetWriter : ChoParquetWriter<dynamic>
    {
        public ChoParquetWriter(ChoParquetRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoParquetWriter(string filePath, ChoParquetRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {
        }

        public ChoParquetWriter(StreamWriter streamWriter, ChoParquetRecordConfiguration configuration = null)
            : base(streamWriter, configuration)
        {

        }
        public ChoParquetWriter(Stream inStream, ChoParquetRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static byte[] SerializeAll(IEnumerable<dynamic> records, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoParquetWriter(writer))
                {
                    w.Write(records);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] SerializeAll<T>(IEnumerable<T> records, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoParquetWriter<T>(writer))
                {
                    w.Write(records);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] Serialize(dynamic record, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoParquetWriter(writer))
                {
                    w.Write(record);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static byte[] Serialize<T>(T record, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                using (var w = new ChoParquetWriter<T>(writer))
                {
                    w.Write(record);
                }
                writer.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
    public class ChoNewRowGroupEventArgs : EventArgs
    {
        public ChoNewRowGroupEventArgs(int index, List<dynamic> records)
        {
            Index = index;
            Records = records;
        }

        public bool DoNotCreateNewRowGroup { get; set; }
        public List<dynamic> Records { get; }
        public int Index { get; }
    }
}
