using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ChoETL
{
    public class ChoXmlWriter<T> : ChoWriter, IChoSerializableWriter, IDisposable
        where T : class
    {
        private bool _isDisposed = false;
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoXmlRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoXmlWriter(StringBuilder sb, ChoXmlRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoXmlWriter(ChoXmlRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoXmlWriter(string filePath, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoXmlWriter(TextWriter textWriter, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoXmlWriter(Stream inStream, ChoXmlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            if (inStream is MemoryStream)
                _textWriter = new StreamWriter(inStream);
            else
                _textWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            //_closeStreamOnDispose = true;
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
            if (_writer != null)
                _writer.EndWrite(_textWriter);
            if (_closeStreamOnDispose)
            {
                if (_textWriter != null)
                    _textWriter.Dispose();
            }
            else
            {
                if (_textWriter != null)
                    _textWriter.Flush();
            }

            if (!finalize)
                GC.SuppressFinalize(this);
        }
        public void Close()
        {
            Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoXmlRecordConfiguration(typeof(T));

            _writer = new ChoXmlRecordWriter(typeof(T), Configuration);
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo(_textWriter, records).Loop();
        }

        public void Write(T record)
        {
            _writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            if (record != null && !record.GetType().IsSimple() && !record.GetType().IsDynamicType() && record is IList)
            {
                if (record is ArrayList)
                    _writer.ElementType = typeof(object);

                _writer.WriteTo(_textWriter, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop();
            }
            else if (record != null && (!record.GetType().IsDynamicType() && record is IDictionary))
            {
                _writer.WriteTo(_textWriter, ((IEnumerable)record).AsTypedEnumerable<T>()).Loop();
            }
            else
                _writer.WriteTo(_textWriter, new T[] { record }).Loop();
        }

        public static string ToText<TRec>(TRec record, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string xpath = null)
            where TRec : class
        {
            if (configuration == null)
            {
                configuration = new ChoXmlRecordConfiguration();
                configuration.IgnoreRootName = true;
                configuration.RootName = null;
                configuration.IgnoreNodeName = true;
            }

            if (record != null)
            {
                if (configuration.NodeName.IsNullOrWhiteSpace())
                {
                    ChoDynamicObject rec1 = record as ChoDynamicObject;
                    if (rec1 != null)
                    {
                        if (rec1.DynamicObjectName != ChoDynamicObject.DefaultName)
                        {
                            configuration.NodeName = rec1.DynamicObjectName;
                        }
                        else
                        {
                            //configuration.IgnoreNodeName = true;
                            //configuration.NodeName = null;
                        }
                    }
                    else
                    {
                        XmlRootAttribute root = ChoType.GetCustomAttribute<XmlRootAttribute>(record.GetType(), false);
                        string nodeName = "XElement";
                        if (root != null && !root.ElementName.IsNullOrWhiteSpace())
                            nodeName = root.ElementName.Trim();
                        else
                            nodeName = record.GetType().Name;

                        configuration.NodeName = nodeName;
                    }
                }
            }

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                //parser.Configuration.XPath = xpath;

                if (record != null)
                    parser.Write(ChoEnumerable.AsEnumerable<TRec>(record));

                parser.Close();

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }


        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string xpath = null)
            where TRec : class
        {
            if (records == null) return null;

            var pe = new ChoPeekEnumerator<TRec>(records, (Func<TRec, bool?>)null);
            if (configuration == null)
            {
                configuration = new ChoXmlRecordConfiguration();
                configuration.IgnoreRootName = false;
            }

            TRec record = pe.Peek;

            if (record != null)
            {
                if (configuration.NodeName.IsNullOrWhiteSpace())
                {
                    ChoDynamicObject rec1 = record as ChoDynamicObject;
                    if (rec1 != null)
                    {
                        configuration.NodeName = rec1.DynamicObjectName;
                        if (configuration.RootName.IsNullOrWhiteSpace())
                            configuration.RootName = configuration.NodeName.ToPlural();
                    }
                    else
                    {
                        XmlRootAttribute root = ChoType.GetCustomAttribute<XmlRootAttribute>(record.GetType(), false);
                        string nodeName = "XElement";
                        if (root != null && !root.ElementName.IsNullOrWhiteSpace())
                            nodeName = root.ElementName.Trim();
                        else
                            nodeName = record.GetType().Name;

                        if (configuration.RootName.IsNullOrWhiteSpace())
                            configuration.RootName = nodeName.ToPlural();
                        configuration.NodeName = nodeName;
                    }
                }
            }
            else
            {
                if (configuration.RootName.IsNullOrWhiteSpace())
                    configuration.RootName = "Root";
            }

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                //parser.Configuration.XPath = xpath;

                parser.Write(pe.ToEnumerable());

                parser.Close();

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        internal static string ToText(object rec, ChoXmlRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            ChoXmlRecordWriter writer = new ChoXmlRecordWriter(rec.GetType(), configuration);
            writer.TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitchOff : traceSwitch;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var sw = new StreamWriter(stream, configuration.Encoding, configuration.BufferSize))
            {
                writer.WriteTo(sw, new object[] { rec }).Loop();
                sw.Flush();
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

        public ChoXmlWriter<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoXmlWriter<T> WithXmlNamespaceManager(XmlNamespaceManager nsMgr)
        {
            ChoGuard.ArgumentNotNull(nsMgr, "XmlNamespaceManager");

            Configuration.NamespaceManager = nsMgr;
            return this;
        }

        public ChoXmlWriter<T> WithXmlNamespace(string prefix, string uri)
        {
            Configuration.NamespaceManager.AddNamespace(prefix, uri);

            return this;
        }

        public ChoXmlWriter<T> WithDefaultXmlNamespace(string prefix, string uri)
        {
            WithXmlNamespace(prefix, uri);
            WithDefaultNamespacePrefix(prefix);
            return this;
        }

        public ChoXmlWriter<T> WithDefaultNamespacePrefix(string prefix)
        {
            Configuration.DefaultNamespacePrefix = prefix;

            return this;
        }

        public ChoXmlWriter<T> WithXPath(string xPath)
        {
            Configuration.XPath = xPath;
            return this;
        }

        public ChoXmlWriter<T> ClearFields()
        {
            Configuration.XmlRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoXmlWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (field != null)
                return IgnoreField(field.GetFullyQualifiedMemberName());
            else
                return this;
        }

        public ChoXmlWriter<T> IgnoreField(string fieldName)
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
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoXmlWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoXmlWriter<T> WithFields(params string[] fieldsNames)
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

                    var nfc = new ChoXmlRecordFieldConfiguration(fnTrim, $"/{fnTrim}");
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;

                    Configuration.XmlRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoXmlWriter<T> WithXmlElementField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithXmlElementField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
                valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoXmlWriter<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, string formatText = null,
            string nullValue = null)
        {
            return WithXmlElementField(name, fieldType, fieldValueTrimOption,
                fieldName, valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, null, formatText, nullValue);
        }

        private ChoXmlWriter<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, 
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"/{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, false, fieldName, valueConverter, customSerializer, isNullable, defaultValue, fallbackValue, 
                encodeValue, fullyQualifiedMemberName, formatText, nullValue);
        }

        public ChoXmlWriter<T> WithXmlAttributeField<TField>(Expression<Func<T, TField>> field, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithXmlAttributeField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName,
                valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoXmlWriter<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, string formatText = null,
            string nullValue = null)
        {
            return WithXmlAttributeField(name, fieldType, fieldValueTrimOption, fieldName, valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, null, formatText, nullValue);
        }

        private ChoXmlWriter<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, 
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"/@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, false, fieldName, valueConverter, customSerializer, isNullable, defaultValue, fallbackValue, 
                encodeValue, fullyQualifiedMemberName, formatText, nullValue);
        }

        public ChoXmlWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoXmlRecordFieldConfigurationMap> setup)
        {
            Configuration.MapRecordField(field.GetMemberName(), setup);
            return this;
        }

        public ChoXmlWriter<T> WithField(string name, Action<ChoXmlRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
                Configuration.MapRecordField(name, mapper);
            return this;
        }

        public ChoXmlWriter<T> WithField<TField>(Expression<Func<T, TField>> field, string xPath = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            bool isXmlAttribute = false, bool isAnyXmlNode = false, string fieldName = null,
            Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = false, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), xPath, field.GetPropertyType(), fieldValueTrimOption, isXmlAttribute, isAnyXmlNode, fieldName,
                valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoXmlWriter<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            bool isXmlAttribute = false, bool isAnyXmlNode = false, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, string formatText = null,
            string nullValue = null)
        {
            return WithField(name, xPath, fieldType, fieldValueTrimOption, isXmlAttribute, isAnyXmlNode, fieldName, valueConverter, customSerializer, isNullable,
                defaultValue, fallbackValue, encodeValue, null, formatText, nullValue);
        }

        private ChoXmlWriter<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, 
            bool isXmlAttribute = false, bool isAnyXmlNode = false, string fieldName = null, Func<object, object> valueConverter = null, 
            Func<object, object> customSerializer = null,
            bool isNullable = false,
            object defaultValue = null, object fallbackValue = null, bool encodeValue = true, 
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null)
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
                xPath = xPath.IsNullOrWhiteSpace() ? $"/{fnTrim}" : xPath;

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
                    ValueConverter = valueConverter,
                    CustomSerializer = customSerializer,
                    IsNullable = isNullable,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    EncodeValue = encodeValue,
                    FormatText = formatText,
                    IsAnyXmlNode = isAnyXmlNode,
                    NullValue = nullValue
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

                Configuration.XmlRecordFieldConfigurations.Add(nfc);
            }

            return this;
        }

        public ChoXmlWriter<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFields();
            Configuration.MapRecordFields(Configuration.RecordType);
            return this;
        }

        public ChoXmlWriter<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoXmlWriter<T> Configure(Action<ChoXmlRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }
        public ChoXmlWriter<T> Setup(Action<ChoXmlWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoXmlWriter<T> MapRecordFields<T1>()
        {
            Configuration.MapRecordFields<T1>();
            return this;
        }

        public ChoXmlWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
                Configuration.MapRecordFields(recordType);

            return this;
        }

        #endregion Fluent API

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            //int ordinal = 0;
            if (Configuration.XmlRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                int startIndex = 0;
                int fieldLength = 0;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    var obj = new ChoXmlRecordFieldConfiguration(colName, xPath: null);
                    Configuration.XmlRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.XmlRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, dr[fc.Name]);
                }

                Write(expando);
            }
        }

        public void Write(DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");

            DataTable schemaTable = dt;
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            string rootName = dt.DataSet == null || dt.DataSet.DataSetName.IsNullOrWhiteSpace() ? "Root" : dt.DataSet.DataSetName;
            string elementName = dt.TableName.IsNullOrWhiteSpace() ? "XElement" : dt.TableName;
            if (Configuration.XPath.IsNullOrWhiteSpace())
                Configuration.XPath = "{0}/{1}".FormatString(rootName, elementName);

            if (Configuration.XmlRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                int startIndex = 0;
                int fieldLength = 0;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    var obj = new ChoXmlRecordFieldConfiguration(colName, xPath: null);
                    Configuration.XmlRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.XmlRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
            }
        }

        ~ChoXmlWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoXmlWriter : ChoXmlWriter<dynamic>
    {

        public ChoXmlWriter(StringBuilder sb, ChoXmlRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoXmlWriter(ChoXmlRecordConfiguration configuration = null)
            : base(configuration)
        {
        }
        public ChoXmlWriter(string filePath, ChoXmlRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoXmlWriter(TextWriter textWriter, ChoXmlRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoXmlWriter(Stream inStream, ChoXmlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll<dynamic>(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToTextAll<T>(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText<dynamic>(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToText<T>(record, configuration, traceSwitch);
        }
    }

    public interface IChoRecordFieldSerializable
    {
        bool RecordFieldSerialize(object record, long index, string propName, ref object value);
        bool RecordFieldDeserialize(object record, long index, string propName, ref object value);
    }

    public interface IChoSerializableWriter
    {
        event EventHandler<ChoRecordFieldSerializeEventArgs> RecordFieldSerialize;
        bool RaiseRecordFieldSerialize(object record, long index, string propName, ref object value);
    }

    public interface IChoSerializableReader
    {
        event EventHandler<ChoRecordFieldSerializeEventArgs> RecordFieldDeserialize;
        bool RaiseRecordFieldDeserialize(object record, long index, string propName, ref object value);
    }

    public class ChoRecordFieldSerializeEventArgs
    {
        public string PropertyName
        {
            get;
            internal set;
        }
        public object Record
        {
            get;
            internal set;
        }
        public long Index
        {
            get;
            internal set;
        }
        public object Source
        {
            get;
            set;
        }
        public bool Handled
        {
            get;
            set;
        }
    }
}
