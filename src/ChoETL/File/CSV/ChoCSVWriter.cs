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
        private TextWriter _txtWriter;
        private bool _closeStreamOnDispose = false;

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

            _txtWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVWriter(TextWriter txtWriter, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(txtWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _txtWriter = txtWriter;
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            _txtWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _txtWriter.Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));
        }

        public void Write(IEnumerable<T> records)
        {
            ChoCSVRecordWriter writer = new ChoCSVRecordWriter(typeof(T), Configuration);
            writer.WriteTo(_txtWriter, records).Loop();
        }
    }

    public class ChoCSVWriter : ChoCSVWriter<ExpandoObject>
    {
        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVWriter(TextWriter txtWriter, ChoCSVRecordConfiguration configuration = null)
            : base(txtWriter, configuration)
        {
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
