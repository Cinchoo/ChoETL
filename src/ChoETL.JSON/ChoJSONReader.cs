using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoJSONReader<T> : ChoReader, IDisposable, IEnumerable<T>
        where T : class
    {
        private TextReader _textReader;
        private JsonTextReader _JSONReader;
        private IEnumerable<JToken> _jObjects;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONReader(ChoJSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _JSONReader = new JsonTextReader(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(TextReader textReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _textReader = textReader;
        }

        public ChoJSONReader(JsonTextReader JSONReader, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(JSONReader, "JSONReader");

            Configuration = configuration;
            Init();

            _JSONReader = JSONReader;
        }

        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoJSONReader(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(jObjects, "JObjects");

            Configuration = configuration;
            Init();
            _jObjects = jObjects;
        }

        public ChoJSONReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _JSONReader = new JsonTextReader(new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize));
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoJSONReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _textReader = textReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoJSONReader<T> Load(JsonTextReader JSONReader)
        {
            ChoGuard.ArgumentNotNull(JSONReader, "JSONReader");

            Close();
            Init();
            _JSONReader = JSONReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoJSONReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _textReader = new StreamReader(inStream);
            else
                _textReader = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoJSONReader<T> Load(IEnumerable<JToken> jObjects)
        {
            ChoGuard.ArgumentNotNull(jObjects, "JObjects");

            Init();
            _jObjects = jObjects;
            return this;
        }

        public void Close()
        {
            Dispose();
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
            {
                if (_textReader != null)
                    _textReader.Dispose();
                if (_JSONReader != null)
                    _JSONReader.Close();
            }

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;

            _closeStreamOnDispose = false;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public IEnumerable<T> DeserializeText(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(string filePath, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(TextReader textReader, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(Stream inStream, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(jObjects, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public T Deserialize(JToken jObject, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoJSONReader<T>(jObject, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        public static ChoJSONReader<T> LoadText(string inputText, Encoding encoding = null, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(inputText.ToStream(encoding), configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static ChoJSONReader<T> LoadJTokens(IEnumerable<JToken> jObjects, ChoJSONRecordConfiguration configuration = null)
        {
            var r = new ChoJSONReader<T>(jObjects, configuration);
            return r;
        }

        public static T LoadJToken(JToken jObject, ChoJSONRecordConfiguration configuration = null)
        {
            if (jObject == null) return default(T);

            return LoadJTokens(new JToken[] { jObject }, configuration).FirstOrDefault();
        }

        //internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize)
        //{
        //    ChoJSONRecordReader rr = new ChoJSONRecordReader(recType, configuration);
        //    rr.TraceSwitch = ChoETLFramework.TraceSwitchOff;
        //    return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        //}

        public IEnumerator<T> GetEnumerator()
        {
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = new JsonTextReader(_textReader);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_JSONReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_jObjects).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            if (_jObjects == null)
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                if (_textReader != null)
                    _JSONReader = new JsonTextReader(_textReader);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_JSONReader), rr);
                return dr;
            }
            else
            {
                ChoJSONRecordReader rr = new ChoJSONRecordReader(typeof(T), Configuration);
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_jObjects), rr);
                return dr;
            }
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }

        public void Fill(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader());
        }

        private void NotifyRowsLoaded(object sender, ChoRowsLoadedEventArgs e)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
            {
                if (!e.IsFinal)
                    ChoETLLog.Info(e.RowsLoaded.ToString("#,##0") + " records loaded.");
                else
                    ChoETLLog.Info("Total " + e.RowsLoaded.ToString("#,##0") + " records loaded.");
            }
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
                T rec = default(T);
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

        #region Fluent API

        public ChoJSONReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONReader<T> WithJSONPath(string jsonPath)
        {
            Configuration.JSONPath = jsonPath;
            return this;
        }

		public ChoJSONReader<T> ClearFields()
		{
			Configuration.JSONRecordFieldConfigurations.Clear();
			_clearFields = true;
			return this;
		}

		public ChoJSONReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
		{
			if (field != null)
				return IgnoreField(field.GetMemberName());
			else
				return this;
		}

		public ChoJSONReader<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                string fnTrim = null;
                if (!_clearFields)
                {
					ClearFields();
					Configuration.MapRecordFields(Configuration.RecordType);
                }
                fnTrim = fieldName.NTrim();
                if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
            }

            return this;
        }

		public ChoJSONReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
		{
			if (fields != null)
			{
				var x = fields.Select(f => f.GetMemberName()).ToArray();
				return WithFields(x);
			}
			return this;
		}

		public ChoJSONReader<T> WithFields(params string[] fieldsNames)
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
						ClearFields();
						Configuration.MapRecordFields(Configuration.RecordType);
                    }
                    fnTrim = fn.NTrim();
                    if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                        Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());

                    Configuration.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fnTrim, (string)null));
                }
            }

            return this;
        }

		public ChoJSONReader<T> WithField<TField>(Expression<Func<T, TField>> field, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null, Func<object, object> valueConverter = null,
			object defaultValue = null, object fallbackValue = null)
		{
			if (field == null)
				return this;

			return WithField(field.GetMemberName(), jsonPath, fieldType, fieldValueTrimOption, isJSONAttribute, fieldName, valueConverter,
				defaultValue, fallbackValue);
		}

		public ChoJSONReader<T> WithField(string name, string jsonPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isJSONAttribute = false, string fieldName = null, Func<object, object> valueConverter = null,
            object defaultValue = null, object fallbackValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
					ClearFields();
					Configuration.MapRecordFields(Configuration.RecordType);
                }

                string fnTrim = name.NTrim();
                jsonPath = jsonPath.IsNullOrWhiteSpace() ? null : jsonPath;

                if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());

                Configuration.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fnTrim, jsonPath) { FieldType = fieldType, FieldValueTrimOption = fieldValueTrimOption, FieldName = fieldName, ValueConverter = valueConverter,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue
                });
            }

            return this;
        }

        public ChoJSONReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoJSONReader<T> Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoJSONReader<T> Setup(Action<ChoJSONReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API
    }

    public class ChoJSONReader : ChoJSONReader<dynamic>
    {
        public ChoJSONReader(ChoJSONRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoJSONReader(string filePath, ChoJSONRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoJSONReader(TextReader txtReader, ChoJSONRecordConfiguration configuration = null)
            : base(txtReader, configuration)
        {
        }
        public ChoJSONReader(Stream inStream, ChoJSONRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
