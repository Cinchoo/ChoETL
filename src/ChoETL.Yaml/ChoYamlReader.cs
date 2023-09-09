using SharpYaml;
using SharpYaml.Serialization;
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
    public class ChoYamlReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
    {
        public const string NODE_VALUE = "##VALUE##";

        private Lazy<TextReader> _textReader;
        private object _yamlStream;
        private IEnumerable<YamlNode> _yamlObjects;
        private IEnumerable<YamlDocument> _yamlDocObjects;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;

        public ChoYamlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoYamlReader(StringBuilder sb, ChoYamlRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

        }

        public ChoYamlReader(ChoYamlRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoYamlReader(string filePath, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoYamlReader(TextReader textReader, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = new Lazy<TextReader>(() => textReader);
        }

        public ChoYamlReader(YamlStream yamlStream, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(yamlStream, "YamlStream");

            Configuration = configuration;
            Init();

            _yamlStream = yamlStream;
        }

        public ChoYamlReader(Stream inStream, ChoYamlRecordConfiguration configuration = null)
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

        public ChoYamlReader(IEnumerable<YamlNode> yamlObjects, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(yamlObjects, "YamlObjects");

            Configuration = configuration;
            Init();
            _yamlObjects = yamlObjects;
        }

        public ChoYamlReader(YamlNode yamlNode, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(yamlNode, "YamlNode");

            Configuration = configuration;
            Init();
            _yamlObjects = new YamlNode[] { yamlNode };
        }

        public ChoYamlReader(IEnumerable<YamlDocument> yamlDocObjects, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(yamlDocObjects, "YamlDocObjects");

            Configuration = configuration;
            Init();
            _yamlDocObjects = yamlDocObjects;
        }

        public ChoYamlReader(YamlDocument yamlDoc, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(yamlDoc, "YamlDoc");

            Configuration = configuration;
            Init();
            _yamlDocObjects = new YamlDocument[] { yamlDoc };
        }

        public ChoYamlReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncodingInternal(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoYamlReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => textReader);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoYamlReader<T> Load(YamlStream yamlStream)
        {
            ChoGuard.ArgumentNotNull(yamlStream, "YamlStream");

            Close();
            Init();
            _yamlStream = yamlStream;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoYamlReader<T> Load(Stream inStream)
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

        public ChoYamlReader<T> Load(IEnumerable<YamlNode> yamlObjects)
        {
            ChoGuard.ArgumentNotNull(yamlObjects, "YamlObjects");

            Init();
            _yamlObjects = yamlObjects;
            return this;
        }

        public ChoYamlReader<T> Load(IEnumerable<YamlDocument> yamlDocObjects)
        {
            ChoGuard.ArgumentNotNull(yamlDocObjects, "YamlDocObjects");

            Init();
            _yamlDocObjects = yamlDocObjects;
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

            _yamlObjects = null;
            _yamlDocObjects = null;
            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_textReader != null)
                    _textReader.Value.Dispose();
                //if (_yamlStream != null)
                //    _yamlStream.Close();
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
                Configuration = new ChoYamlRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;
            Configuration.IsDynamicObjectInternal = Configuration.RecordTypeInternal.IsDynamicType();

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
            {
                _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
            }
        }

        public static ChoYamlReader<T> LoadText(string inputText, ChoYamlRecordConfiguration configuration = null)
        {
            return LoadText(inputText, null, configuration);
        }

        public static ChoYamlReader<T> LoadText(string inputText, Encoding encoding, ChoYamlRecordConfiguration configuration = null)
        {
            var r = new ChoYamlReader<T>(inputText.ToStream(encoding), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoYamlReader<T> LoadYamlNodes(IEnumerable<YamlNode> yamlObjects, ChoYamlRecordConfiguration configuration = null)
        {
            var r = new ChoYamlReader<T>(yamlObjects, configuration);
            return r;
        }

        public static T LoadYamlNode(YamlNode yamlObject, ChoYamlRecordConfiguration configuration = null)
        {
            if (yamlObject == null) return default(T);

            return LoadYamlNodes(new YamlNode[] { yamlObject }, configuration).FirstOrDefault();
        }

        private EventReader Create(TextReader textReader)
        {
            var r = new EventReader(Parser.CreateParser(textReader));
            return r;
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckDisposed();
            if (_yamlStream != null || _textReader != null)
            {
                ChoYamlRecordReader rr = new ChoYamlRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _yamlStream = Create(_textReader.Value);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
                var e = rr.AsEnumerable(_yamlStream).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else if (_yamlObjects != null)
            {
                ChoYamlRecordReader rr = new ChoYamlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
                var e = rr.AsEnumerable(_yamlObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else if (_yamlDocObjects != null)
            {
                ChoYamlRecordReader rr = new ChoYamlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                rr.RecordFieldTypeAssessment += RecordFieldTypeAssessment;
                var e = rr.AsEnumerable(_yamlDocObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => {
                    ++_recordNumber;
                    return e.MoveNext();
                }, () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else
                return Enumerable.Empty<T>().GetEnumerator();
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

            try
            {
                dt.BeginLoadData();
                dt.Load(AsDataReader(selector));
            }
            finally
            {
                dt.EndLoadData();
            }
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
            foreach (var fn in Configuration.YamlRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.YamlRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.YamlRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoYamlReader<T> WithTagMapping(string tagName, Type tagType, bool isAlias = false)
        {
            Configuration.WithTagMapping(tagName, tagType, isAlias);
            return this;
        }

        public ChoYamlReader<T> DetectEncodingFromByteOrderMarks(bool value = true)
        {
            Configuration.DetectEncodingFromByteOrderMarks = value;
            return this;
        }

        public ChoYamlReader<T> ReuseSerializerObject(bool flag = true)
        {
            Configuration.ReuseSerializerObject = flag;
            return this;
        }

        public ChoYamlReader<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoYamlReader<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoYamlReader<T> YamlSerializerSettings(Action<SerializerSettings> settings)
        {
            settings?.Invoke(Configuration.YamlSerializerSettings);
            return this;
        }

        //public ChoYamlReader<T> AllowComplexYamlPath(bool flag = true)
        //{
        //    Configuration.AllowComplexYamlPath = flag;
        //    return this;
        //}

        public ChoYamlReader<T> UseYamlSerialization(bool flag = true)
        {
            Configuration.UseYamlSerialization = flag;
            return this;
        }

        public ChoYamlReader<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoYamlReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoYamlReader<T> WithYamlPath(string yamlPath, bool allowComplexYamlPath = false)
        {
            Configuration.YamlPath = yamlPath;
            //Configuration.AllowComplexYamlPath = allowComplexYamlPath;
            return this;
        }

        public ChoYamlReader<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        private ChoYamlReader<T> ClearFieldsIf()
        {
            if (!_clearFields)
            {
                Configuration.ClearFields();
                _clearFields = true;
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            return this;
        }

        public ChoYamlReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoYamlReader<T> IgnoreField(string fieldName)
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

        public ChoYamlReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoYamlReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoYamlRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    ClearFieldsIf();

                    fnTrim = fn.NTrim();
                    if (Configuration.YamlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.YamlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.YamlRecordFieldConfigurations.Remove(Configuration.YamlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoYamlRecordFieldConfiguration(fnTrim, (string)null);
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.YamlRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoYamlReader<T> WithField(string name, Action<ChoYamlRecordFieldConfigurationMap> mapper)
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

        public ChoYamlReader<T> ClearFieldForType<TClass>()
        {
            Configuration.ClearRecordFieldsForType(typeof(TClass));
            return this;
        }

        public ChoYamlReader<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            string yamlPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<IDictionary<string, object>, Type> fieldTypeSelector = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), yamlPath, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, typeof(TClass), fieldTypeSelector);
        }

        public ChoYamlReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            ClearFieldsIf();

            if (!field.GetMemberName().IsNullOrWhiteSpace())
                Configuration.Map(field.GetMemberName(), mapper);
            return this;
        }

        public ChoYamlReader<T> WithField<TField>(Expression<Func<T, TField>> field, string yamlPath = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null, Func<IDictionary<string, object>, Type> fieldTypeSelector = null,
            Func<object, Type> itemRecordTypeSelector = null
           )
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), yamlPath, field.GetPropertyType(), fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, null, fieldTypeSelector,
                itemRecordTypeSelector);
        }

        public ChoYamlReader<T> WithField(string name, string yamlPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, bool isArray = true,
            string nullValue = null, Func<IDictionary<string, object>, Type> fieldTypeSelector = null,
            Func<object, Type> itemRecordTypeSelector = null
            )
        {
            return WithField(name, yamlPath, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, null, formatText, isArray, nullValue, null, fieldTypeSelector,
                itemRecordTypeSelector);
        }

        private ChoYamlReader<T> WithField(string name, string yamlPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null,
            Type subRecordType = null, Func<IDictionary<string, object>, Type> fieldTypeSelector = null,
            Func<object, Type> itemRecordTypeSelector = null
            )
        {
            if (!name.IsNullOrEmpty())
            {
                ClearFieldsIf();

                Configuration.WithField(name, yamlPath, fieldType, fieldValueTrimOption, fieldName,
                    valueConverter, itemConverter, customSerializer, defaultValue, fallbackValue, fullyQualifiedMemberName, formatText,
                    isArray, nullValue, typeof(T), subRecordType, fieldTypeSelector, itemRecordTypeSelector);
            }
            return this;
        }

        public ChoYamlReader<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFieldsIf();
            Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            return this;
        }

        public ChoYamlReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoYamlReader<T> Configure(Action<ChoYamlRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoYamlReader<T> Setup(Action<ChoYamlReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoYamlReader<T> WithMaxScanNodes(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoYamlReader<T> WithCustomRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordTypeSelector = recordSelector;
            return this;
        }

        public ChoYamlReader<T> WithCustomNodeSelector(Func<IDictionary<string, object>, IDictionary<string, object>> nodeSelector)
        {
            Configuration.CustomNodeSelecter = nodeSelector;
            return this;
        }

        #endregion Fluent API

        ~ChoYamlReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoYamlReader : ChoYamlReader<dynamic>
    {
        public ChoYamlReader(StringBuilder sb, ChoYamlRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }

        public ChoYamlReader(ChoYamlRecordConfiguration configuration = null)
            : base(configuration)
        {

        }

        public ChoYamlReader(string filePath, ChoYamlRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }

        public ChoYamlReader(TextReader txtReader, ChoYamlRecordConfiguration configuration = null)
            : base(txtReader, configuration)
        {
        }

        public ChoYamlReader(YamlStream stream, ChoYamlRecordConfiguration configuration = null)
            : base(stream, configuration)
        {
        }

        public ChoYamlReader(Stream inStream, ChoYamlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public ChoYamlReader(IEnumerable<YamlNode> yamlObjects, ChoYamlRecordConfiguration configuration = null)
            : base(yamlObjects, configuration)
        {
        }

        public ChoYamlReader(YamlNode yamlObject, ChoYamlRecordConfiguration configuration = null)
            : base(yamlObject, configuration)
        {
        }

        public ChoYamlReader(IEnumerable<YamlDocument> yamlDocObjects, ChoYamlRecordConfiguration configuration = null)
            : base(yamlDocObjects, configuration)
        {
        }

        public ChoYamlReader(YamlDocument yamlDoc, ChoYamlRecordConfiguration configuration = null)
            : base(yamlDoc, configuration)
        {
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, string yamlPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return DeserializeText(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, Encoding encoding = null, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration();

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, string yamlPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return DeserializeText<T>(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration();

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration();

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize<T>(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration();

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, string yamlPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoYamlRecordConfiguration();
            configuration.YamlPath = yamlPath;
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                //if (configuration.YamlPath.IsNullOrWhiteSpace())
                //    configuration.YamlPath = "$";
            }
            return new ChoYamlReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(IEnumerable<YamlNode> yamlObjects, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader(yamlObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(IEnumerable<YamlNode> yamlObjects, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader<T>(yamlObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static dynamic Deserialize(YamlNode yamlObject, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader(yamlObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(YamlNode yamlObject, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader<T>(yamlObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static IEnumerable<dynamic> Deserialize(IEnumerable<YamlDocument> yamlDocs, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader(yamlDocs, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(IEnumerable<YamlDocument> yamlDocs, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader<T>(yamlDocs, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static dynamic Deserialize(YamlDocument yamlDoc, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader(yamlDoc, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(YamlDocument yamlDoc, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader<T>(yamlDoc, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static dynamic Deserialize(YamlStream yamlStream, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader(yamlStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(YamlStream yamlStream, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoYamlReader<T>(yamlStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }
    }
}
