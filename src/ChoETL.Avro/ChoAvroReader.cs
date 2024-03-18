using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
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
    public class ChoAvroReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
    //where T : class
    {
        private Lazy<StreamReader> _sr;
        private object _avroReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;
        internal object AvroSerializer = null;

        public ChoAvroRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoAvroReader(ChoAvroRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoAvroReader(string filePath, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _sr = new Lazy<StreamReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoAvroReader(IAvroReader<T> avroReader, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(avroReader, "AvroReader");

            Configuration = configuration;
            Init();

            _avroReader = avroReader;
        }

        protected ChoAvroReader(IAvroReader<Dictionary<string, object>> avroReader, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(avroReader, "AvroReader");

            Configuration = configuration;
            Init();

            _avroReader = avroReader;
        }

        public ChoAvroReader(Stream inStream, ChoAvroRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
            {
                _sr = new Lazy<StreamReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
            //_closeStreamOnDispose = true;
        }

        public virtual ChoAvroReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _sr = new Lazy<StreamReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public virtual ChoAvroReader<T> Load(StreamReader sr)
        {
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            Close();
            Init();
            _sr = new Lazy<StreamReader>(() => sr);
            _closeStreamOnDispose = false;

            return this;
        }

        public virtual ChoAvroReader<T> Load(IAvroReader<T> avroReader)
        {
            ChoGuard.ArgumentNotNull(avroReader, "AvroReader");

            Close();
            Init();
            _avroReader = avroReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public virtual ChoAvroReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
            {
                _sr = new Lazy<StreamReader>(() =>
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
                if (_sr != null)
                    _sr.Value.Dispose();
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

            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoAvroRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;
            Configuration.IsDynamicObjectInternal = Configuration.RecordTypeInternal.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            ChoAvroRecordReader rr = new ChoAvroRecordReader(typeof(T), Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;

            if (typeof(T) == typeof(object))
            {
                IEnumerator<object> e = _avroReader != null ? rr.AsEnumerable<Dictionary<string, object>>(_avroReader).GetEnumerator() : rr.AsEnumerable<Dictionary<string, object>>(_sr).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)((object)e.Current), () => Dispose()).GetEnumerator();
            }
            else
            {
                IEnumerator<object> e = _avroReader != null ? rr.AsEnumerable<T>(_avroReader).GetEnumerator() : rr.AsEnumerable<T>(_sr).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
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
            this.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
            return this.Select(s =>
            {
                if (s is IDictionary<string, object>)
                    return ((IDictionary<string, object>)s).Flatten(Configuration.NestedKeySeparator, Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, 
                        Configuration.IgnoreDictionaryFieldPrefix, Configuration.ArrayValueNamePrefix,
                        Configuration.IgnoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix) as object;
                else
                    return s;
            }).AsDataReader();
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Locale = Configuration.Culture;
            dt.Load(AsDataReader());
            return dt;
        }

        public void Fill(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader());
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
            foreach (var fn in Configuration.AvroRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.AvroRecordFieldConfigurations.Select(fc => fc.FieldName)
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
            if (columnMappings == null)
                columnMappings = Configuration.AvroRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoAvroReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoAvroReader<T> AvroSerializerSettings(Action<AvroSerializerSettings> action)
        {
            action?.Invoke(Configuration.AvroSerializerSettings);
            return this;
        }

        public ChoAvroReader<T> KnownTypes(params Type[] types)
        {
            if (types != null)
                Configuration.KnownTypes = types.ToList();

            return this;
        }

        public ChoAvroReader<T> UseAvroSerializer(bool flag = true)
        {
            Configuration.UseAvroSerializer = flag;
            return this;
        }

        public virtual ChoAvroReader<T> WithAvroSerializer(IAvroSerializer<T> avroSerializer)
        {
            AvroSerializer = avroSerializer;
            Configuration.UseAvroSerializer = true;
            return this;
        }

        public ChoAvroReader<T> WithRecordSchema(string schema)
        {
            Configuration.RecordSchema = schema;
            return this;
        }

        public ChoAvroReader<T> WithCodecFactory(CodecFactory cf)
        {
            Configuration.CodecFactory = cf;
            return this;
        }

        public ChoAvroReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoAvroReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoAvroReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoAvroReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoAvroReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoAvroReader<T> IgnoreField(string fieldName)
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

        public ChoAvroReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field, (string)null);
            }
            return this;
        }

        public ChoAvroReader<T> WithFields(params string[] fieldsNames)
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
                        //Configuration.ColumnOrderStrict = true;
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

        public ChoAvroReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoAvroRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field, setup);
            return this;
        }

        public ChoAvroReader<T> WithField(string name, Action<ChoAvroRecordFieldConfigurationMap> mapper)
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

        public ChoAvroReader<T> WithField<TField>(Expression<Func<T, TField>> field, string fieldName)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, fieldName: fieldName);
        }

        public ChoAvroReader<T> WithField(string name)
        {
            return WithField(name, null);
        }

        private ChoAvroReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, bool excelField = false, Type subRecordType = null,
            IChoValueConverter propertyConverter = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }

                Configuration.WithField(name, position, fieldType, quoteField, fieldValueTrimOption, fieldName,
                    valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames, fullyQualifiedMemberName, formatText,
                    nullValue, typeof(T), subRecordType, propertyConverter: propertyConverter);
            }

            return this;
        }

        public ChoAvroReader<T> Configure(Action<ChoAvroRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoAvroReader<T> Setup(Action<ChoAvroReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API

        ~ChoAvroReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoAvroReader : ChoAvroReader<dynamic>
    {
        public ChoAvroReader(ChoAvroRecordConfiguration configuration = null) : base(configuration)
        {
        }

        public ChoAvroReader(string filePath, ChoAvroRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {
        }

        public ChoAvroReader(IAvroReader<Dictionary<string, object>> avroReader, ChoAvroRecordConfiguration configuration = null)
            : base(avroReader, configuration)
        {
        }

        public ChoAvroReader(Stream inStream, ChoAvroRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public override ChoAvroReader<dynamic> Load(string filePath)
        {
            return base.Load(filePath);
        }

        public override ChoAvroReader<dynamic> Load(StreamReader sr)
        {
            return base.Load(sr);
        }

        public override ChoAvroReader<dynamic> Load(IAvroReader<dynamic> avroReader)
        {
            return base.Load(avroReader);
        }

        public override ChoAvroReader<dynamic> Load(Stream inStream)
        {
            return base.Load(inStream);
        }

        #region Fluent API

        public override ChoAvroReader<dynamic> WithAvroSerializer(IAvroSerializer<dynamic> avroSerializer)
        {
            throw new NotSupportedException("Use WithAvroSerializer(IAvroSerializer<Dictionary<string, object>> avroSerializer) instead.");
        }

        public ChoAvroReader<dynamic> WithAvroSerializer(IAvroSerializer<Dictionary<string, object>> avroSerializer)
        {
            AvroSerializer = avroSerializer;
            Configuration.UseAvroSerializer = true;
            return this;
        }
        
        #endregion Fluent API

        public static IEnumerable<T> Deserialize<T>(string filePath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoAvroRecordConfiguration();
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoAvroRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoAvroReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoAvroRecordConfiguration();
            return Deserialize(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoAvroRecordConfiguration();

            if (configuration != null)
            {
            }
            return new ChoAvroReader(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoAvroRecordConfiguration();
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoAvroRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoAvroReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoAvroRecordConfiguration();
            return Deserialize(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoAvroRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoAvroRecordConfiguration();

            if (configuration != null)
            {
            }
            return new ChoAvroReader(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }
    }
}
