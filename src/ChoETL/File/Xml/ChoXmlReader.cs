using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml;
using System.Xml.Linq;

namespace ChoETL
{
    public class ChoXmlReader<T> : ChoReader, IDisposable, IEnumerable<T>
        where T : class
    {
        //private TextReader _textReader;
        private TextReader _sr;
        private XmlReader _xmlReader;
        private IEnumerable<XElement> _xElements;
        private bool _closeStreamOnDispose = false;
        private Lazy<IEnumerator<T>> _enumerator = null;
        private CultureInfo _prevCultureInfo = null;
        private bool _clearFields = false;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlReader(ChoXmlRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoXmlReader(string filePath, string defaultNamespace)
		{
			ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

			Configuration = new ChoXmlRecordConfiguration();
			if (!defaultNamespace.IsNullOrWhiteSpace())
				Configuration.NamespaceManager.AddNamespace("", defaultNamespace);

			Init();

			_sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
			//InitXml();
			_closeStreamOnDispose = true;
		}

		private void InitXml()
		{
			_xmlReader = XmlReader.Create(_sr,
				new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
		}

		public ChoXmlReader(string filePath, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
			//InitXml();
			_closeStreamOnDispose = true;
        }

        public ChoXmlReader(TextReader textReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Configuration = configuration;
            Init();

            _sr = textReader;
			InitXml();
		}

		public ChoXmlReader(XmlReader xmlReader, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xmlReader, "XmlReader");

            Configuration = configuration;
            Init();

            _xmlReader = xmlReader;
        }

        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();

            if (inStream is MemoryStream)
                _sr = new StreamReader(inStream);
            else
                _sr = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
			//InitXml();
			_closeStreamOnDispose = true;
        }

        public ChoXmlReader(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(xElements, "XmlElements");

            Configuration = configuration;
            Init();
            _xElements = xElements;
        }

        public ChoXmlReader<T> Load(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Close();
            Init();
            _sr = new StreamReader(ChoPath.GetFullPath(filePath), Configuration.GetEncoding(filePath), false, Configuration.BufferSize);
			//InitXml();
			_closeStreamOnDispose = true;

            return this;
        }

        public ChoXmlReader<T> Load(TextReader textReader)
        {
            ChoGuard.ArgumentNotNull(textReader, "TextReader");

            Close();
            Init();
            _sr = textReader;
			//InitXml();
			_closeStreamOnDispose = false;

            return this;
        }

        public ChoXmlReader<T> Load(XmlReader xmlReader)
        {
            ChoGuard.ArgumentNotNull(xmlReader, "XmlReader");

            Close();
            Init();
            _xmlReader = xmlReader;
            _closeStreamOnDispose = false;

            return this;
        }

        public ChoXmlReader<T> Load(Stream inStream)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Close();
            Init();
            if (inStream is MemoryStream)
                _sr = new StreamReader(inStream);
            else
                _sr = new StreamReader(inStream, Configuration.GetEncoding(inStream), false, Configuration.BufferSize);
            _closeStreamOnDispose = true;

            return this;
        }

        public ChoXmlReader<T> Load(IEnumerable<XElement> xElements)
        {
            ChoGuard.ArgumentNotNull(xElements, "XmlElements");

            Init();
            _xElements = xElements;
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
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_closeStreamOnDispose)
            {
                if (_xmlReader != null)
                    _xmlReader.Dispose();
                if (_sr != null)
                    _sr.Dispose();
            }

            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                System.Threading.Thread.CurrentThread.CurrentCulture = _prevCultureInfo;

