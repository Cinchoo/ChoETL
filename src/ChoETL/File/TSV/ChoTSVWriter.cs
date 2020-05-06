using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoTSVWriter<T>
        where T : class
    {
        private static readonly string delimiter = "\t";

        public static ChoCSVWriter<T> New(StringBuilder sb, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter<T>(sb, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<T> New(ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter<T>(configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<T> New(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter<T>(filePath, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<T> New(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter<T>(textWriter, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<T> New(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter<T>(inStream, configuration).WithDelimiter(delimiter);
        }

        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ChoCSVWriter<TRec>.ToTextAll(records, configuration, traceSwitch);
        }

        public static string ToText<TRec>(TRec record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable(record), configuration, traceSwitch);
        }
    }

    public static class ChoTSVWriter
    {
        private static readonly string delimiter = "\t";

        public static ChoCSVWriter<dynamic> New(StringBuilder sb, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter(sb, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<dynamic> New(ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter(configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<dynamic> New(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter(filePath, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<dynamic> New(TextWriter textWriter, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter(textWriter, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVWriter<dynamic> New(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVWriter(inStream, configuration).WithDelimiter(delimiter);
        }

        public static string ToTextAll<TRec>(IEnumerable<TRec> records, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ChoCSVWriter<TRec>.ToTextAll(records, configuration, traceSwitch);
        }

        public static string ToText<TRec>(TRec record, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
            where TRec : class
        {
            return ToTextAll(ChoEnumerable.AsEnumerable(record), configuration, traceSwitch);
        }
    }
}
