using Newtonsoft.Json;
using SharpYaml.Serialization;
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

namespace ChoETL
{
    public class ChoYamlWriter<T> : ChoWriter, IChoSerializableWriter, IDisposable
        //where T : class
    {
        private Lazy<TextWriter> _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoYamlRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoYamlRecordConfiguration Configuration
        {
            get;
            private set;
        }
        
        public ChoYamlWriter(StringBuilder sb, ChoYamlRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoYamlWriter(ChoYamlRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoYamlWriter(string filePath, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new Lazy<TextWriter>(() => new StreamWriter(filePath, Configuration.Append, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoYamlWriter(TextWriter textWriter, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = new Lazy<TextWriter>(() => textWriter);
        }

        public ChoYamlWriter(Stream inStream, ChoYamlRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            if (inStream is MemoryStream)
                _textWriter = new Lazy<TextWriter>(() => new StreamWriter(inStream));
            else
                _textWriter = new Lazy<TextWriter>(() => new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize));
            //_closeStreamOnDispose = true;
        }

        public void Flush()
        {
            if (_textWriter != null)
                _textWriter.Value.Flush();
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
            if (_writer != null && _textWriter != null)
                _writer.EndWrite(_textWriter.Value);

            if (_closeStreamOnDispose)
            {
                if (_textWriter != null)
                    _textWriter.Value.Dispose();
            }
            else
            {
                if (_textWriter != null)
                    _textWriter.Value.Flush();
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
            var recordType = typeof(T).GetUnderlyingType();
            if (Configuration == null)
                Configuration = new ChoYamlRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            _writer = new ChoYamlRecordWriter(recordType, Configuration);
            _writer.Writer = this;
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            //_writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            _writer.WriteTo(_textWriter.Value, records.OfType<object>()).Loop();
        }

        public void Write(T record)
        {
            if (record is DataTable)
            {
                Write(record as DataTable);
                return;
            }
            else if (record is IDataReader)
            {
                Write(record as IDataReader);
                return;
            }

            //_writer.Writer = this;
            _writer.TraceSwitch = TraceSwitch;
            //if (record != null && !record.GetType().IsSimple() && !record.GetType().IsDynamicType() && record is IList)
            if (record is ArrayList)
            {
                if (Configuration.SingleDocument == null)
                    Configuration.SingleDocument = true;
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>().OfType<object>()).Loop();
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                if (Configuration.SingleDocument == null)
                    Configuration.SingleDocument = true;
                _writer.WriteTo(_textWriter.Value, new object[] { record }).Loop();
            }
            else
            {
                if (Configuration.SingleDocument == null) Configuration.SingleDocument = true;
                _writer.WriteTo(_textWriter.Value, new object[] { record }).Loop();
            }
        }

        public static string ToText<TRec>(TRec record, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string yamlPath = null)
        {
            if (record is DataTable)
            {
                StringBuilder xml = new StringBuilder();
                using (var w = new ChoYamlWriter(xml, configuration))
                    w.Write(record as DataTable);
                return xml.ToString();
            }
            else if (record is IDataReader)
            {
                StringBuilder xml = new StringBuilder();
                using (var w = new ChoYamlWriter(xml, configuration))
                    w.Write(record as IDataReader);
                return xml.ToString();
            }

            if (configuration == null)
                configuration = new ChoYamlRecordConfiguration(typeof(TRec));

            if (configuration.SingleDocument == null) configuration.SingleDocument = true;
            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch, yamlPath);
        }


        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string yamlPath = null)
        {
            if (records == null) return null;

            if (typeof(DataTable).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder json = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    using (var w = new ChoYamlWriter(json, configuration))
                    {
                        w.Write(dt);
                    }
                }

                return json.ToString();
            }
            else if (typeof(IDataReader).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder json = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    using (var w = new ChoYamlWriter(json, configuration))
                    {
                        w.Write(dt);
                    }
                }

                return json.ToString();
            }


            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoYamlWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Configuration.YamlPath = yamlPath;

                parser.Write(records);
                parser.Close();
                writer.Flush();
                
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        internal static string ToText(object rec, ChoYamlRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            if (rec is DataTable)
            {
                StringBuilder json = new StringBuilder();
                using (var w = new ChoYamlWriter(json, configuration))
                {
                    w.Write(rec as DataTable);
                }
                return json.ToString();
            }
            else if (rec is IDataReader)
            {
                StringBuilder json = new StringBuilder();
                using (var w = new ChoYamlWriter(json, configuration))
                {
                    w.Write(rec as IDataReader);
                }
                return json.ToString();
            }

            ChoYamlRecordWriter writer = new ChoYamlRecordWriter(rec.GetType(), configuration);
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

        public ChoYamlWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoYamlWriter<T> WithTagMapping(string tagName, Type tagType, bool isAlias = false)
        {
            Configuration.WithTagMapping(tagName, tagType, isAlias);
            return this;
        }

        public ChoYamlWriter<T> UseYamlSerialization(bool flag = true)
        {
            Configuration.UseYamlSerialization = flag;
            return this;
        }

        public ChoYamlWriter<T> ReuseSerializerObject(bool flag = true)
        {
            Configuration.ReuseSerializerObject = flag;
            return this;
        }

        public ChoYamlWriter<T> YamlSerializerSettings(Action<SerializerSettings> settings)
        {
            settings?.Invoke(Configuration.YamlSerializerSettings);
            return this;
        }

        public ChoYamlWriter<T> NullValueHandling(ChoNullValueHandling value = ChoNullValueHandling.Default)
        {
            Configuration.NullValueHandling = value;
            return this;
        }

        public ChoYamlWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoYamlWriter<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoYamlWriter<T> SingleDocument(bool flag = true)
        {
            Configuration.SingleDocument = flag;
            return this;
        }

        public ChoYamlWriter<T> WithMaxScanNodes(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoYamlWriter<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoYamlWriter<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        private ChoYamlWriter<T> ClearFieldsIf()
        {
            if (!_clearFields)
            {
                Configuration.ClearFields();
                _clearFields = true;
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            return this;
        }

        public ChoYamlWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (!_clearFields)
            {
                ClearFields();
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoYamlWriter<T> IgnoreField(string fieldName)
        {
            if (!fieldName.IsNullOrWhiteSpace())
            {
                ClearFieldsIf();
                string fnTrim = null;
                fnTrim = fieldName.NTrim();
                Configuration.IgnoreField(fnTrim);
            }

            return this;
        }

        public ChoYamlWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoYamlWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoYamlRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    ClearFieldsIf();
                    fnTrim = fn.NTrim();
                    if (Configuration.YamlRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.YamlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.YamlRecordFieldConfigurations.Remove(Configuration.YamlRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoYamlRecordFieldConfiguration(fnTrim, (string)null);
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.YamlRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoYamlWriter<T> WithField(string name, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordTypeInternal);
                }

                Configuration.Map(name, mapper);
            }
            return this;
        }

        public ChoYamlWriter<T> ClearFieldForType<TClass>()
        {
            Configuration.ClearRecordFieldsForType(typeof(TClass));
            return this;
        }

        public ChoYamlWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            string yamlPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, typeof(TClass));
        }

        public ChoYamlWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoYamlRecordFieldConfigurationMap> mapper)
        {
            ClearFieldsIf();

            if (!field.GetMemberName().IsNullOrWhiteSpace())
                Configuration.Map(field.GetMemberName(), mapper);
            return this;
        }

        public ChoYamlWriter<T> WithField<TField>(Expression<Func<T, TField>> field, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), field.GetPropertyType(), fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, null);
        }

