using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoTSVReader<T>
        where T : class
    {
        private static readonly string delimiter = "\t";

        public static ChoCSVReader<T> New(StringBuilder sb, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader<T>(sb, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> New(ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader<T>(configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> New(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader<T>(filePath, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> New(TextReader textReader, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader<T>(textReader, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> New(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader<T>(inStream, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> LoadText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader<T>.LoadText(inputText, encoding, configuration, traceSwitch).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> LoadText(string inputText, ChoCSVRecordConfiguration configuration, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader<T>.LoadText(inputText, configuration, traceSwitch).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<T> LoadLines(IEnumerable<string> inputLines, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader<T>.LoadLines(inputLines, configuration, traceSwitch).WithDelimiter(delimiter);
        }
    }

    public static class ChoTSVReader
    {
        private static readonly string delimiter = "\t";

        public static ChoCSVReader<dynamic> New(StringBuilder sb, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader(sb, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> New(ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader(configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> New(string filePath, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader(filePath, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> New(TextReader textReader, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader(textReader, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> New(Stream inStream, ChoCSVRecordConfiguration configuration = null)
        {
            return new ChoCSVReader(inStream, configuration).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> LoadText(string inputText, Encoding encoding = null, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader.LoadText(inputText, encoding, configuration, traceSwitch).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> LoadText(string inputText, ChoCSVRecordConfiguration configuration, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader.LoadText(inputText, configuration, traceSwitch).WithDelimiter(delimiter);
        }

        public static ChoCSVReader<dynamic> LoadLines(IEnumerable<string> inputLines, ChoCSVRecordConfiguration configuration = null, TraceSwitch traceSwitch = null)
        {
            return ChoCSVReader.LoadLines(inputLines, configuration, traceSwitch).WithDelimiter(delimiter);
        }
    }
}
