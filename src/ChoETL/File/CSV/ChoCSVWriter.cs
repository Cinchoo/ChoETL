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

namespace ChoETL
{
    public class ChoCSVWriter<T> : ChoWriter, IDisposable
        where T : class
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoCSVRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;
        private bool _isDisposed = false;

        public override dynamic Context
        {
            get { return Configuration.Context; }
        }

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVWriter(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : this(new StringWriter(sb), configuration)
        {

        }

        public ChoCSVWriter(ChoCSVRecordConfiguration configuration = null)
        {
            Configuration = configuration;
            Init();
        }

        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVWriter(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
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
            else
            {
                if (_textWriter != null)
                    _textWriter.Flush();
            }

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoCSVRecordConfiguration(typeof(T));

            _writer = new ChoCSVRecordWriter(typeof(T), Configuration);
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

        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<TRec>(writer, configuration) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }
        public static string ToText<TRec>(TRec record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable(record), configuration, traceSwitch);
        }

        internal static string ToText(object rec, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
            ChoCSVRecordWriter writer = new ChoCSVRecordWriter(rec.GetType(), configuration);
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

        public ChoCSVWriter<T> NotifyAfter(long rowsWritten)
        {
            Configuration.NotifyAfter = rowsWritten;
            return this;
        }

        public ChoCSVWriter<T> WithDelimiter(string delimiter)
        {
            Configuration.Delimiter = delimiter;
            return this;
        }

        public ChoCSVWriter<T> WithFirstLineHeader()
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = true;
            return this;
        }

        public ChoCSVWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoCSVWriter<T> HasExcelSeparator(bool? value)
        {
            Configuration.HasExcelSeparator = value;
            return this;
        }

        public ChoCSVWriter<T> WithEOLDelimiter(string delimiter)
        {
            Configuration.EOLDelimiter = delimiter;
            return this;
        }

        public ChoCSVWriter<T> ConfigureHeader(Action<ChoCSVFileHeaderConfiguration> action)
        {
            if (action != null)
                action(Configuration.FileHeaderConfiguration);

            return this;
        }

        public ChoCSVWriter<T> ClearFields()
        {
            Configuration.CSVRecordFieldConfigurations.Clear();
            _clearFields = true;
            return this;
        }

        public ChoCSVWriter<T> IgnoreField<TField>(Expression<Func<T, TField>> field)
        {
            if (field != null)
                return IgnoreField(field.GetMemberName());
            else
                return this;
        }

        public ChoCSVWriter<T> IgnoreField(string fieldName)
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
                if (Configuration.CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    Configuration.CSVRecordFieldConfigurations.Remove(Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                else
                    Configuration.IgnoredFields.Add(fieldName);
            }

            return this;
        }

        public ChoCSVWriter<T> WithFields<TField>(params Expression<Func<T, TField>>[] fields)
        {
            if (fields != null)
            {
                foreach (var field in fields)
                    return WithField(field);
            }
            return this;
        }

