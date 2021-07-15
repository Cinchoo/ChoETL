using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class ChoManifoldReader : ChoReader, IDisposable, IEnumerable, IChoManifoldReader
    {
        private Lazy<TextReader> _textReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        private bool _isDisposed = false;
        public event EventHandler<ChoRecordConfigurationConstructArgs> AfterRecordConfigurationConstruct;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldReader(StringBuilder sb, ChoManifoldRecordConfiguration configuration = null) : this(new StringReader(sb.ToString()), configuration)
        {

        }

        public ChoManifoldReader(ChoManifoldRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoManifoldReader(string filePath, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoManifoldReader(TextReader textReader, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = new Lazy<TextReader>(() => textReader);
        }

        public ChoManifoldReader(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            if (inStream is MemoryStream)
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }
            _closeStreamOnDispose = true;
        }

        public ChoManifoldReader Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => new StreamReader(filePath, Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoManifoldReader Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = new Lazy<TextReader>(() => textReader);
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoManifoldReader Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _textReader = new Lazy<TextReader>(() => new StreamReader(inStream));
            else
            {
                _textReader = new Lazy<TextReader>(() =>
                {
                    if (Configuration.DetectEncodingFromByteOrderMarks == null)
                        return new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
                    else
                        return new StreamReader(inStream, Encoding.Default, Configuration.DetectEncodingFromByteOrderMarks.Value, Configuration.BufferSize);
                });
            }

            //_closeStreamOnDispose = true;

            return this;
        }

        public void Close()
        {
            Dispose();
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
            Dispose(false);
        }

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_textReader != null)
                {
                    _textReader.Value.Dispose();
                    _textReader = null;
                }
            }

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;

            _closeStreamOnDispose = false;

            if (!finalize)
                GC.SuppressFinalize(this);
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
            return rr.AsEnumerable(_textReader.Value).GetEnumerator();
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

        public static ChoManifoldReader LoadText(string inputText, ChoManifoldRecordConfiguration config, TraceSwitch traceSwitch = null)
        {
            return LoadText(inputText, null, config, traceSwitch);
        }

        private void NotifyRowsLoaded(object sender, ChoRowsLoadedEventArgs e)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
                ChoETLLog.Info(e.RowsLoaded.ToString("#,##0") + " records loaded.");
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
                object rec = null;
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

        public void RaiseAfterRecordConfigurationConstruct(Type recordType, ChoRecordConfiguration config)
        {
            EventHandler<ChoRecordConfigurationConstructArgs> afterRecordConfigurationConstruct = AfterRecordConfigurationConstruct;
            if (afterRecordConfigurationConstruct == null)
                return;

            var ea = new ChoRecordConfigurationConstructArgs(recordType, config);
            afterRecordConfigurationConstruct(this, ea);
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

        public ChoManifoldReader WithCustomRecordTypeCodeExtractor(Func<string, string> recordTypeCodeExtractor)
        {
            Configuration.RecordTypeCodeExtractor = recordTypeCodeExtractor;
            return this;
        }

        public ChoManifoldReader WithCustomRecordSelector(Func<object, Type> recordSelector)
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

        public ChoManifoldReader Configure(Action<ChoManifoldRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoManifoldReader Setup(Action<ChoManifoldReader> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API
    
        ~ChoManifoldReader()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    internal interface IChoManifoldReader
    {
        void RaiseAfterRecordConfigurationConstruct(Type recordType, ChoRecordConfiguration config);
    }
}
