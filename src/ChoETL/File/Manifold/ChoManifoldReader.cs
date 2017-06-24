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

namespace ChoETL
{
    public class ChoManifoldReader : ChoReader, IDisposable, IEnumerable
    {
        private TextReader _textReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldReader(string filePath, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoManifoldReader(TextReader textReader, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = textReader;
        }

        public ChoManifoldReader(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public object Read()
        {
            if (_enumerator.Value.MoveNext())
                return _enumerator.Value.Current;
            else
                return null;
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _textReader.Dispose();

            System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoManifoldRecordConfiguration();
            else
                Configuration.RecordType = typeof(object);

            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public IEnumerator GetEnumerator()
        {
            ChoManifoldRecordReader rr = new ChoManifoldRecordReader(Configuration);
            rr.Reader = this;
            rr.TraceSwitch = TraceSwitch;
            rr.RowsLoaded += NotifyRowsLoaded;
            return rr.AsEnumerable(_textReader).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static ChoManifoldReader LoadText(string inputText, Encoding encoding = null, ChoManifoldRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoManifoldReader(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            r._closeStreamOnDispose = true;

            return r;
        }

        private void NotifyRowsLoaded(object sender, ChoRowsLoadedEventArgs e)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
                Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " records loaded.");
            else
                rowsLoadedEvent(this, e);
        }

        #region Fluent API

        public ChoManifoldReader NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoManifoldReader WithFirstLineHeader(bool ignoreHeader = false)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            Configuration.FileHeaderConfiguration.IgnoreHeader = ignoreHeader;
            return this;
        }

        public ChoManifoldReader WithCustomRecordSelector(Func<string, Type> recordSelector)
        {
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        public ChoManifoldReader WithRecordSelector(int startIndex, int size, params Type[] recordTypes)
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
