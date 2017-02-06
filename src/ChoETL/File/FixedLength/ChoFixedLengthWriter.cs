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
    public class ChoFixedLengthWriter<T> : IDisposable
        where T : class
    {
        private StreamWriter _streamWriter;
        private bool _closeStreamOnDispose = false;
        private ChoFixedLengthRecordWriter _writer = null;
        private bool _clearFields = false;

        public ChoFixedLengthRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoFixedLengthWriter(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            Configuration = configuration;

            Init();

            _streamWriter = new StreamWriter(ChoPath.GetFullPath(filePath), false, Configuration.Encoding, Configuration.BufferSize);
            _closeStreamOnDispose = true;
        }

        public ChoFixedLengthWriter(StreamWriter streamWriter, ChoFixedLengthRecordConfiguration configuration = null)
        {
            ChoGuard.ArgumentNotNull(streamWriter, "StreamWriter");

            Configuration = configuration;
            Init();

            _streamWriter = streamWriter;
        }

        public ChoFixedLengthWriter(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
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
                Configuration = new ChoFixedLengthRecordConfiguration(typeof(T));

            _writer = new ChoFixedLengthRecordWriter(typeof(T), Configuration);
        }

        public void Write(IEnumerable<T> records)
        {
            _writer.WriteTo(_streamWriter, records).Loop();
        }

        public void Write(T record)
        {
            _writer.WriteTo(_streamWriter, new T[] { record } ).Loop();
        }

        public static string ToText<TRec>(IEnumerable<TRec> records, ChoFixedLengthRecordConfiguration configuration = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthWriter<TRec>(writer))
            {
                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        public static string ToText<TRec>(TRec record, ChoFixedLengthRecordConfiguration configuration = null)
            where TRec : class
        {
            return ToText(ChoEnumerable.AsEnumerable(record), configuration);
        }

        internal static string ToText(object rec, ChoFixedLengthRecordConfiguration configuration, Encoding encoding, int bufferSize)
        {
            ChoFixedLengthRecordWriter writer = new ChoFixedLengthRecordWriter(rec.GetType(), configuration);
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

        #region Fluent API

        public ChoFixedLengthWriter<T> WithRecordLength(int length)
        {
            Configuration.RecordLength = length;
            return this;
        }

        public ChoFixedLengthWriter<T> WithFirstLineHeader(bool flag = true)
        {
            Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return this;
        }

        public ChoFixedLengthWriter<T> QuoteAllFields(bool flag = true, char quoteChar = '"')
        {
            Configuration.QuoteAllFields = flag;
            Configuration.QuoteChar = quoteChar;
            return this;
        }

        public ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, Type fieldType, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.FixedLengthRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                Configuration.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration(name.Trim(), startIndex, size) { FieldType = fieldType, QuoteField = quoteField,
                    FillChar = fillChar,
                    FieldValueJustification = fieldValueJustification,
                    Truncate = truncate,
                    FieldName = fieldName.IsNullOrWhiteSpace() ? name : fieldName
                });
            }

            return this;
        }

        public ChoFixedLengthWriter<T> WithField(string name, int startIndex, int size, bool? quoteField = null, char? fillChar = null, ChoFieldValueJustification? fieldValueJustification = null,
            bool truncate = true, string fieldName = null)
        {
            return WithField(name, startIndex, size, null, quoteField, fillChar, fieldValueJustification, truncate, fieldName);
        }

        #endregion Fluent API
    }

    public class ChoFixedLengthWriter : ChoFixedLengthWriter<ExpandoObject>
    {
        public ChoFixedLengthWriter(string filePath, ChoFixedLengthRecordConfiguration configuration = null)
            : base(filePath, configuration)
        {

        }
        public ChoFixedLengthWriter(StreamWriter streamWriter, ChoFixedLengthRecordConfiguration configuration = null)
            : base(streamWriter, configuration)
        {
        }

        public ChoFixedLengthWriter(Stream inStream, ChoFixedLengthRecordConfiguration configuration = null)
            : base(inStream, configuration)
        {
        }

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            //int ordinal = 0;
            if (Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
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

                    fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(colType);
                    var obj = new ChoFixedLengthRecordFieldConfiguration(colName, startIndex, fieldLength);
                    Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            while (dr.Read())
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.FixedLengthRecordFieldConfigurations)
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

            if (Configuration.FixedLengthRecordFieldConfigurations.IsNullOrEmpty())
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

                    fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(colType);
                    var obj = new ChoFixedLengthRecordFieldConfiguration(colName, startIndex, fieldLength);
                    Configuration.FixedLengthRecordFieldConfigurations.Add(obj);
                    startIndex += fieldLength;
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                expandoDic.Clear();

                foreach (var fc in Configuration.FixedLengthRecordFieldConfigurations)
                {
                    expandoDic.Add(fc.Name, row[fc.Name]);
                }

                Write(expando);
            }
        }
    }
}
