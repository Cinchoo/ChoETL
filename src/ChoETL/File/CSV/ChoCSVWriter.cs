using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVWriter<T> : IDisposable
        where T : class
    {
        private StreamWriter _streamWriter;
        private bool _closeStreamOnDispose = false;
        private ChoCSVRecordWriter _writer = null;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVWriter(StreamWriter streamWriter, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = streamWriter;
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _streamWriter.Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));

            _writer = new ChoCSVRecordWriter(typeof(T), Configuration);
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.WriteTo(_streamWriter, records).Loop();
        }

        public void Write(T record)
        {
            _writer.WriteTo(_streamWriter, new T[] { record } ).Loop();
        }
    }

    public class ChoCSVWriter : ChoCSVWriter<ExpandoObject>
    {
        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVWriter(StreamWriter streamWriter, ChoCSVRecordConfiguration configuration = null)
            : base(streamWriter, configuration)
        {
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
