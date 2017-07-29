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
using System.Xml;

namespace ChoETL
{
    public class ChoJSONWriter<T> : ChoWriter, IDisposable
        where T : class
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoJSONRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public ChoJSONRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoJSONWriter(string filePath, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _textWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoJSONWriter(TextWriter textWriter, ChoJSONRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(textWriter, "TextWriter");

            Configuration = configuration;
            Init();

            _textWriter = textWriter;
        }

        public ChoJSONWriter(Stream inStream, ChoJSONRecordConfiguration configuration = null)
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

        public void Dispose()
        {
            _writer.EndWrite(_textWriter);

            if (_closeStreamOnDispose)
                _textWriter.Dispose();
        }

        private void Init()
        {
            if (Configuration == null)
                Configuration = new ChoJSONRecordConfiguration(typeof(T));

            _writer = new ChoJSONRecordWriter(typeof(T), Configuration);
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
            _writer.WriteTo(_textWriter, new T[] { record } ).Loop();
        }

        public static string ToText<TRec>(TRec record, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string xpath = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable<TRec>(record), configuration, traceSwitch);
        }


        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoJSONRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string jsonPath = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONWriter<TRec>(writer) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Configuration.JSONPath = jsonPath;

                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        internal static string ToText(object rec, ChoJSONRecordConfiguration configuration, Encoding encoding, int bufferSize, TraceSwitch traceSwitch = null)
        {
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

        public ChoJSONWriter<T> NotifyAfter(long rowsLoaded)
        {
            Configuration.NotifyAfter = rowsLoaded;
            return this;
        }

        public ChoJSONWriter<T> WithFields(params string[] fieldsNames)
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
                        Configuration.JSONRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }
                    fnTrim = fn.NTrim();
                }
            }

            return this;
        }


        public ChoJSONWriter<T> WithField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null, Func<object, object> valueConverter = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.JSONRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                string fnTrim = name.NTrim();
                fieldType = fieldType == null ? typeof(string) : fieldType;

                Configuration.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fnTrim, (string)null) { FieldType = fieldType, FieldValueTrimOption = fieldValueTrimOption, FieldName = fieldName, ValueConverter = valueConverter });
            }

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

        #endregion Fluent API
        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            dynamic expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

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

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.JSONRecordFieldConfigurations)
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

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.JSONRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
            }
        }
    }

    public class ChoJSONWriter : ChoJSONWriter<dynamic>
    {
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


    }
}