            _closeStreamOnDispose = false;
        }

        private void Init()
        {
            _enumerator = new Lazy<IEnumerator<T>>(() => GetEnumerator());
            if (Configuration == null)
                Configuration = new ChoXmlRecordConfiguration(typeof(T));
            else
                Configuration.RecordType = typeof(T);

            Configuration.RecordType = ResolveRecordType(Configuration.RecordType);
            _prevCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = Configuration.Culture;
        }

        public static ChoXmlReader<T> LoadXElements(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null)
        {
            var r = new ChoXmlReader<T>(xElements, configuration);
            r._closeStreamOnDispose = true;

            return r;
        }

        public static T LoadXElement(XElement xElement, ChoXmlRecordConfiguration configuration = null)
        {
            if (xElement == null) return default(T);

            return LoadXElements(new XElement[] { xElement }, configuration).FirstOrDefault();
        }

        public static ChoXmlReader<T> LoadText(string inputText, Encoding encoding = null, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            var r = new ChoXmlReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
            return r;
        }

        public IEnumerable<T> DeserializeText(string inputText, Encoding encoding = null, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(inputText.ToStream(encoding), configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(string filePath, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(filePath, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(TextReader textReader, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(textReader, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(Stream inStream, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(inStream, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public IEnumerable<T> Deserialize(IEnumerable<XElement> xElements, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(xElements, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch };
        }

        public T Deserialize(XElement xElement, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            if (configuration == null)
                configuration = Configuration;

            return new ChoXmlReader<T>(new XElement[] { xElement }, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch }.FirstOrDefault();
        }

        //internal static IEnumerator<object> LoadText(Type recType, string inputText, ChoXmlRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        //{
        //    ChoXmlRecordReader rr = new ChoXmlRecordReader(recType, configuration);
        //    rr.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;
        //    return rr.AsEnumerable(new StreamReader(inputText.ToStream(), encoding, false, bufferSize)).GetEnumerator();
        //}

        public IEnumerator<T> GetEnumerator()
        {
            if (_xElements == null)
            {
				InitXml();

				ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                //if (_textReader != null)
                //    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xmlReader).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var e = rr.AsEnumerable(_xElements).GetEnumerator();
                return ChoEnumeratorWrapper.BuildEnumerable<T>(() => e.MoveNext(), () => (T)ChoConvert.ChangeType<ChoRecordFieldAttribute>(e.Current, typeof(T))).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataReader AsDataReader()
        {
            if (_xElements == null)
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);
                //if (_textReader != null)
                //    _xmlReader = XmlReader.Create(_textReader, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }, new XmlParserContext(null, Configuration.NamespaceManager, null, XmlSpace.None));
                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xmlReader), rr);
                return dr;
            }
            else
            {
                ChoXmlRecordReader rr = new ChoXmlRecordReader(typeof(T), Configuration);

                rr.Reader = this;
                rr.TraceSwitch = TraceSwitch;
                rr.RowsLoaded += NotifyRowsLoaded;
                rr.MembersDiscovered += MembersDiscovered;
                var dr = new ChoEnumerableDataReader(rr.AsEnumerable(_xElements), rr);
                return dr;
            }
        }

        public DataTable AsDataTable(string tableName = null)
        {
            DataTable dt = tableName.IsNullOrWhiteSpace() ? new DataTable() : new DataTable(tableName);
            dt.Load(AsDataReader());
            return dt;
        }

        public int Fill(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentException("Missing datatable.");
            dt.Load(AsDataReader());

            return dt.Rows.Count;
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

        public ChoXmlReader<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoXmlReader<T> WithXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            ChoGuard.ArgumentNotNull(nsMgr, "XmlNamespaceManager");

            Configuration.NamespaceManager = nsMgr;
            return this;
        }

        public ChoXmlReader<T> WithXmlNamespace(string prefix, string uri)
        {
            Configuration.NamespaceManager.AddNamespace(prefix, uri);

            return this;
        }

        public ChoXmlReader<T> WithXPath(string xPath)
        {
            Configuration.XPath = xPath;
            return this;
        }

        public ChoXmlReader<T> UseXmlSerialization()
        {
            Configuration.UseXmlSerialization = true;
            return this;
        }

		public ChoXmlReader<T> ClearFields()
		{
			Configuration.XmlRecordFieldConfigurations.Clear();
			_clearFields = true;
			return this;
		}

		public ChoXmlReader<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
		{
			if (field != null)
				return IgnoreField(field.GetFullyQualifiedMemberName());
			else
				return this;
		}

		public ChoXmlReader<T> IgnoreField(string fieldName)
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
                if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.XmlRecordFieldConfigurations.Remove(Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
            }

            return this;
        }

		public ChoXmlReader<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
		{
			if (fields != null)
			{
				foreach (var field in fields)
					return WithField(field);
			}
			return this;
		}

		public ChoXmlReader<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
				PropertyDescriptor pd = null;
				ChoXmlRecordFieldConfiguration fc = null;
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
					if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
					{
						fc = Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
						Configuration.XmlRecordFieldConfigurations.Remove(Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
					}
					else
						pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

					var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, $"//{fnTrim}");
					nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
					nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
					if (pd != null)
					{
						if (nfc.FieldType == null)
							nfc.FieldType = pd.PropertyType;
					}

					Configuration.XmlRecordFieldConfigurations.Add(nfc);
				}
            }

            return this;
        }

		public ChoXmlReader<T> WithXmlElementField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null)
		{
			if (field == null)
				return this;

			return WithXmlElementField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
				valueConverter,
				itemConverter,
				defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText);
		}

		public ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null)
		{
			return WithXmlElementField(name, fieldType, fieldValueTrimOption, fieldName,
				valueConverter,
				itemConverter,
				defaultValue, fallbackValue, encodeValue, null, formatText);
		}

		private ChoXmlReader<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, fieldName, false, valueConverter, itemConverter, defaultValue, 
				fallbackValue, encodeValue, formatText);
        }

		public ChoXmlReader<T> WithXmlAttributeField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null)
		{
			if (field == null)
				return this;

			return WithXmlAttributeField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
				valueConverter,
				itemConverter,
				defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText);
		}

		public ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null)
		{
			return WithXmlAttributeField(name, fieldType, fieldValueTrimOption, fieldName,
						valueConverter,
						itemConverter,
						defaultValue, fallbackValue, encodeValue, null, formatText);
		}

		private ChoXmlReader<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, fieldName, false, valueConverter, itemConverter, defaultValue, fallbackValue, encodeValue, formatText);
        }

		public ChoXmlReader<T> WithField<TField>(Expression<Func<T, TField>> field, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
			bool isXmlAttribute = false, string fieldName = null, bool isArray = false, 
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null)
		{
			if (field == null)
				return this;

			return WithField(field.GetMemberName(), xPath, fieldType, fieldValueTrimOption, isXmlAttribute, fieldName, isArray,
				valueConverter,
				itemConverter,
				defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName());
		}

		public ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null, bool isArray = false,
			Func<object, object> valueConverter = null,
			Func<object, object> itemConverter = null,
			object defaultValue = null, object fallbackValue = null,
			bool encodeValue = false, string formatText = null)
		{
			return WithField(name, xPath, fieldType, fieldValueTrimOption, isXmlAttribute, fieldName, isArray,
				valueConverter,
				itemConverter,
				defaultValue, fallbackValue,
				encodeValue, null, formatText);
		}

		private ChoXmlReader<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null, bool isArray = false, 
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            object defaultValue = null, object fallbackValue = null,
            bool encodeValue = false, string fullyQualifiedMemberName = null, string formatText = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
					ClearFields();
					Configuration.MapRecordFields(Configuration.RecordType);
                }

				string fnTrim = name.NTrim();
				ChoXmlRecordFieldConfiguration fc = null;
				PropertyDescriptor pd = null;
				if (Configuration.XmlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
				{
					fc = Configuration.XmlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
					Configuration.XmlRecordFieldConfigurations.Remove(fc);
				}
				else
					pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);

				var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, xPath)
				{
					FieldType = fieldType,
					FieldValueTrimOption = fieldValueTrimOption,
					IsXmlAttribute = isXmlAttribute,
					FieldName = fieldName,
					IsArray = isArray,
					ValueConverter = valueConverter,
					ItemConverter = itemConverter,
					DefaultValue = defaultValue,
					FallbackValue = fallbackValue,
					EncodeValue = encodeValue,
                    FormatText = formatText
				};
				if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
				{
					nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
					nfc.DeclaringMember = fc != null ? fc.DeclaringMember : fullyQualifiedMemberName;
				}
				else
				{
					pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName);
					nfc.PropertyDescriptor = pd;
					nfc.DeclaringMember = fullyQualifiedMemberName;
				}
				if (pd != null)
				{
					if (nfc.FieldType == null)
						nfc.FieldType = pd.PropertyType;
				}
				Configuration.XmlRecordFieldConfigurations.Add(nfc);
            }

            return this;
        }

        public ChoXmlReader<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoXmlReader<T> Configure(Action<ChoXmlRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoXmlReader<T> Setup(Action<ChoXmlReader<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

		#endregion Fluent API

		~ChoXmlReader()
		{
			Dispose();
		}
	}

	public class ChoXmlReader : ChoXmlReader<dynamic>
    {
        public ChoXmlReader(string filePath, string defaultNamespace)
            : base(filePath, defaultNamespace)
        {

        }

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
        public ChoXmlReader(Stream inStream, ChoXmlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }
    }
}
