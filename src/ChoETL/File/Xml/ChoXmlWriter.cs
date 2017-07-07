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
    public class ChoXmlWriter<T> : ChoWriter, IDisposable
        where T : class
    {
        private TextWriter _textWriter;
        private bool _closeStreamOnDispose = false;
        private ChoXmlRecordWriter _writer = null;
        private bool _clearFields = false;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public ChoXmlRecordConfiguration Configuration
        {
            get;
            private set;
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
            _writer.WriteTo(_textWriter, new T[] { record } ).Loop();
        }

        public static string ToText<TRec>(TRec record, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string xpath = null)
            where TRec : class
        {
            return ToText(ChoEnumerable.AsEnumerable(record), configuration, traceSwitch);
        }


        public static string ToText<TRec>(IEnumerable<TRec> records, ChoXmlRecordConfiguration configuration = null, TraceSwitch traceSwitch = null, string xpath = null)
            where TRec : class
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlWriter<TRec>(writer) { TraceSwitch = traceSwitch == null ? ChoETLFramework.TraceSwitch : traceSwitch })
            {
                parser.Configuration.XPath = xpath;

                parser.Write(records);

                writer.Flush();
                stream.Position = 0;

                return reader.ReadToEnd();
            }
        }

        //public static string ToText<TRec>(TRec record, ChoXmlRecordConfiguration configuration = null)
        //    where TRec : class
        //{
        //    return ToText(ChoEnumerable.AsEnumerable(record), configuration);
        //}

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

        public ChoXmlWriter<T> WithXPath(string xPath)
        {
            Configuration.XPath = xPath;
            return this;
        }

        public ChoXmlWriter<T> WithFields(params string[] fieldsNames)
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
                        Configuration.XmlRecordFieldConfigurations.Clear();
                        _clearFields = true;
                    }
                    fnTrim = fn.NTrim();
                    Configuration.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(fnTrim, $"//{fnTrim}"));
                }

            }

            return this;
        }

        public ChoXmlWriter<T> WithXmlElementField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, false, fieldName);
        }

        public ChoXmlWriter<T> WithXmlAttributeField(string name, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null)
        {
            string fnTrim = name.NTrim();
            string xPath = $"//@{fnTrim}";
            return WithField(fnTrim, xPath, fieldType, fieldValueTrimOption, true, fieldName);
        }

        public ChoXmlWriter<T> WithField(string name, string xPath = null, Type fieldType = null, ChoFieldValueTrimOption fieldValueTrimOption = ChoFieldValueTrimOption.Trim, bool isXmlAttribute = false, string fieldName = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (!_clearFields)
                {
                    Configuration.XmlRecordFieldConfigurations.Clear();
                    _clearFields = true;
                }

                string fnTrim = name.NTrim();
                fieldType = fieldType == null ? typeof(string) : fieldType;
                xPath = xPath.IsNullOrWhiteSpace() ? $"//{fnTrim}" : xPath;

                Configuration.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration(fnTrim, xPath) { FieldType = fieldType, FieldValueTrimOption = fieldValueTrimOption, IsXmlAttribute = isXmlAttribute, FieldName = fieldName });
            }

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

        #endregion Fluent API
    }

    public class ChoXmlWriter : ChoXmlWriter<dynamic>
    {
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

        public void Write(IDataReader dr)
        {
            ChoGuard.ArgumentNotNull(dr, "DataReader");

            DataTable schemaTable = dr.GetSchemaTable();
            var expando = new ExpandoObject();
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
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

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
    }
}
