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
    public class ChoManifoldWriter : IDisposable
    {
        private StreamWriter _streamWriter;
        private bool _closeStreamOnDispose = false;
        private ChoManifoldRecordWriter _writer = null;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldWriter(string filePath, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoManifoldWriter(StreamWriter streamWriter, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = streamWriter;
        }

        public ChoManifoldWriter(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
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
                Configuration = new ChoManifoldRecordConfiguration();

            _writer = new ChoManifoldRecordWriter(typeof(object), Configuration);
        }

        public void Write(IEnumerable<object> records)
        {
            _writer.WriteTo(_streamWriter, records).Loop();
        }

        public void Write(object record)
        {
            _writer.WriteTo(_streamWriter, new object[] { record } ).Loop();
        }

        public static string ToText(IEnumerable records)
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldWriter(writer))
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        #region Fluent API

        public ChoManifoldWriter WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(Func<string, Type> recordSelector)
        {
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(int startIndex, int size, params Type[] recordTypes)
        {
            Configuration.RecordTypeConfiguration.StartIndex = startIndex;
            Configuration.RecordTypeConfiguration.Size = size;

            if (recordTypes != null)
            {
                foreach (var t in recordTypes)
                    Configuration.RecordTypeConfiguration.RegisterType(t);
            }
            return this;
        }

        #endregion Fluent API
    }
}
