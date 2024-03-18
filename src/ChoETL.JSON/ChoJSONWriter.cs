using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
    public class ChoJSONWriter<T> : ChoWriter, IChoSerializableWriter, IDisposable
        //where T : class
    {
        private Lazy<TextWriter> _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoJSONRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }
        
        public ChoJSONWriter(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoJSONWriter(ChoJSONRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoJSONWriter(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new Lazy<TextWriter>(() => new StreamWriter(filePath, Configuration.Append, Configuration.Encoding, Configuration.BufferSize));
            _closeStreamOnDispose = true;
        }

        public ChoJSONWriter(TextWriter textWriter, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = new Lazy<TextWriter>(() => textWriter);
        }

        public ChoJSONWriter(Stream inStream, ChoJSONRecordConfiguration configuration = null)
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

        public void Dispose()
        {
            Dispose(false);
        }

        public void Flush()
        {
            if (_textWriter != null)
                _textWriter.Value.Flush();
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
                Configuration = new ChoJSONRecordConfiguration(recordType);
            else
                Configuration.RecordTypeInternal = recordType;

            _writer = new ChoJSONRecordWriter(recordType, Configuration);
            _writer.Writer = this;
            _writer.RowsWritten += NotifyRowsWritten;
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.Writer = this;
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
            if (record is ArrayList)
            {
                if (Configuration.SingleElement == null)
                    Configuration.SingleElement = true;
                _writer.WriteTo(_textWriter.Value, ((IEnumerable)record).AsTypedEnumerable<T>().OfType<object>()).Loop();
            }
            else if (record != null && !(/*!record.GetType().IsDynamicType() && record is IDictionary*/ record.GetType() == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(record.GetType()) || record.GetType() == typeof(object) || record.GetType().IsAnonymousType())
                && (typeof(IDictionary).IsAssignableFrom(record.GetType()) || (record.GetType().IsGenericType && record.GetType().GetGenericTypeDefinition() == typeof(IDictionary<,>))))
            {
                if (Configuration.SingleElement == null)
                    Configuration.SingleElement = true;
                _writer.WriteTo(_textWriter.Value, new object[] { record }).Loop();
            }
            else
            {
                if (Configuration.SingleElement == null) Configuration.SingleElement = true;
                _writer.WriteTo(_textWriter.Value, new object[] { record }).Loop();
            }
        }

        public static string ToText<TRec>(TRec record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string jsonPath = null)
        {
            if (record is DataTable)
            {
                StringBuilder xml = new StringBuilder();
                using (var w = new ChoJSONWriter(xml, configuration))
                    w.Write(record as DataTable);
                return xml.ToString();
            }
            else if (record is IDataReader)
            {
                StringBuilder xml = new StringBuilder();
                using (var w = new ChoJSONWriter(xml, configuration))
                    w.Write(record as IDataReader);
                return xml.ToString();
            }

            if (configuration == null)
                configuration = new ChoJSONRecordConfiguration(typeof(TRec));

            configuration.IgnoreRootName = true;
            configuration.RootName = null;
            if (configuration.SingleElement == null) configuration.SingleElement = true;
            configuration.SupportMultipleContent = true;

            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch, jsonPath);
        }


        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string jsonPath = null)
        {
            if (records == null) return null;

            if (typeof(DataTable).IsAssignableFrom(typeof(TRec)))
            {
                StringBuilder json = new StringBuilder();

                foreach (var dt in records.Take(1))
                {
                    using (var w = new ChoJSONWriter(json, configuration))
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
                    using (var w = new ChoJSONWriter(json, configuration))
                    {
                        w.Write(dt);
                    }
                }

                return json.ToString();
            }


            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Configuration.JSONPath = jsonPath;

                parser.Write(records);
                parser.Close();
                writer.Flush();
                
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        internal static string ToText(object rec, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            if (rec is DataTable)
            {
                StringBuilder json = new StringBuilder();
                using (var w = new ChoJSONWriter(json, configuration))
                {
                    w.Write(rec as DataTable);
                }
                return json.ToString();
            }
            else if (rec is IDataReader)
            {
                StringBuilder json = new StringBuilder();
                using (var w = new ChoJSONWriter(json, configuration))
                {
                    w.Write(rec as IDataReader);
                }
                return json.ToString();
            }

            ChoJSONRecordWriter writer = new ChoJSONRecordWriter(rec.GetType(), configuration);
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

        public ChoJSONWriter<T> TypeConverterFormatSpec(Action<ChoTypeConverterFormatSpec> spec)
        {
            Configuration.CreateTypeConverterSpecsIfNull();
            spec?.Invoke(Configuration.TypeConverterFormatSpec);
            return this;
        }

        public ChoJSONWriter<T> WithJSONConverter(JsonConverter converter)
        {
            Configuration.WithJSONConverter(converter);
            return this;
        }

        public ChoJSONWriter<T> RegisterNodeConverterForType<ModelType>(Func<object, object> selector)
        {
            return RegisterNodeConverterForType(typeof(ModelType), selector);
        }

        public ChoJSONWriter<T> RegisterNodeConverterForType(Type type, Func<object, object> selector)
        {
            Configuration.RegisterNodeConverterForType(type, selector);
            return this;
        }

        public ChoJSONWriter<T> RegisterNodeSelectorForType(Type type, Func<object, object> selector)
        {
            Configuration.RegisterNodeConverterForType(type, selector);
            return this;
        }

        public ChoJSONWriter<T> NullValueHandling(ChoNullValueHandling value = ChoNullValueHandling.Default)
        {
            Configuration.NullValueHandling = value;
            return this;
        }

        public ChoJSONWriter<T> Formatting(Newtonsoft.Json.Formatting value = Newtonsoft.Json.Formatting.Indented)
        {
            Configuration.Formatting = value;
            return this;
        }

        public ChoJSONWriter<T> ErrorMode(ChoErrorMode mode)
        {
            Configuration.ErrorMode = mode;
            return this;
        }

        public ChoJSONWriter<T> IgnoreFieldValueMode(ChoIgnoreFieldValueMode mode)
        {
            Configuration.IgnoreFieldValueMode = mode;
            return this;
        }

        public ChoJSONWriter<T> SingleElement(bool flag = true)
        {
            Configuration.SingleElement = flag;
            return this;
        }

        public ChoJSONWriter<T> SupportMultipleContent(bool flag = true)
        {
            Configuration.SupportMultipleContent = flag;
            return this;
        }

        public ChoJSONWriter<T> UseJsonSerialization(bool flag = true)
        {
            Configuration.UseJSONSerialization = flag;
            return this;
        }

        public ChoJSONWriter<T> JsonSerializationSettings(Action<JsonSerializerSettings> settings)
        {
            settings?.Invoke(Configuration.JsonSerializerSettings);
            return this;
        }

        public ChoJSONWriter<T> JsonSerializerContext(Action<dynamic> ctxSettings)
        {
            ctxSettings?.Invoke((dynamic)Configuration.JsonSerializer.Context.Context);
            return this;
        }

        public ChoJSONWriter<T> WithMaxScanNodes(int value)
        {
            if (value > 0)
                Configuration.MaxScanRows = value;
            return this;
        }

        public ChoJSONWriter<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONWriter<T> ClearFields()
        {
            Configuration.ClearFields();
            _clearFields = true;
            return this;
        }

        private ChoJSONWriter<T> ClearFieldsIf()
        {
            if (!_clearFields)
            {
                Configuration.ClearFields();
                _clearFields = true;
                Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            }
            return this;
        }

        public ChoJSONWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            ClearFieldsIf();
            Configuration.IgnoreField(field);
            return this;
        }

        public ChoJSONWriter<T> IgnoreField(string fieldName)
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

        public ChoJSONWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoJSONWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                PropertyDescriptor pd = null;
                ChoJSONRecordFieldConfiguration fc = null;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    ClearFieldsIf();
                    fnTrim = fn.NTrim();
                    if (Configuration.JSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.JSONRecordFieldConfigurations.Remove(Configuration.JSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoJSONRecordFieldConfiguration(fnTrim, (string)null);
                    nfc.PropertyDescriptorInternal = fc != null ? fc.PropertyDescriptorInternal : pd;
                    nfc.DeclaringMemberInternal = fc != null ? fc.DeclaringMemberInternal : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.LoadFieldConfigurationAttributes(nfc, typeof(T));
                    Configuration.JSONRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoJSONWriter<T> WithField(string name, Action<ChoJSONRecordFieldConfigurationMap> mapper)
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

        public ChoJSONWriter<T> ClearFieldForType<TClass>()
        {
            Configuration.ClearRecordFieldsForType(typeof(TClass));
            return this;
        }

        public ChoJSONWriter<T> WithFieldForType<TClass>(Expression<Func<TClass, object>> field,
            string jsonPath = null, Type fieldType = null,
            ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim,
            string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null,
            IChoValueConverter propertyConverter = null)
            where TClass : class
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, typeof(TClass),
                propertyConverter);
        }

        public ChoJSONWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            ClearFieldsIf();

            if (!field.GetMemberName().IsNullOrWhiteSpace())
                Configuration.Map(field.GetMemberName(), mapper);
            return this;
        }

        public ChoJSONWriter<T> WithField<TField>(Expression<Func<T, TField>> field, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null,
            string nullValue = null,
            IChoValueConverter propertyConverter = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), field.GetPropertyType(), fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, field.GetFullyQualifiedMemberName(), formatText, true, nullValue, null, propertyConverter);
        }

        public ChoJSONWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string formatText = null, bool isArray = true,
            string nullValue = null,
            IChoValueConverter propertyConverter = null)
        {
            return WithField(name, fieldType, fieldValueTrimOption, fieldName, valueConverter, itemConverter,
                customSerializer, defaultValue, fallbackValue, null, formatText, isArray, nullValue, null, propertyConverter);
        }

        private ChoJSONWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null,
            Func<object, object> itemConverter = null,
            Func<object, object> customSerializer = null,
            object defaultValue = null, object fallbackValue = null, string fullyQualifiedMemberName = null,
            string formatText = null, bool isArray = true, string nullValue = null,
            Type subRecordType = null,
            IChoValueConverter propertyConverter = null)
        {
            if (!name.IsNullOrEmpty())
            {
                ClearFieldsIf();
                Configuration.WithField(name, null, fieldType, fieldValueTrimOption, fieldName,
                    valueConverter, itemConverter, customSerializer, defaultValue, fallbackValue, fullyQualifiedMemberName, formatText,
                    isArray, nullValue, typeof(T), subRecordType, propertyConverter: propertyConverter);
            }
            return this;
        }

        public ChoJSONWriter<T> WithFlatToNestedObjectSupport(bool flatToNestedObjectSupport = true)
        {
            Configuration.FlatToNestedObjectSupport = flatToNestedObjectSupport;
            ClearFieldsIf();
            Configuration.MapRecordFields(Configuration.RecordTypeInternal);
            return this;
        }

        public ChoJSONWriter<T> ColumnCountStrict()
        {
            Configuration.ColumnCountStrict = true;
            return this;
        }

        public ChoJSONWriter<T> Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoJSONWriter<T> Setup(Action<ChoJSONWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoJSONWriter<T> MapRecordFields<T1>()
        {
            if (typeof(T) == typeof(T1))
                Configuration.MapRecordFields<T1>();
            else
                Configuration.MapRecordFieldsForType(typeof(T1));

            return this;
        }

        public ChoJSONWriter<T> MapRecordFields(Type recordType)
        {
            if (recordType != null)
            {
                if (typeof(T) == recordType)
                    Configuration.MapRecordFields(recordType);
                else
                    Configuration.MapRecordFieldsForType(recordType);
            }

            return this;
        }

        public ChoJSONWriter<T> UseDefaultContractResolver(bool flag = true, Action<ChoPropertyRenameAndIgnoreSerializerContractResolver> setup = null)
        {
            Configuration.UseDefaultContractResolver = flag;
            Configuration.DefaultContractResolverSetup = setup;

            return this;
        }

        public ChoJSONWriter<T> ConfigureContractResolver(Action<IContractResolver> setup = null)
        {
            Configuration.ConfigureContractResolver(setup);
            return this;
        }

        #endregion Fluent API

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");
            if (Configuration.UseJSONSerialization)
            {
                Write(dr);
                return;
            }

            DataTable schemaTable = dr.GetSchemaTable();

            //int ordinal = 0;
            if (Configuration.JSONRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoJSONRecordFieldConfiguration(colName, jsonPath: null);
                    Configuration.JSONRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            var ordinals = Configuration.JSONRecordFieldConfigurations.ToDictionary(c => c.Name, c => dr.HasColumn(c.Name) ? dr.GetOrdinal(c.Name) : -1);
            while (dr.Read())
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in ordinals)
                {
                    expandoDic.Add(fc.Key, fc.Value == -1 ? null : dr[fc.Value]);
                }

                if (Configuration.IsDynamicObjectInternal)
                    Write(expando);
                else
                {
                    Write((T)ChoObjectEx.ConvertToObject<T>(expando));
                }
            }
        }

        public void Write(DataTable dt)
        {
            ChoGuard.ArgumentNotNull(dt, "DataTable");
            if (Configuration.UseJSONSerialization)
            {
                Write(dt);
                return;
            }

            DataTable schemaTable = dt;

            if (Configuration.JSONRecordFieldConfigurations.IsNullOrEmpty())
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

                    var obj = new ChoJSONRecordFieldConfiguration(colName, jsonPath: null);
                    Configuration.JSONRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }
            Configuration.RootName = dt.TableName.IsNullOrWhiteSpace() ? null : dt.TableName;

            foreach (DataRow row in dt.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDic = (IDictionary<string, object>)expando;

                foreach (var fc in Configuration.JSONRecordFieldConfigurations)
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
        }

        public void Write(DataSet ds)
        {
            ChoGuard.ArgumentNotNull(ds, "DataSet");

            foreach (DataTable dt in ds.Tables)
            {
                Configuration.Reset();
                Configuration.RootName = ds.DataSetName.IsNullOrWhiteSpace() ? "Root" : ds.DataSetName;
                Write(dt);
            }
        }

        ~ChoJSONWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }

    public class ChoJSONWriter : ChoJSONWriter<dynamic>
    {
        public ChoJSONWriter(StringBuilder sb, ChoJSONRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoJSONWriter(string filePath, ChoJSONRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoJSONWriter(TextWriter textWriter, ChoJSONRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoJSONWriter(Stream inStream, ChoJSONRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText(record, configuration, traceSwitch);
        }

        public static string SerializeAll(IEnumerable<dynamic> records, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, new ChoJSONRecordConfiguration().Configure(c => c.JsonSerializerSettings = jsonSerializerSettings).Configure(c => c.UseJSONSerialization = true),
                traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToTextAll(records, new ChoJSONRecordConfiguration().Configure(c => c.JsonSerializerSettings = jsonSerializerSettings).Configure(c => c.UseJSONSerialization = true), traceSwitch);
        }

        public static string Serialize(dynamic record, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToText(record, new ChoJSONRecordConfiguration().Configure(c => c.JsonSerializerSettings = jsonSerializerSettings).Configure(c => c.UseJSONSerialization = true), traceSwitch);
        }

        public static string Serialize<T>(T record, JsonSerializerSettings jsonSerializerSettings, TraceSwitch traceSwitch = null)
        {
            return ToText(record, new ChoJSONRecordConfiguration().Configure(c => c.JsonSerializerSettings = jsonSerializerSettings).Configure(c => c.UseJSONSerialization = true), traceSwitch);
        }

        ~ChoJSONWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }
}
