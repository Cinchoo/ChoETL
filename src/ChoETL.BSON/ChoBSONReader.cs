using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
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
    public class ChoBSONReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
    //where T : class
    {
        private Lazy<StreamReader> _sr;
        private BsonDataReader _bsonReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;
        private bool _isDisposed = false;

        public ChoBSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoBSONReader(ChoBSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoBSONReader(string filePath, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _sr = new Lazy<StreamReader>(() => new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoBSONReader(BsonDataReader bsonReader, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(bsonReader, "BsonReader");

            Configuration = configuration;
            Init();

            _bsonReader = bsonReader;
        }

        public ChoBSONReader(Stream inStream, ChoBSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize));
            //_closeStreamOnDispose = true;
        }

        public ChoBSONReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _sr = new Lazy<StreamReader>(() => new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoBSONReader<T> Load(StreamReader sr)
        {
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            Close();
            Init();
            _sr = new Lazy<StreamReader>(() => sr);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoBSONReader<T> Load(BsonDataReader bsonReader)
        {
            ChoGuard.ArgumentNotNull(bsonReader, "bsonReader");

            Close();
            Init();
            _bsonReader = bsonReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoBSONReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
                _sr = new Lazy<StreamReader>(() => new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

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
                if (_sr != null)
                    _sr.Value.Dispose();
                if (_bsonReader != null)
                    _bsonReader.Close();
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

            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoBSONRecordConfiguration(recordType);
            else
                Configuration.RecordType = recordType;
            Configuration.IsDynamicObject = Configuration.RecordType.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        private BsonDataReader Create(StreamReader sr)
        {
            return new BsonDataReader(sr.BaseStream);
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoBSONRecordReader rr = new ChoBSONRecordReader(typeof(T), Configuration);
            if (_sr != null)
                _bsonReader = Create(_sr.Value);

            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
            var e = rr.AsEnumerable<T>(_bsonReader).GetEnumerator();
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
            this.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
            return this.Select(s =>
            {
                if (s is IDictionary<string, object>)
                    return ((IDictionary<string, object>)s).Flatten(Configuration.NestedColumnSeparator, Configuration.ArrayIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix).ToDictionary() as object;
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
            foreach (var fn in Configuration.BSONRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.BSONRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.BSONRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoBSONReader<T> AvroSerializerSettings(Action<JsonSerializerSettings> action)
        {
            action?.Invoke(Configuration.JsonSerializerSettings);
            return this;
        }

        public ChoBSONReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoBSONReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoBSONReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoBSONReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoBSONReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoBSONReader<T> IgnoreField(string fieldName)
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
                if (Configuration.BSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.BSONRecordFieldConfigurations.Remove(Configuration.BSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoBSONReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field, (string)null);
            }
            return this;
        }

        public ChoBSONReader<T> WithFields(params string[] fieldsNames)
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
                        //Configuration.ColumnOrderStrict = true;
                    }

                    fnTrim = fn.NTrim();
                    if (Configuration.BSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.BSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.BSONRecordFieldConfigurations.Remove(Configuration.BSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
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

                    Configuration.BSONRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoBSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoBSONRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field, setup);
            return this;
        }

        public ChoBSONReader<T> WithField(string name, Action<ChoBSONRecordFieldConfigurationMap> mapper)
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

        public ChoBSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, string fieldName)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, fieldName: fieldName);
        }

        public ChoBSONReader<T> WithField(string name)
        {
            return WithField(name, null);
        }

        private ChoBSONReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
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
                    valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, altFieldNames, fullyQualifiedMemberName, formatText,
                    nullValue, typeof(T), subRecordType);
            }

            return this;
        }

        public ChoBSONReader<T> Configure(Action<ChoBSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoBSONReader<T> Setup(Action<ChoBSONReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API

        ~ChoBSONReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public static class ChoBSONReader
    {
        public static IEnumerable<T> Deserialize<T>(string filePath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoBSONRecordConfiguration();
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoBSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoBSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoBSONReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoBSONRecordConfiguration();
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoBSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoBSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoBSONReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }
    }
}
