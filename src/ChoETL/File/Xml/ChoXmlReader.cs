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

namespace ChoETL
{
    public class ChoXmlReader<T> : IDisposable, IEnumerable<T>
        where T : class
    {
        private StreamReader _streamReader;
        private XmlReader _xmlReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        internal TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlReader(string filePath, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoXmlReader(TextReader txtReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(txtReader, "TextReader");

            Configuration = configuration;
            Init();

            _xmlReader = XmlReader.Create(txtReader);
        }

        public ChoXmlReader(XmlReader xmlReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xmlReader, "XmlReader");

            Configuration = configuration;
            Init();

            _xmlReader = xmlReader;
        }

        public ChoXmlReader(StreamReader streamReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamReader = new StreamReader(inStream, Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
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
                _streamReader.Dispose();

            System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoXmlRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoXmlReader<T> LoadText(string inputText, ChoXmlRecordConfiguration configuration = null)
        {
            var r = new ChoXmlReader<T>(inputText.ToStream(), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoXmlRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoXmlRecordReader reader = new ChoXmlRecordReader(recType, configuration);
            reader.TraceSwitch = ChoETLFramework.TraceSwitchOff;
            return reader.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_xmlReader == null)
                _xmlReader = XmlReader.Create(_streamReader, new XmlReaderSettings(), new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));

            ChoXmlRecordReader reader = new ChoXmlRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            reader.RowsLoaded += NotifyRowsLoaded;
            var e = reader.AsEnumerable(_xmlReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoXmlRecordReader reader = new ChoXmlRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            reader.LoadSchema(_streamReader);
            reader.RowsLoaded += NotifyRowsLoaded;
            var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.XmlRecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
            return dr;
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
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " records loaded.");
                else
                    Console.WriteLine("Total " + e.RowsLoaded.ToString("#,##0") + " records loaded.");
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

        public ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, fieldName);
        }

        public ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, fieldName);
        }

        public ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.XmlRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                string fnTrim = name.NTrim();
                fieldType = fieldType == null ? typeof(string) : fieldType;
                xPath = xPath.IsNullOrWhiteSpace() ? $"//{fnTrim}" : xPath;

                Configuration.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(fnTrim, xPath) { FieldType = fieldType, FieldValueTrimOption = fieldValueTrimOption, IsXmlAttribute = isXmlAttribute, FieldName = fieldName });
            }

            return this;
        }

        public ChoXmlReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        #endregion Fluent API
    }

    public class ChoXmlReader : ChoXmlReader<ExpandoObject>
    {
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
        public ChoXmlReader(StreamReader streamReader, ChoXmlRecordConfiguration configuration = null)
            : base(streamReader, configuration)
        {
        }
        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