        public ChoCSVWriter<T> WithFields(params string[] fieldsNames)
        {
            string fnTrim = null;
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                PropertyDescriptor pd = null;
                ChoCSVRecordFieldConfiguration fc = null;
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
                    if (Configuration.CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                    {
                        fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                        Configuration.CSVRecordFieldConfigurations.Remove(Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).First());
                    }
                    else
                        pd = ChoTypeDescriptor.GetProperty(typeof(T), fn);

                    var nfc = new ChoCSVRecordFieldConfiguration(fnTrim, ++maxFieldPos) { FieldName = fn };
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : null;
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoCSVWriter<T> WithField<TField>(Expression<Func<T, TField>> field, Action<ChoCSVRecordFieldConfigurationMap> setup)
        {
            Configuration.MapRecordField(field.GetMemberName(), setup);
            return this;
        }

        public ChoCSVWriter<T> WithField(string name, Action<ChoCSVRecordFieldConfigurationMap> mapper)
        {
            if (!name.IsNullOrWhiteSpace())
                Configuration.MapRecordField(name, mapper);
            return this;
        }

        public ChoCSVWriter<T> Index<TField>(Expression<Func<T, TField>> field, int minumum, int maximum)
        {
            Type recordType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();

            if (typeof(IList).IsAssignableFrom(recordType) && !typeof(ArrayList).IsAssignableFrom(recordType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum)
            {
                recordType = recordType.GetItemType().GetUnderlyingType();
                if (recordType.IsSimple())
                {

                }
                else
                {
                    //Remove any unused config
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        var fcs = Configuration.CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(field.GetFullyQualifiedMemberName(), pd.Name)
                        && o.ArrayIndex != null && (o.ArrayIndex < minumum || o.ArrayIndex > maximum)).ToArray();

                        foreach (var fc in fcs)
                            Configuration.CSVRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                        {
                            var fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(field.GetFullyQualifiedMemberName(), pd.Name)
                            && o.ArrayIndex != null && o.ArrayIndex == index).FirstOrDefault();

                            if (fc != null) continue;

                            Type pt = pd.PropertyType.GetUnderlyingType();
                            if (pt != typeof(object) && !pt.IsSimple())
                            {
                            }
                            else
                            {
                                int fieldPosition = 0;
                                fieldPosition = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                                fieldPosition++;
                                ChoCSVRecordFieldConfiguration obj = Configuration.NewFieldConfiguration(ref fieldPosition, field.GetFullyQualifiedMemberName(), pd, index, field.GetPropertyDescriptor().GetDisplayName());

                                //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                Configuration.CSVRecordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
            return this;
        }

        public ChoCSVWriter<T> DictionaryKeys<TField>(Expression<Func<T, TField>> field, params string[] keys)
        {
            Type recordType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            PropertyDescriptor pd = field.GetPropertyDescriptor();

            if (recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                && typeof(string) == recordType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
                //Remove any unused config
                var fcs = Configuration.CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == pd.Name
                && !o.DictKey.IsNullOrWhiteSpace() && !keys.Contains(o.DictKey)).ToArray();

                foreach (var fc in fcs)
                    Configuration.CSVRecordFieldConfigurations.Remove(fc);

                foreach (var key in keys)
                {
                    if (!key.IsNullOrWhiteSpace())
                    {
                        var fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == pd.Name
                            && !o.DictKey.IsNullOrWhiteSpace() && key == o.DictKey).FirstOrDefault();

                        if (fc != null) continue;

                        //ChoCSVRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);
                        int fieldPosition = 0;
                        fieldPosition = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;
                        ChoCSVRecordFieldConfiguration obj = Configuration.NewFieldConfiguration(ref fieldPosition, null, field.GetPropertyDescriptor(), dictKey: key);

                        //if (!CSVRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                        Configuration.CSVRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            return this;
        }

        public ChoCSVWriter<T> WithField<TField>(Expression<Func<T, TField>> field, bool? quoteField = null, 
            char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, int? fieldPosition = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, 
            string formatText = null, string nullValue = null)
        {
            if (field == null)
                return this;

            return WithField(field.GetMemberName(), field.GetPropertyType(), quoteField, fillChar, fieldValueJustification, truncate, 
                fieldName, fieldPosition, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue,
                field.GetFullyQualifiedMemberName(), formatText, nullValue);
        }

        public ChoCSVWriter<T> WithField(string name, Type fieldType = null, bool? quoteField = null, char? fillChar = null, 
            ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null, int? fieldPosition = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, 
            string formatText = null, string nullValue = null)
        {
            return WithField(name, fieldType, quoteField, fillChar, fieldValueJustification,
                truncate, fieldName, fieldPosition, valueConverter, valueSelector, headerSelector, defaultValue, fallbackValue, null, formatText, nullValue);
        }

        private ChoCSVWriter<T> WithField(string name, Type fieldType = null, bool? quoteField = null, char? fillChar = null,
            ChoFieldValueJustification? fieldValueJustification = null,
            bool? truncate = null, string fieldName = null, int? fieldPosition = null, 
            Func<object, object> valueConverter = null, 
            Func<dynamic, object> valueSelector = null,
            Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null,
            string fullyQualifiedMemberName = null, string formatText = null, string nullValue = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    ClearFields();
                    Configuration.MapRecordFields(Configuration.RecordType);
                }
                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;

                string fnTrim = name.NTrim();
                ChoCSVRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                //if (Configuration.CSVRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                //{
                    if (fullyQualifiedMemberName != null)
                        fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.DeclaringMember == fullyQualifiedMemberName).FirstOrDefault();
                    else
                        fc = Configuration.CSVRecordFieldConfigurations.Where(o => o.Name == fnTrim).FirstOrDefault();

                    if (fc != null && fieldPosition == null)
                        fieldPosition = fc.FieldPosition;

                    //Configuration.CSVRecordFieldConfigurations.Remove(fc);
                //}
                if (fc == null)
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(typeof(T), fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                    if (fieldPosition == null)
                    {
                        fieldPosition = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                        fieldPosition++;
                    }
                }

                ChoCSVRecordFieldConfiguration nfc = null;
                if (fc != null)
                {
                    nfc = fc;

                    if (fieldType != null)
                        nfc.FieldType = fieldType;
                    if (quoteField != null)
                        nfc.QuoteField = quoteField;
                    if (fillChar != null)
                        nfc.FillChar = fillChar;
                    if (fieldValueJustification != null)
                        nfc.FieldValueJustification = fieldValueJustification;
                    if (truncate != null)
                        nfc.Truncate = truncate.Value;
                    if (fieldName != null)
                        nfc.FieldName = fieldName;
                    if (valueConverter != null)
                        nfc.ValueConverter = valueConverter;
                    if (valueSelector != null)
                        nfc.ValueSelector = valueSelector;
                    if (headerSelector != null)
                        nfc.HeaderSelector = headerSelector;
                    if (defaultValue != null)
                        nfc.DefaultValue = defaultValue;
                    if (fallbackValue != null)
                        nfc.FallbackValue = fallbackValue;
                    if (formatText != null)
                        nfc.FormatText = formatText;
                    if (nullValue != null)
                        nfc.NullValue = nullValue;
                }
                else
                {
                    nfc = new ChoCSVRecordFieldConfiguration(fnTrim, fieldPosition.Value)
                    {
                        FieldType = fieldType,
                        QuoteField = quoteField,
                        FillChar = fillChar,
                        FieldValueJustification = fieldValueJustification,
                        Truncate = truncate != null ? truncate.Value : true,
                        FieldName = fieldName,
                        ValueConverter = valueConverter,
                        ValueSelector = valueSelector,
                        HeaderSelector = headerSelector,
                        DefaultValue = defaultValue,
                        FallbackValue = fallbackValue,
                        FormatText = formatText,
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
                    if (pd != null)
                    {
                        if (nfc.FieldType == null)
                            nfc.FieldType = pd.PropertyType;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(nfc);
                }
            }

            return this;
        }

        public ChoCSVWriter<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        public ChoCSVWriter<T> ThrowAndStopOnMissingField(bool flag = true)
        {
            Configuration.ThrowAndStopOnMissingField = flag;
            return this;
        }

        public ChoCSVWriter<T> Configure(Action<ChoCSVRecordConfiguration> action)
        {
            if (action != null)
                action(Configuration);

            return this;
        }

        public ChoCSVWriter<T> Setup(Action<ChoCSVWriter<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoCSVWriter<T> MapRecordFields<T1>()
        {
            Configuration.MapRecordFields<T1>();
            return this;
        }

        public ChoCSVWriter<T> MapRecordFields(Type recordType)
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

            Configuration.UseNestedKeyFormat = false;

            int ordinal = 0;
            if (Configuration.CSVRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataRow row in schemaTable.Rows)
                {
                    colName = row["ColumnName"].CastTo<string>();
                    colType = row["DataType"] as Type;
                    //if (!colType.IsSimple()) continue;

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.CSVRecordFieldConfigurations)
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

            int ordinal = 0;
            if (Configuration.CSVRecordFieldConfigurations.IsNullOrEmpty())
            {
                string colName = null;
                Type colType = null;
                foreach (DataColumn col in schemaTable.Columns)
                {
                    colName = col.ColumnName;
                    colType = col.DataType;
                    //if (!colType.IsSimple()) continue;

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(colName, ++ordinal) { FieldType = colType });
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.CSVRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
            }
        }

        ~ChoCSVWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }

    }

    public class ChoCSVWriter : ChoCSVWriter<dynamic>
    {
        public ChoCSVWriter(StringBuilder sb, ChoCSVRecordConfiguration configuration = null) : base(sb, configuration)
        {

        }
        public ChoCSVWriter(ChoCSVRecordConfiguration configuration = null)
            : base(configuration)
        {

        }
        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {
        }
        public ChoCSVWriter(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
            : base(textWriter, configuration)
        {
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public static string SerializeAll(IEnumerable<dynamic> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToTextAll<dynamic>(records, configuration, traceSwitch);
        }

        public static string SerializeAll<T>(IEnumerable<T> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToTextAll<T>(records, configuration, traceSwitch);
        }

        public static string Serialize(dynamic record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ToText<dynamic>(record, configuration, traceSwitch);
        }

        public static string Serialize<T>(T record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where T : class
        {
            return ToText<T>(record, configuration, traceSwitch);
        }
    }
}
