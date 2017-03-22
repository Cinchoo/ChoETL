using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVWriter<T> : IDisposable
        where T : class
    {
        private StreamWriter _streamWriter;
        private bool _closeStreamOnDispose = false;
        private ChoCSVRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public ChoCSVRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoCSVWriter(StreamWriter streamWriter, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = streamWriter;
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(inStream, "Stream");

            Configuration = configuration;
            Init();
            _streamWriter = new StreamWriter(inStream, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public void Dispose()
        {
            if (_closeStreamOnDispose)
                _streamWriter.Dispose();
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
            _writer.WriteTo(_streamWriter, records).Loop();
        }

        public void Write(T record)
        {
            _writer.WriteTo(_streamWriter, new T[] { record } ).Loop();
        }

        public static string ToText<TRec>(IEnumerable<TRec> records, ChoCSVRecordConfiguration configuration = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<TRec>(writer, configuration))
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        //public static string ToText<TRec>(TRec record, ChoCSVRecordConfiguration configuration = null)
        //    where TRec : class
        //{
        //    if (record is IEnumerable<TRec>)
        //        return ToText(record as IEnumerable<TRec>, configuration);
        //    else
        //        return ToText(ChoEnumerable.AsEnumerable(record), configuration);
        //}

        internal static string ToText(object rec, ChoCSVRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoCSVRecordWriter writer = new ChoCSVRecordWriter(rec.GetType(), configuration);
            writer.TraceSwitch = ChoETLFramework.TraceSwitchOff;

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

        public ChoCSVWriter<T> WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoCSVWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoCSVWriter<T> WithFields(params string[] fieldsNames)
        {
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;
                    if (!_clearFields)
                    {
                        Configuration.CSVRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }

                    Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fn.Trim(), ++maxFieldPos));
                }

            }

            return this;
        }

        public ChoCSVWriter<T> WithField(string name, Type fieldType, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.CSVRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }
                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;

                int maxFieldPos = Configuration.CSVRecordFieldConfigurations.Count > 0 ? Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(name.Trim(), ++maxFieldPos) { FieldType = fieldType, QuoteField = quoteField,
                    FillChar = fillChar,
                    FieldValueJustification = fieldValueJustification,
                    Truncate = truncate,
                    FieldName = fieldName.NTrim()
                });
            }

            return this;
        }

        public ChoCSVWriter<T> WithField(string name, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null)
        {
            return WithField(name, null, quoteField, fillChar, fieldValueJustification, truncate, fieldName);
        }

        public ChoCSVWriter<T> ColumnCountStrict(bool flag = true)
        {
            Configuration.ColumnCountStrict = flag;
            return this;
        }

        #endregion Fluent API
    }

    public class ChoCSVWriter : ChoCSVWriter<ExpandoObject>
    {
        public ChoCSVWriter(string filePath, ChoCSVRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoCSVWriter(StreamWriter streamWriter, ChoCSVRecordConfiguration configuration = null)
            : base(streamWriter, configuration)
        {
        }

        public ChoCSVWriter(Stream inStream, ChoCSVRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

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
            var expando = new ExpandoObject();
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
    }
}
