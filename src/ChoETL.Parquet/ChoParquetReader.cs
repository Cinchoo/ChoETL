using Parquet;
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
    public class ChoParquetReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
    //where T : class
    {
        private Lazy<StreamReader> _streamReader;
        private ParquetReader _parquetReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;
        public event EventHandler<ChoRowGroupEventArgs> BeforeRowGroupLoad;
        public event EventHandler<ChoRowGroupEventArgs> AfterRowGroupLoaded;

        public ChoParquetRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoParquetReader(ChoParquetRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoParquetReader(string filePath, ChoParquetRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamReader = new Lazy<StreamReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoParquetReader(ParquetReader parquetReader, ChoParquetRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(parquetReader, "ParquetReader");

            Configuration = configuration;
            Init();

            _parquetReader = parquetReader;
        }

        public ChoParquetReader(Stream inStream, ChoParquetRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _streamReader = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
            {
                _streamReader = new Lazy<StreamReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
            //_closeStreamOnDispose = true;
        }

        public ChoParquetReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _streamReader = new Lazy<StreamReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoParquetReader<T> Load(StreamReader sr)
        {
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            Close();
            Init();
            _streamReader = new Lazy<StreamReader>(() => sr);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoParquetReader<T> Load(ParquetReader parquetReader)
        {
            ChoGuard.ArgumentNotNull(parquetReader, "ParquetReader");

            Close();
            Init();
            _parquetReader = parquetReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoParquetReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _streamReader = new Lazy<StreamReader>(() => new StreamReader(inStream));
            else
                _streamReader = new Lazy<StreamReader>(() => new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

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
                if (_streamReader != null)
                    _streamReader.Value.Dispose();
                if (_parquetReader != null)
                    _parquetReader.Dispose();
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
                Configuration = new ChoParquetRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;
            Configuration.IsDynamicObjectInternal = Configuration.RecordTypeInternal.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        private ParquetReader Create(StreamReader sr)
        {
            var r = ChoAsyncHelper.RunSync<ParquetReader>(() => ParquetReader.CreateAsync(sr.BaseStream, Configuration.ParquetOptions, leaveStreamOpen: Configuration.LeaveStreamOpen));
            if (Configuration != null)
            {
                //if (Configuration.Culture != null)
                //    r.Culture = Configuration.Culture;
                //if (Configuration.SupportMultipleContent != null)
                //    r.SupportMultipleContent = Configuration.SupportMultipleContent.Value;
                //if (Configuration.ParquetSerializerSettings != null)
                //{
                //    r.DateTimeZoneHandling = Configuration.ParquetSerializerSettings.DateTimeZoneHandling;
                //    r.FloatParseHandling = Configuration.ParquetSerializerSettings.FloatParseHandling;
                //    r.DateFormatString = Configuration.ParquetSerializerSettings.DateFormatString;
                //    r.DateParseHandling = Configuration.ParquetSerializerSettings.DateParseHandling;
                //    r.MaxDepth = Configuration.ParquetSerializerSettings.MaxDepth;
                //}
            }
            return r;
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            ChoParquetRecordReader rr = new ChoParquetRecordReader(typeof(T), Configuration);
            if (_streamReader != null)
                _parquetReader = Create(_streamReader.Value);

            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            rr.BeforeRowGroupLoad += BeforeRowGroupLoad;
            rr.AfterRowGroupLoaded += AfterRowGroupLoaded;
            rr.MembersDiscovered += MembersDiscovered;
            rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
            var beforeRowGroup = BeforeRowGroupLoad;
            var afterRowGroup = AfterRowGroupLoaded;
            if (beforeRowGroup != null || afterRowGroup != null)
                rr.InterceptRowGroup = true;

            var e = rr.AsEnumerable(_parquetReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                ++_recordNumber;
                return e.MoveNext();
            }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            return AsDataReader(null);
        }

        private IDataReader AsDataReader(Action<IDictionary<string, Type>> membersDiscovered, Action<IDictionary<string, object>> selector = null)
        {
            CheckDisposed();
            this.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
            return this.Select(s =>
            {
                IDictionary<string, object> dict = null;
                if (s is IDictionary<string, object>)
                    dict = ((IDictionary<string, object>)s).Flatten(Configuration.NestedKeySeparator, Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, 
                        Configuration.IgnoreDictionaryFieldPrefix, Configuration.ArrayValueNamePrefix,
                        Configuration.IgnoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix);
                else
                {
                    dict = s.ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix).Flatten(Configuration.NestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : Configuration.NestedKeySeparator,
                        Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix, Configuration.ArrayValueNamePrefix,
                        Configuration.IgnoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix);
                }

                selector?.Invoke(dict);

                return dict as object;
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
            foreach (var fn in Configuration.ParquetRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.ParquetRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.ParquetRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoParquetReader<T> UseNestedKeyFormat(bool flag = true)
        {
            Configuration.UseNestedKeyFormat = flag;
            return this;
        }

        public ChoParquetReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoParquetReader<T> ParquetOptions(Action<ParquetOptions> action)
        {
            action?.Invoke(Configuration.ParquetOptions);
            return this;
        }

        public ChoParquetReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoParquetReader<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoParquetReader<T> AutoArrayDiscovery(bool flag = true)
        {
            Configuration.AutoArrayDiscovery = flag;
            return this;
        }

        public ChoParquetReader<T> ArrayIndexSeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid array index separator passed.");

            Configuration.ArrayIndexSeparator = value;
            return this;
        }

        public ChoParquetReader<T> NestedKeySeparator(char value)
        {
            if (value == ChoCharEx.NUL)
                throw new ArgumentException("Invalid nested column separator passed.");

            Configuration.NestedKeySeparator = value;
            return this;
        }

        public ChoParquetReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoParquetReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoParquetReader<T> OnRowsLoaded(Action<object, ChoRowsLoadedEventArgs> rowsLoaded)
        {
            RowsLoaded += (o, e) => rowsLoaded(o, e);
            return this;
        }

        public ChoParquetReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        public ChoParquetReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoParquetReader<T> IgnoreField(string fieldName)
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

        public ChoParquetReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoParquetReader<T> WithFields(params string[] fieldsNames)
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
                        //Configuration.ColumnOrderStrict = true;
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

        public ChoParquetReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field, int? position,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null, bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, nullValue, excelField, field.GetReflectedType());
        }

        public ChoParquetReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null, bool excelField = false)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, nullValue, excelField, field.GetReflectedType());
        }

        public ChoParquetReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoParquetRecordFieldConfigurationMap> setup)
        {
            Configuration.Map(field, setup);
            return this;
        }

        public ChoParquetReader<T> WithField(string name, Action<ChoParquetRecordFieldConfigurationMap> mapper)
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

        public ChoParquetReader<T> WithField<TField>(Expression<Func<T, TField>> field,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null, bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), (int?)null, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, nullValue, excelField, field.GetReflectedType());
        }

        public ChoParquetReader<T> WithField<TField>(Expression<Func<T, TField>> field, int? position,
            bool? quoteField = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null,
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            bool excelField = false)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), position, field.GetPropertyType(), quoteField, fieldValueTrimOption, fieldName,
                valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, altFieldNames,
                field.GetFullyQualifiedMemberName(), formatText, excelField, field.GetReflectedType());
        }

        public ChoParquetReader<T> WithField(string name, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            return WithField(name, null, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter,
                valueSelector, customSerializer, headerSelector,
                defaultValue, fallbackValue, altFieldNames, formatText, nullValue, excelField, subRecordType);
        }

        public ChoParquetReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null, string formatText = null,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            return WithField(name, position, fieldType, quoteField, fieldValueTrimOption, fieldName, valueConverter, valueSelector, customSerializer, headerSelector,
                defaultValue, fallbackValue, altFieldNames, null, formatText, nullValue, excelField, subRecordType);
        }

        private ChoParquetReader<T> WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, 
            Func<object, object> customSerializer = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, bool excelField = false, Type subRecordType = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }

                Configuration.WithField(name, position, fieldType, quoteField, fieldValueTrimOption, fieldName,
                    valueConverter, valueSelector, customSerializer, headerSelector, defaultValue, fallbackValue, altFieldNames, fullyQualifiedMemberName, formatText,
                    nullValue, typeof(T), subRecordType);
            }

            return this;
        }

        public ChoParquetReader<T> Index<TField>(Expression<Func<T, TField>> field, int minumum, int maximum)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }

            Configuration.IndexMap(field, minumum, maximum, null);
            return this;
        }

        public ChoParquetReader<T> DictionaryKeys<TField>(Expression<Func<T, TField>> field, params string[] keys)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }

            Configuration.DictionaryMap(field, keys, null);
            return this;
        }

        //public ChoParquetReader<T> IgnoreEmptyLine(bool flag = true)
        //{
        //    Configuration.IgnoreEmptyLine = flag;
        //    return this;
        //}

        //public ChoParquetReader<T> ColumnCountStrict(bool flag = true)
        //{
        //    Configuration.ColumnCountStrict = flag;
        //    return this;
        //}

        //public ChoParquetReader<T> ColumnOrderStrict(bool flag = true)
        //{
        //    Configuration.ColumnOrderStrict = flag;
        //    return this;
        //}

        public ChoParquetReader<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoParquetReader<T> TreatDateTimeAsDateTimeOffset(bool flag = true, TimeSpan? offset = null)
        {
            Configuration.TreatDateTimeAsDateTimeOffset = flag;
            Configuration.DateTimeOffset = offset;
            return this;
        }

        public ChoParquetReader<T> TreatDateTimeAsString(bool flag = true, string format = null)
        {
            Configuration.TreatDateTimeAsString = flag;
            if (format != null)
                Configuration.TypeConverterFormatSpec.DateTimeFormat = format;
            return this;
        }

        public ChoParquetReader<T> Configure(Action<ChoParquetRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoParquetReader<T> Setup(Action<ChoParquetReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoParquetReader<T> MapRecordFields<TClass>()
        {
            MapRecordFields(typeof(TClass));
            return this;
        }

        public ChoParquetReader<T> MapRecordFields(params Type[] recordTypes)
        {
            Configuration.RecordTypeMappedInternal = true;
            if (recordTypes != null)
            {
                foreach (var t in recordTypes)
                {
                    if (t == null)
                        continue;

                    //if (!typeof(T).IsAssignableFrom(t))
                    //	throw new ChoParserException("Incompatible [{0}] record type passed.".FormatString(t.FullName));

                    Configuration.MapRecordFields(t);
                }
            }

            Configuration.MapRecordFields(recordTypes);
            return this;
        }

        public ChoParquetReader<T> WithCustomRecordTypeCodeExtractor(Func<string, string> recordTypeCodeExtractor)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordTypeCodeExtractor = recordTypeCodeExtractor;
            return this;
        }

        public ChoParquetReader<T> WithCustomRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordTypeSelector = recordSelector;
            return this;
        }

        #endregion Fluent API

        ~ChoParquetReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoParquetReader : ChoParquetReader<dynamic>
    {
        public ChoParquetReader(ChoParquetRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoParquetReader(string filePath, ChoParquetRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoParquetReader(Stream inStream, ChoParquetRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoParquetRecordConfiguration();
            return Deserialize(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoParquetRecordConfiguration();

            if (configuration != null)
            {
            }
            return new ChoParquetReader(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoParquetRecordConfiguration();
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoParquetRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoParquetReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoParquetRecordConfiguration();
            return Deserialize(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoParquetRecordConfiguration();

            if (configuration != null)
            {
            }
            return new ChoParquetReader(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoParquetRecordConfiguration();
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoParquetRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoParquetRecordConfiguration(typeof(T));

            if (configuration != null)
            {
            }
            return new ChoParquetReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }
    }

    public class ChoRowGroupEventArgs : EventArgs
    {
        public ChoRowGroupEventArgs(int index, List<Parquet.Data.DataColumn[]> records)
        {
            RowGroupIndex = index;
            Records = records;
        }

        public bool Skip { get; set; }
        public List<Parquet.Data.DataColumn[]> Records { get; }
        public int RowGroupIndex { get; }
    }
}
