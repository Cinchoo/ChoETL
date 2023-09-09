using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
    //where T : class
    {
        private Lazy<TextReader> _textReader;
        private JsonTextReader _JSONReader;
        private IEnumerable<JToken> _jObjects;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public override dynamic Context
        {
            get { return Configuration.Context; }
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

            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(TextReader textReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = new Lazy<TextReader>(() => textReader);
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
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncodingInternal(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
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
            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoJSONReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => textReader);
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
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
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

            _jObjects = null;
            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_textReader != null)
                    _textReader.Value.Dispose();
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
            _isDisposed = false;
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            Configuration.RecordTypeInternal = Configuration.RecordTypeInternal.GetUnderlyingType();
            Configuration.IsDynamicObjectInternal = Configuration.RecordTypeInternal.IsDynamicType();
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
            CheckDisposed();
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = Create(_textReader.Value);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
                var e = rr.AsEnumerable(_JSONReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
                var e = rr.AsEnumerable(_jObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() =>
                {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader(Action<IDictionary<string, object>> selector = null)
        {
            return AsDataReader(null, selector);
        }

        private IDataReader AsDataReader(Action<IDictionary<string, Type>> membersDiscovered, Action<IDictionary<string, object>> selector = null)
        {
            CheckDisposed();
            this.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
            return this.Select(s =>
            {
                IDictionary<string, object> dict = null;
                if (s is IDictionary<string, object>)
                    dict = ((IDictionary<string, object>)s).Flatten(Configuration.NestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : Configuration.NestedKeySeparator, 
                        Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix, Configuration.ArrayValueNamePrefix,
                        Configuration.IgnoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix);
                else
                    dict = s.ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix).Flatten(Configuration.NestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : Configuration.NestedKeySeparator, 
                        Configuration.ArrayIndexSeparator, Configuration.ArrayEndIndexSeparator, Configuration.IgnoreDictionaryFieldPrefix, Configuration.ArrayValueNamePrefix,
                        Configuration.IgnoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: Configuration.ArrayValueNamePrefix);

                selector?.Invoke(dict);

                return dict as object;
            }).AsDataReader();
        }

        public DataTable AsDataTable(Action<IDictionary<string, object>> selector)
        {
            return AsDataTable(null, selector);
        }

        public DataTable AsDataTable(string tableName = null, Action<IDictionary<string, object>> selector = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Locale = Configuration.Culture;
            dt.Load(AsDataReader(selector));
            return dt;
        }

        public void Fill(DataTable dt, Action<IDictionary<string, object>> selector = null)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader(selector));
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

        public ChoJSONReader<T> WithJSONConverter(JsonConverter converter)
        {
            Configuration.WithJSONConverter(converter);
            return this;
        }

        public ChoJSONReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoJSONReader<T> RegisterNodeConverterForType<ModelType>(Func<object, object> selector)
        {
            return RegisterNodeConverterForType(typeof(ModelType), selector);
        }

        public ChoJSONReader<T> RegisterNodeConverterForType(Type type, Func<object, object> selector)
        {
            Configuration.RegisterNodeConverterForType(type, selector);
            return this;
        }

        public ChoJSONReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoJSONReader<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoJSONReader<T> AllowComplexJSONPath(bool flag = true)
        {
            Configuration.AllowComplexJSONPath = flag;
            return this;
        }

        public ChoJSONReader<T> SupportMultipleContent(bool flag = true)
        {
            Configuration.SupportMultipleContent = flag;
            return this;
        }

        public ChoJSONReader<T> UseJsonSerialization(bool flag = true)
        {
            Configuration.UseJSONSerialization = flag;
            return this;
        }

        public ChoJSONReader<T> JsonSerializationSettings(Action<JsonSerializerSettings> settings)
        {
            settings?.Invoke(Configuration.JsonSerializerSettings);
            return this;
        }

        public ChoJSONReader<T> JsonSerializerContext(Action<dynamic> ctxSettings)
        {
            ctxSettings?.Invoke((dynamic)Configuration.JsonSerializer.Context.Context);
            return this;
        }

        public ChoJSONReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoJSONReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONReader<T> WithJSONPath(string jsonPath, bool allowComplexJSONPath = false, bool flattenIfJArrayWhenReading = true)
        {
            Configuration.JSONPath = jsonPath;
            Configuration.AllowComplexJSONPath = allowComplexJSONPath;
            Configuration.FlattenIfJArrayWhenReading = flattenIfJArrayWhenReading;
            return this;
        }

        public ChoJSONReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        private ChoJSONReader<T> ClearFieldsIf()
        {
            if (!_clearFields)
            {
                Configuration.ClearFields();
                _clearFields = true;
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            return this;
        }

        public ChoJSONReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            ClearFieldsIf();
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoJSONReader<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                string fnTrim = null;
                ClearFieldsIf();
                fnTrim = fieldName.NTrim();
                Configuration.IgnoreField(fnTrim);
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
                    ClearFieldsIf();

                    fnTrim = fn.NTrim();
                    if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, (string)null);
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.LoadFieldConfigurationAttributes(nfc, typeof(T));
                    Configuration.JSONRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoJSONReader<T> WithField(string name, Action<ChoJSONRecordFieldConfigurationMap> mapper)
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

        public ChoJSONReader<T> ClearFieldForType<TClass>()
        {
            Configuration.ClearRecordFieldsForType(typeof(TClass));
            return this;
        }

        public ChoJSONReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            string jsonPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null,
            IChoValueConverter propertyConverter = null
            )
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), jsonPath, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, typeof(TClass), 
                fieldTypeSelector, itemTypeSelector, fieldTypeDiscriminator, itemTypeDiscriminator, propertyConverter);
        }

        public ChoJSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            ClearFieldsIf();
            
            if (!field.GetMemberName().IsNullOrWhiteSpace())
                Configuration.Map(field.GetMemberName(), mapper);
            return this;
        }

        public ChoJSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, string jsonPath = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null,
            IChoValueConverter propertyConverter = null
            )
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), jsonPath, field.GetPropertyType(), fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, null, fieldTypeSelector, itemTypeSelector,
                fieldTypeDiscriminator, itemTypeDiscriminator, propertyConverter);
        }

        public ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, bool isArray = true,
            string nullValue = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null,
            IChoValueConverter propertyConverter = null
            )
        {
            return WithField(name, jsonPath, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, null, formatText, isArray, nullValue, null, fieldTypeSelector, itemTypeSelector,
                fieldTypeDiscriminator, itemTypeDiscriminator, propertyConverter);
        }

        private ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null,
            Type subRecordType = null, Func<object, Type> fieldTypeSelector = null, Func<object, Type> itemTypeSelector = null,
            string fieldTypeDiscriminator = null, string itemTypeDiscriminator = null, IChoValueConverter propertyConverter = null
            )
        {
            if (!name.IsNullOrEmpty())
            {
                ClearFieldsIf();

                Configuration.WithField(name, jsonPath, fieldType, fieldValueTrimOption, fieldName,
                    valueConverter, itemConverter, customSerializer, defaultValue, fallbackValue, fullyQualifiedMemberName, formatText,
                    isArray, nullValue, typeof(T), subRecordType, fieldTypeSelector, itemTypeSelector, fieldTypeDiscriminator, itemTypeDiscriminator,
                    propertyConverter);
            }
            return this;
        }

        public ChoJSONReader<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFieldsIf();
            Configuration.MapRecordFields(Configuration.RecordTypeInternal);
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
            Configuration.RecordTypeSelector = recordSelector;
            return this;
        }

        public ChoJSONReader<T> WithCustomNodeSelector(Func<JObject, JObject> nodeSelector)
        {
            Configuration.CustomNodeSelecter = nodeSelector;
            return this;
        }

        public ChoJSONReader<T> UseDefaultContractResolver(bool flag = true, Action<ChoPropertyRenameAndIgnoreSerializerContractResolver> setup = null)
        {
            Configuration.UseDefaultContractResolver = flag;
            Configuration.DefaultContractResolverSetup = setup;
            return this;
        }

        public ChoJSONReader<T> ConfigureContractResolver(Action<IContractResolver> setup = null)
        {
            Configuration.ConfigureContractResolver(setup);
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
        public ChoJSONReader(JsonTextReader txtReader, ChoJSONRecordConfiguration configuration = null)
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
        //where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return DeserializeText<T>(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class
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
        //where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
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

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, string jsonPath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
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
        //where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
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
        //where T : class, new()
        {
            return new ChoJSONReader<T>(jObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static dynamic Deserialize(JToken jObject, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoJSONReader(jObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(JToken jObject, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            return new ChoJSONReader<T>(jObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static IEnumerable<T> Deserialize<T>(JsonTextReader jr, string jsonPath, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize<T>(jr, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(JsonTextReader jr, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(T));

            return new ChoJSONReader<T>(jr, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(JsonTextReader jr, string jsonPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoJSONRecordConfiguration();
            configuration.JSONPath = jsonPath;
            return Deserialize(jr, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(JsonTextReader jr, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        //where T : class, new()
        {
            return new ChoJSONReader(jr, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }
    }
}