        public ChoYamlWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, bool isArray = true,
            string nullValue = null)
        {
            return WithField(name, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, null, formatText, isArray, nullValue, null);
        }

        private ChoYamlWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null,
            Type subRecordType = null)
        {
            if (!name.IsNullOrEmpty())
            {
                ClearFieldsIf();
                Configuration.WithField(name, null, fieldType, fieldValueTrimOption, fieldName,
                    valueConverter, itemConverter, customSerializer, defaultValue, fallbackValue, fullyQualifiedMemberName, formatText,
                    isArray, nullValue, typeof(T), subRecordType);
            }
            return this;
        }

        public ChoYamlWriter<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFieldsIf();
            Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            return this;
        }

        public ChoYamlWriter<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoYamlWriter<T> Configure(Action<ChoYamlRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoYamlWriter<T> Setup(Action<ChoYamlWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoYamlWriter<T> MapRecordFields<T1>()
        {
            Configuration.MapRecordFields<T1>();
            return this;
        }

        public ChoYamlWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
                Configuration.MapRecordFields(recordType);

            return this;
        }

        #endregion Fluent API

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");
            if (Configuration.UseYamlSerialization)
            {
                Write(dr);
                return;
            }

            Write(FromDataReader(dr));
        }

        private IEnumerable<T> FromDataReader(IDataReader dr)
        {
            DataTable schemaTable = dr.GetSchemaTable();

            //int ordinal = 0;
            if (Configuration.YamlRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoYamlRecordFieldConfiguration(colName, yamlPath: null);
                    Configuration.YamlRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            var ordinals = Configuration.YamlRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
            while (dr.Read())
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in ordinals)
                {
                    expandoDic.Add(fc.Key, fc.Value == -1 ? null : dr[fc.Value]);
                }

                if (Configuration.IsDynamicObjectInternal)
                    yield return expando;
                else
                {
                    yield return (T)ChoObjectEx.ConvertToObject<T>(expando);
                }
            }
        }

        public void Write(DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");
            if (Configuration.UseYamlSerialization)
            {
                _writer.TraceSwitch = TraceSwitch;
                _writer.WriteTo(_textWriter.Value, dt.ConvertToEnumerable()).Loop();
                return;
            }

            DataTable schemaTable = dt;

            if (Configuration.YamlRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoYamlRecordFieldConfiguration(colName, yamlPath: null);
                    Configuration.YamlRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }
            Configuration.RootName = dt.TableName.IsNullOrWhiteSpace() ? null : dt.TableName;

            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in Configuration.YamlRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name] == DBNull.Value ? null : row[fc.Name]);
                }

                if (Configuration.IsDynamicObjectInternal)
                    Write(expando);
                else
                {
                    Write((T)ChoObjectEx.ConvertToObject<T>(expando));
                }
            }
        
            //Write(list.ToArray());
        }

        public void Write(DataSet ds)
        {
            ChoGuard.ArgumentNotNull(ds, "DataSet");

            foreach (DataTable dt in ds.Tables)
            {
                Configuration.Reset();
                Write(dt);
            }
        }

        ~ChoYamlWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoYamlWriter : ChoYamlWriter<dynamic>
    {
        public ChoYamlWriter(StringBuilder sb, ChoYamlRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoYamlWriter(string filePath, ChoYamlRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoYamlWriter(TextWriter textWriter, ChoYamlRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoYamlWriter(Stream inStream, ChoYamlRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoYamlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText(record, configuration, traceSwitch);
        }

        public static string SerializeAll(IEnumerable<dynamic> records, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, new ChoYamlRecordConfiguration(),
                traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, new ChoYamlRecordConfiguration(), traceSwitch);
        }

        public static string Serialize(dynamic record, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToText(record, new ChoYamlRecordConfiguration(), traceSwitch);
        }

        public static string Serialize<T>(T record, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToText(record, new ChoYamlRecordConfiguration(), traceSwitch);
        }

        ~ChoYamlWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }
}
