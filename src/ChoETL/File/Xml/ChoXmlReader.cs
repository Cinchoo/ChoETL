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
using System.Xml;
using System.Xml.Linq;

namespace ChoETL
{
    public class ChoXmlReader<T> : ChoReader, IDisposable, IEnumerable<T>, IChoSerializableReader
        where T : class
    {
        //private TextReader _textReader;
        private TextReader _sr;
        private XmlReader _xmlReader;
        private IEnumerable<XElement> _xElements;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlReader(StringBuilder sb, ChoXmlRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

        }

        public ChoXmlReader(ChoXmlRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoXmlReader(string filePath, string defaultNamespace, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;
            Init();
            if (!defaultNamespace.IsNullOrWhiteSpace())
                Configuration.NamespaceManager.AddNamespace("", defaultNamespace);


            _sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            //InitXml();
            _closeStreamOnDispose = true;
        }

        private void InitXml()
        {
            if (_sr != null)
            {
                _xmlReader = XmlReader.Create(_sr,
                    new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
            }
        }

        public ChoXmlReader(string filePath, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            //InitXml();
            _closeStreamOnDispose = true;
        }

        public ChoXmlReader(TextReader textReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _sr = textReader;
            //InitXml();
        }

        public ChoXmlReader(XmlReader xmlReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xmlReader, "XmlReader");

            Configuration = configuration;
            Init();

            _xmlReader = xmlReader;
        }

        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _sr = new StreamReader(inStream);
            else
                _sr = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            //InitXml();
            //_closeStreamOnDispose = true;
        }

        public ChoXmlReader(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xElements, "XmlElements");

            Configuration = configuration;
            Init();
            _xElements = xElements;
        }

        public ChoXmlReader(XElement xElement, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xElement, "XmlElement");

