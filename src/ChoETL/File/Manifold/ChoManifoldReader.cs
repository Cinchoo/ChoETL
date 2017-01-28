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
    public class ChoManifoldReader : IDisposable, IEnumerable
    {
        private StreamReader _streamReader;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;

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

            _streamReader = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.Encoding, false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoManifoldReader(StreamReader streamReader, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamReader, "StreamReader");

            Configuration = configuration;
            Init();

            _streamReader = streamReader;
        }

        public ChoManifoldReader(Stream inStream, ChoManifoldRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamReader = new StreamReader(inStream, Configuration.Encoding, false, Configuration.BufferSize);
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
                _streamReader.Dispose();

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
            ChoManifoldRecordReader reader = new ChoManifoldRecordReader(Configuration);
            return reader.AsEnumerable(_streamReader).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static ChoManifoldReader LoadText(string inputText, ChoManifoldRecordConfiguration configuration = null)
        {
            var r = new ChoManifoldReader(inputText.ToStream(), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        #region Fluent API

        public ChoManifoldReader WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoManifoldReader WithRecordSelector(Func<string, Type> recordSelector)
        {
            Configuration.RecordSelector = recordSelector;
            return this;
        }

        #endregion Fluent API
    }
}
