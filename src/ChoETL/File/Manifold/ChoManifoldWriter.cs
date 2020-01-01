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
    public class ChoManifoldWriter : ChoWriter, IDisposable
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoManifoldRecordWriter _writer = null;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }


        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldWriter(StringBuilder sb, ChoManifoldRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoManifoldWriter(ChoManifoldRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoManifoldWriter(string filePath, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoManifoldWriter(TextWriter textWriter, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoManifoldWriter(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            if (inStream is MemoryStream)
                _textWriter = new StreamWriter(inStream);
            else
                _textWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public void Close()
        {
            Dispose();
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
                if (_textWriter != null)
                    _textWriter.Dispose();
            }

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoManifoldRecordConfiguration();

            _writer = new ChoManifoldRecordWriter(Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            foreach (object rec in records)
                _writer.WriteTo(_textWriter, new object[] { rec }).Loop();
        }

        public void Write(object record)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo(_textWriter, new object[] { record }).Loop();
        }

        public static string ToText(object record, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(ChoEnumerable.AsEnumerable<object>(record), traceSwitch);
        }


        public static string ToTextAll(IEnumerable records, TraceSwitch traceSwitch = null)
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldWriter(writer) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        private void NotifyRowsWritten(object sender, ChoRowsWrittenEventArgs e)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                Console.WriteLine(e.RowsWritten.ToString("#,##0") + " records written.");
            else
                rowsWrittenEvent(this, e);
        }

        #region Fluent API

        public ChoManifoldWriter NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoManifoldWriter WithFirstLineHeader()
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(Func<object, Type> recordSelector)
        {
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        public ChoManifoldWriter WithRecordSelector(Func<string, string> recordTypeCodeExtractor)
        {
            Configuration.RecordTypeCodeExtractor = recordTypeCodeExtractor;
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
        public ChoManifoldWriter Configure(Action<ChoManifoldRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoManifoldWriter Setup(Action<ChoManifoldWriter> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API

        ~ChoManifoldWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }
}