            Configuration = configuration;
            Init();
            _xElements = new XElement[] { xElement };
        }

        public ChoXmlReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            //InitXml();
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoXmlReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _sr = textReader;
            //InitXml();
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoXmlReader<T> Load(XmlReader xmlReader)
        {
            ChoGuard.ArgumentNotNull(xmlReader, "XmlReader");

            Close();
            Init();
            _xmlReader = xmlReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoXmlReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _sr = new StreamReader(inStream);
            else
                _sr = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoXmlReader<T> Load(IEnumerable<XElement> xElements)
        {
            ChoGuard.ArgumentNotNull(xElements, "XmlElements");

            Init();
            _xElements = xElements;
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
                if (_xmlReader != null)
                    _xmlReader.Dispose();
                if (_sr != null)
                    _sr.Dispose();
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
                Configuration = new ChoXmlRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);
            Configuration.IsDynamicObject = Configuration.RecordType.IsDynamicType();
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoXmlReader<T> LoadXElements(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            var r = new ChoXmlReader<T>(xElements, configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static T LoadXElement(XElement xElement, ChoXmlRecordConfiguration configuration = null)
        {
            if (xElement == null) return default(T);

            return LoadXElements(new XElement[] { xElement }, configuration).FirstOrDefault();
        }

        public static ChoXmlReader<T> LoadText(string inputText, Encoding encoding = null, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoXmlReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            return r;
        }

        //internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoXmlRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        //{
        //    ChoXmlRecordReader rr = new ChoXmlRecordReader(recType, configuration);
        //    rr.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;
        //    return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        //}

        public IEnumerator<T> GetEnumerator()
        {
            if (_xElements == null)
            {
                InitXml();

                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                //if (_textReader != null)
                //    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xmlReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T)), () => Dispose()).GetEnumerator();
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xElements).GetEnumerator();
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
            if (_xElements == null)
            {
                InitXml();

                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                //if (_textReader != null)
                //    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xmlReader), rr);
                return dr;
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += membersDiscovered != null ? (o, e) => membersDiscovered(e.Value) : MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xElements), rr);
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

        public int Fill(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader());

            return dt.Rows.Count;
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
            foreach (var fn in Configuration.XmlRecordFieldConfigurations.Select(fc => fc.FieldName))
                bcp.ColumnMappings.Add(new SqlBulkCopyColumnMapping(fn, fn));
        }

        public void Bcp(string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            if (columnMappings == null)
                columnMappings = Configuration.XmlRecordFieldConfigurations.Select(fc => fc.FieldName)
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
                columnMappings = Configuration.XmlRecordFieldConfigurations.Select(fc => fc.FieldName)
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

        public ChoXmlReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoXmlReader<T> WithXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            ChoGuard.ArgumentNotNull(nsMgr, "XmlNamespaceManager");

            Configuration.NamespaceManager = nsMgr;
            return this;
        }

        public ChoXmlReader<T> WithXmlNamespace(string prefix, string uri)
        {
            Configuration.NamespaceManager.AddNamespace(prefix, uri);

            return this;
        }

        public ChoXmlReader<T> WithXPath(string xPath)
        {
            Configuration.XPath = xPath;
            return this;
        }

        public ChoXmlReader<T> UseXmlSerialization()
        {
            Configuration.UseXmlSerialization = true;
            return this;
        }

        public ChoXmlReader<T> ClearFields()
        {
            Configuration.XmlRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoXmlReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (field != null)
                return IgnoreField(field.GetFullyQualifiedMemberName());
            else
                return this;
        }

        public ChoXmlReader<T> IgnoreField(string fieldName)
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
                if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.XmlRecordFieldConfigurations.Remove(Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoXmlReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoXmlReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoXmlRecordFieldConfiguration fc = null;
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
                    if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.XmlRecordFieldConfigurations.Remove(Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, $"/{fnTrim}");
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.XmlRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoXmlReader<T> WithXmlElementField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            if (field == null)
                return this;

            return WithXmlElementField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
                valueConverter,
                itemConverter, customSerializer,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText, 
                nullValue, fieldTypeSelector);
        }

        public ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            return WithXmlElementField(name, fieldType, fieldValueTrimOption, fieldName,
                valueConverter,
                itemConverter, customSerializer,
                defaultValue, fallbackValue, encodeValue, null, formatText, nullValue, fieldTypeSelector);
        }

        private ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"/{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, fieldName, false, valueConverter, itemConverter,
                customSerializer, defaultValue,
                fallbackValue, encodeValue, formatText, nullValue, fieldTypeSelector);
        }

        public ChoXmlReader<T> WithXmlAttributeField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            if (field == null)
                return this;

            return WithXmlAttributeField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
                valueConverter,
                itemConverter, customSerializer,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText, nullValue, 
                fieldTypeSelector);
        }

        public ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            return WithXmlAttributeField(name, fieldType, fieldValueTrimOption, fieldName,
                        valueConverter,
                        itemConverter, customSerializer,
                        defaultValue, fallbackValue, encodeValue, null, formatText, nullValue, fieldTypeSelector);
        }

        private ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"/@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, fieldName, false, valueConverter,
                itemConverter, customSerializer, defaultValue, fallbackValue, encodeValue, formatText, nullValue, fieldTypeSelector);
        }

        public ChoXmlReader<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoXmlRecordFieldConfigurationMap> setup)
        {
            Configuration.MapRecordField(field.GetMemberName(), setup);
            return this;
        }

        public ChoXmlReader<T> WithField(string name, Action<ChoXmlRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
                Configuration.MapRecordField(name, mapper);
            return this;
        }

        public ChoXmlReader<T> WithField<TField>(Expression<Func<T, TField>> field, string xPath = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            bool isXmlAttribute = false, string fieldName = null, bool isArray = false,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), xPath, field.GetPropertyType(), fieldValueTrimOption, isXmlAttribute, fieldName, isArray,
                valueConverter,
                itemConverter, customSerializer,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), nullValue, fieldTypeSelector);
        }

        public ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null, bool isArray = false,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null,
            bool encodeValue = false, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            return WithField(name, xPath, fieldType, fieldValueTrimOption, isXmlAttribute, fieldName, isArray,
                valueConverter,
                itemConverter, customSerializer,
                defaultValue, fallbackValue,
                encodeValue, null, formatText, nullValue, fieldTypeSelector);
        }

        private ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null, bool isArray = false,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null,
            bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Func<XElement, Type> fieldTypeSelector = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }

                string fnTrim = name.NTrim();
                ChoXmlRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    Configuration.XmlRecordFieldConfigurations.Remove(fc);
                }
                else
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

                var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, xPath)
                {
                    FieldType = fieldType,
                    FieldValueTrimOption = fieldValueTrimOption,
                    IsXmlAttribute = isXmlAttribute,
                    FieldName = fieldName,
                    IsArray = isArray,
                    ValueConverter = valueConverter,
                    ItemConverter = itemConverter,
                    CustomSerializer = customSerializer,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    EncodeValue = encodeValue,
                    FormatText = formatText,
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
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName);
                    nfc.PropertyDescriptor = pd;
                    nfc.DeclaringMember = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }
                Configuration.XmlRecordFieldConfigurations.Add(nfc);
            }

            return this;
        }

        public ChoXmlReader<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFields();
            Configuration.MapRecordFields(Configuration.RecordType);
            return this;
        }

        public ChoXmlReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoXmlReader<T> Configure(Action<ChoXmlRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoXmlReader<T> Setup(Action<ChoXmlReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoXmlReader<T> WithMaxScanNodes(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoXmlReader<T> WithCustomRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.SupportsMultiRecordTypes = true;
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        #endregion Fluent API

        ~ChoXmlReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoXmlReader : ChoXmlReader<dynamic>
    {
        public ChoXmlReader(StringBuilder sb, ChoXmlRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }

        public ChoXmlReader(string filePath, string defaultNamespace, ChoXmlRecordConfiguration configuration = null)
            : base(filePath, defaultNamespace, configuration)
        {

        }

        public ChoXmlReader(string filePath, ChoXmlRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoXmlReader(TextReader txtReader, ChoXmlRecordConfiguration configuration = null)
            : base(txtReader, configuration)
        {
        }
        public ChoXmlReader(XmlReader xmlReader, ChoXmlRecordConfiguration configuration = null)
            : base(xmlReader, configuration)
        {
        }
        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public ChoXmlReader(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
            : base(xElements, configuration)
        {
        }

        public ChoXmlReader(XElement xElement, ChoXmlRecordConfiguration configuration = null)
            : base(xElement, configuration)
        {
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, string xPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return DeserializeText(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> DeserializeText(string inputText, Encoding encoding = null, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, string xPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return DeserializeText<T>(inputText, encoding, configuration, traceSwitch);
        }

        public static IEnumerable<T> DeserializeText<T>(string inputText, Encoding encoding = null, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, string xPath, Encoding encoding = null, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(string filePath, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, string xPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize<T>(filePath, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(string filePath, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, string xPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(TextReader textReader, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, string xPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize<T>(textReader, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(TextReader textReader, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, string xPath, TraceSwitch traceSwitch = null)
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<dynamic> Deserialize(Stream inStream, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration();

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, string xPath, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            var configuration = new ChoXmlRecordConfiguration();
            configuration.XPath = xPath;
            return Deserialize<T>(inStream, configuration, traceSwitch);
        }

        public static IEnumerable<T> Deserialize<T>(Stream inStream, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            if (configuration == null)
                configuration = new ChoXmlRecordConfiguration(typeof(T));

            if (configuration != null)
            {
                if (configuration.XPath.IsNullOrWhiteSpace())
                    configuration.XPath = "//";
            }
            return new ChoXmlReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<dynamic> Deserialize(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoXmlReader(xElements, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static IEnumerable<T> Deserialize<T>(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoXmlReader<T>(xElements, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public static dynamic Deserialize(XElement xElement, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return new ChoXmlReader(xElement, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static T Deserialize<T>(XElement xElement, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class, new()
        {
            return new ChoXmlReader<T>(xElement, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }
    }
}
