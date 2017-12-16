using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ChoETL
{
    public class ChoXmlReader<T> : ChoReader, IDisposable, IEnumerable<T>
        where T : class
    {
        private TextReader _textReader;
        private XmlReader _xmlReader;
        private IEnumerable<XElement> _xElements;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlReader(string filePath, string defaultNamespace)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = new ChoXmlRecordConfiguration();
            if (!defaultNamespace.IsNullOrWhiteSpace())
                Configuration.NamespaceManager.AddNamespace("", defaultNamespace);

            Init();

            _xmlReader = XmlReader.Create(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize),
                new XmlReaderSettings(), new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
            _closeStreamOnDispose = true;
        }

        public ChoXmlReader(string filePath, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _xmlReader = XmlReader.Create(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize),
                new XmlReaderSettings(), new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
            _closeStreamOnDispose = true;
        }

        public ChoXmlReader(TextReader txtReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(txtReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = txtReader;
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
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoXmlReader(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xElements, "XmlElements");

            Configuration = configuration;
            Init();
            _xElements = xElements;
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
            if (_closeStreamOnDispose)
            {
                if (_textReader != null)
                    _textReader.Dispose();
                if (_xmlReader != null)
                    _xmlReader.Dispose();
            }

            System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoXmlRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoXmlReader<T> LoadXElements(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            var r = new ChoXmlReader<T>(xElements, configuration);
            r._closeStreamOnDispose = true;

            return r;
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
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings(), new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xmlReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xElements).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            if (_xElements == null)
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings(), new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xmlReader), rr);
                return dr;
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xElements), rr);
                return dr;
            }
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
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

        public ChoXmlReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        Configuration.XmlRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }
                    fnTrim = fn.NTrim();
                    Configuration.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(fnTrim, $"//{fnTrim}"));
                }

            }

            return this;
        }

        public ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, fieldName, false, valueConverter, itemConverter, defaultValue, fallbackValue);
        }

        public ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, fieldName, false, valueConverter, itemConverter, defaultValue, fallbackValue);
        }

        public ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null, bool isArray = false, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.XmlRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                string fnTrim = name.NTrim();
                xPath = xPath.IsNullOrWhiteSpace() ? $"//{fnTrim}" : xPath;

                Configuration.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(fnTrim, xPath) { FieldType = fieldType,
                    FieldValueTrimOption = fieldValueTrimOption, IsXmlAttribute = isXmlAttribute, FieldName = fieldName, IsArray = isArray,
                    ValueConverter = valueConverter,
                    ItemConverter = itemConverter,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue
                });
            }

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

        #endregion Fluent API
    }

    public class ChoXmlReader : ChoXmlReader<dynamic>
    {
        public ChoXmlReader(string filePath, string defaultNamespace)
            : base(filePath, defaultNamespace)
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
    }
}
