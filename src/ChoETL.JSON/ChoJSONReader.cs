using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ChoJSONReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
        where T : class
    {
        private TextReader _textReader;
        private JsonTextReader _JSONReader;
        private IEnumerable<JToken> _jObjects;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        private bool _isDisposed = false;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONReader(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

        }

        public ChoJSONReader(ChoJSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _JSONReader = new JsonTextReader(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(TextReader textReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = textReader;
        }

        public ChoJSONReader(JsonTextReader JSONReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(JSONReader, "JSONReader");

            Configuration = configuration;
            Init();

            _JSONReader = JSONReader;
        }

        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            //_closeStreamOnDispose = true;
        }

        public ChoJSONReader(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(jObjects, "JObjects");

            Configuration = configuration;
            Init();
            _jObjects = jObjects;
        }

        public ChoJSONReader(JToken jObject, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(jObject, "jObject");

            Configuration = configuration;
            Init();
            _jObjects = new JToken[] { jObject };
        }

        public ChoJSONReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoJSONReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = textReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoJSONReader<T> Load(JsonTextReader JSONReader)
        {
            ChoGuard.ArgumentNotNull(JSONReader, "JSONReader");

            Close();
            Init();
            _JSONReader = JSONReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoJSONReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoJSONReader<T> Load(IEnumerable<JToken> jObjects)
        {
            ChoGuard.ArgumentNotNull(jObjects, "JObjects");

            Init();
            _jObjects = jObjects;
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
                if (_textReader != null)
                    _textReader.Dispose();
                if (_JSONReader != null)
                    _JSONReader.Close();
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
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = Configuration.RecordType.GetUnderlyingType();
            Configuration.IsDynamicObject = Configuration.RecordType.IsDynamicType();
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoJSONReader<T> LoadText(string inputText, ChoJSONRecordConfiguration configuration = null)
        {
            return LoadText(inputText, null, configuration);
        }

        public static ChoJSONReader<T> LoadText(string inputText, Encoding encoding, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(inputText.ToStream(encoding), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoJSONReader<T> LoadJTokens(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(jObjects, configuration);
            return r;
        }

        public static T LoadJToken(JToken jObject, ChoJSONRecordConfiguration configuration = null)
        {
            if (jObject == null) return default(T);

            return LoadJTokens(new JToken[] { jObject }, configuration).FirstOrDefault();
        }

        //internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize)
        //{
        //    ChoJSONRecordReader rr = new ChoJSONRecordReader(recType, configuration);
        //    rr.TraceSwitch = ChoETLFramework.TraceSwitchOff;
        //    return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        //}

        private JsonTextReader Create(TextReader textReader)
        {
            var r = new JsonTextReader(textReader);
            if (Configuration != null)
            {
                if (Configuration.Culture != null)
                    r.Culture = Configuration.Culture;
                if (Configuration.SupportMultipleContent != null)
                    r.SupportMultipleContent = Configuration.SupportMultipleContent.Value;
                if (Configuration.JsonSerializerSettings != null)
                {
                    r.DateTimeZoneHandling = Configuration.JsonSerializerSettings.DateTimeZoneHandling;
                    r.FloatParseHandling = Configuration.JsonSerializerSettings.FloatParseHandling;
                    r.DateFormatString = Configuration.JsonSerializerSettings.DateFormatString;
                    r.DateParseHandling = Configuration.JsonSerializerSettings.DateParseHandling;
                    r.MaxDepth = Configuration.JsonSerializerSettings.MaxDepth;
                }
            }
            return r;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = Create(_textReader);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_JSONReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_jObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
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
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = Create(_textReader);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_JSONReader), rr);
                return dr;
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_jObjects), rr);
                return dr;
            }
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
            foreach (var fn in Configuration.JSONRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.JSONRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.JSONRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoJSONReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONReader<T> WithJSONPath(string jsonPath)
        {
            Configuration.JSONPath = jsonPath;
            return this;
        }

        public ChoJSONReader<T> ClearFields()
        {
            Configuration.JSONRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoJSONReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (field != null)
                return IgnoreField(field.GetMemberName());
            else
                return this;
        }

        public ChoJSONReader<T> IgnoreField(string fieldName)
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

        public ChoJSONReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        //public ChoJSONReader<T> WithFields<TClass, TField>(params Expression<Func<TClass, TField>>[] fields)
        //{
        //    if (fields != null)
        //    {
        //        foreach (var field in fields)
        //            return WithField<TClass>(field);
        //    }
        //    return this;
        //}

        public ChoJSONReader<T> WithFields(params string[] fieldsNames)
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

        //public ChoJSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> setup)
        //{
        //    Configuration.MapRecordField(field.GetMemberName(), setup);
        //    return this;
        //}

        //public ChoJSONReader<T> WithField<TClass, TField>(Expression<Func<TClass, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> setup)
        //{
        //    Configuration.MapRecordField(field.GetMemberName(), setup);
        //    return this;
        //}

        public ChoJSONReader<T> WithField(string name, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
                Configuration.MapRecordField(name, mapper);
            return this;
        }

        public ChoJSONReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            string jsonPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            bool isJSONAttribute = false, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<JObject, Type> fieldTypeSelector = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), jsonPath, fieldType, fieldValueTrimOption, isJSONAttribute, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, typeof(TClass), fieldTypeSelector);
        }

        public ChoJSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, string jsonPath = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<JObject, Type> fieldTypeSelector = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), jsonPath, field.GetPropertyType(), fieldValueTrimOption, isJSONAttribute, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, null, fieldTypeSelector);
        }

        public ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, bool isArray = true,
            string nullValue = null, Func<JObject, Type> fieldTypeSelector = null)
        {
            return WithField(name, jsonPath, fieldType, fieldValueTrimOption, isJSONAttribute, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, null, formatText, isArray, nullValue, null, fieldTypeSelector);
        }

        private ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null,
            Type recordType = null, Func<JObject, Type> fieldTypeSelector = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }
                if (recordType != null)
                    Configuration.MapRecordFieldsForType(recordType);

                string fnTrim = name.NTrim();
                ChoJSONRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    Configuration.JSONRecordFieldConfigurations.Remove(fc);
                }
                else if (recordType != null)
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, jsonPath)
                {
                    FieldType = fieldType,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    CustomSerializer = customSerializer,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    ItemConverter = itemConverter,
                    IsArray = isArray,
                    NullValue = nullValue,
                    FieldTypeSelector = fieldTypeSelector,
                };
                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : fullyQualifiedMemberName;
                }
                else
                {
                    if (recordType == null)
                        pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName);
                    else
                        pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName);

                    nfc.PropertyDescriptor = pd;
                    nfc.DeclaringMember = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                if (recordType == null)
                    Configuration.JSONRecordFieldConfigurations.Add(nfc);
                else
                    Configuration.Add(recordType, nfc);
            }

            return this;
        }

        public ChoJSONReader<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFields();
            Configuration.MapRecordFields(Configuration.RecordType);
            return this;
        }

        public ChoJSONReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoJSONReader<T> Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoJSONReader<T> Setup(Action<ChoJSONReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoJSONReader<T> WithMaxScanNodes(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoJSONReader<T> WithCustomRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        #endregion Fluent API

        ~ChoJSONReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoJSONReader : ChoJSONReader<dynamic>
    {
        public ChoJSONReader(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }

        public ChoJSONReader(ChoJSONRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoJSONReader(TextReader txtReader, ChoJSONRecordConfiguration configuration = null)
            : base(txtReader, configuration)
        {
        }
        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
        public ChoJSONReader(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
            : base(jObjects, configuration)
        {
        }
        public ChoJSONReader(JToken jObject, ChoJSONRecordConfiguration configuration = null)
            : base(jObject, configuration)
        {
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, string jsonPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return DeserializeText(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, string jsonPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return DeserializeText<T>(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, string jsonPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, string jsonPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, string jsonPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize<T>(TextReader textReader, string jsonPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, string jsonPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, string jsonPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.SupportMultipleContent == null)
                    configuration.SupportMultipleContent = false;
                //if (configuration.JSONPath.IsNullOrWhiteSpace())
                //    configuration.JSONPath = "$";
            }
            return new ChoJSONReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoJSONReader(jObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoJSONReader<T>(jObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static dynamic Deserialize(JToken jObject, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoJSONReader(jObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(JToken jObject, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoJSONReader<T>(jObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }
    }
}
