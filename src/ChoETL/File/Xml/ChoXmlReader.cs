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
            _xmlReader = XmlReader.Create(_streamReader);
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
            _xmlReader = XmlReader.Create(_streamReader);
        }

        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamReader = new StreamReader(inStream, Configuration.Encoding, false, Configuration.BufferSize);
            _xmlReader = XmlReader.Create(_streamReader);
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
            ChoXmlRecordReader reader = new ChoXmlRecordReader(typeof(T), Configuration);
            reader.TraceSwitch = TraceSwitch;
            var e = reader.AsEnumerable(_xmlReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        //public IDataReader AsDataReader()
        //{
        //    ChoXmlRecordReader reader = new ChoXmlRecordReader(typeof(T), Configuration);
        //    reader.TraceSwitch = TraceSwitch;
        //    reader.LoadSchema(_streamReader);

        //    var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.CSVRecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
        //    return dr;
        //}

        //public DataTable AsDataTable(string tableName = null)
        //{
        //    DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
        //    dt.Load(AsDataReader());
        //    return dt;
        //}
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
