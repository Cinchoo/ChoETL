    using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVReader<T> : IDisposable, IEnumerable<T>
        where T : class
    {
        private StreamReader _streamReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVReader(StreamReader streamReader, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
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
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));
        }

        public static ChoCSVReader<T> LoadText(string inputText)
        {
            var r = new ChoCSVReader<T>(inputText.ToStream());
            r._closeStreamOnDispose = true;

            return r;
        }

        public IEnumerator<T> GetEnumerator()
        {
            ChoCSVRecordReader reader = new ChoCSVRecordReader(typeof(T), Configuration);
            var e = reader.AsEnumerable(_streamReader).GetEnumerator();
            return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            ChoCSVRecordReader reader = new ChoCSVRecordReader(typeof(T), Configuration);
            reader.LoadSchema(_streamReader);

            var dr = new ChoEnumerableDataReader(GetEnumerator().ToEnumerable(), Configuration.RecordFieldConfigurations.Select(i => new KeyValuePair<string, Type>(i.Name, i.FieldType)).ToArray());
            return dr;
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }
    }

    public class ChoCSVReader : ChoCSVReader<ExpandoObject>
    {
        public ChoCSVReader(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVReader(StreamReader streamReader, ChoCSVRecordConfiguration configuration = null)
            : base(streamReader, configuration)
        {
        }
        public ChoCSVReader(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
