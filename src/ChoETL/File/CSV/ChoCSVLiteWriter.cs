using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Flags]
    public enum QuoteAllFields
    {
        None = 0,
        DataOnly = 1,
        HeaderOnly = 2,
        Both = DataOnly | HeaderOnly
    }

    public unsafe struct ChoCSVLiteWriter : IChoCSVLiteWriter, IChoLiteParser, IDisposable
    {
        private StringComparison? _stringComparision
        {
            get;
            set;
        }

        public ChoCSVLiteWriter(StringComparison stringComparision = StringComparison.Ordinal)
        {
            _stringComparision = stringComparision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFile<T>(string filename, IEnumerable<T> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"', 
            Action<int, T, IList<string>> mapper = null, QuoteAllFields quoteAllFields = QuoteAllFields.None) where T : new()
        {
            if (filename.IsNullOrWhiteSpace())
                throw new ArgumentException("Invalid filename passed.");

            using (FileStream f = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter s = new StreamWriter(f))
                {
                    Write<T>(s, records, headers, delimiter, EOLDelimiter, quoteChar, mapper, quoteAllFields);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(StreamWriter writer, IEnumerable<T> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"',
            Action<int, T, IList<string>> mapper = null, QuoteAllFields quoteAllFields = QuoteAllFields.None) where T : new()
        {
            Write(writer as TextWriter, records, headers, delimiter, EOLDelimiter, quoteChar, mapper, quoteAllFields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(StringBuilder sb, IEnumerable<T> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"',
            Action<int, T, IList<string>> mapper = null, QuoteAllFields quoteAllFields = QuoteAllFields.None) where T : new()
        {
            if (sb == null)
                throw new ArgumentException("Invalid StringBuilder passed.");
            Write(new StringWriter(sb), records, headers, delimiter, EOLDelimiter, quoteChar, mapper, quoteAllFields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(TextWriter writer, IEnumerable<T> records, string[] headers = null, 
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"',
            Action<int, T, IList<string>> mapper = null, QuoteAllFields quoteAllFields = QuoteAllFields.None) where T : new()
        {
            if (writer == null)
                throw new ArgumentException("Invalid stream writer passed.");
            this.Validate<T>();
            var @this = this;

            var recEnum = records.GetEnumerator();

            if (headers.IsNullOrEmpty())
            {
                T firstRec = default(T);
                if (GetFirstNotNullRecord(recEnum, ref firstRec))
                {
                    headers = this.GetHeaders(firstRec);
                }

                WriteToFile(writer, GetRecords(firstRec, recEnum).Select((rec, index) =>
                {
                    IList<string> cols = new List<string>();
                    if (mapper == null)
                        @this.GetValues(rec, cols);
                    else
                        mapper(index, rec, cols);

                    return cols.ToArray();
                }), headers, delimiter, EOLDelimiter, quoteChar, quoteAllFields);
            }
            else
            {
                WriteToFile(writer, records.Select((rec, index) =>
                {
                    IList<string> cols = new List<string>();
                    if (mapper == null)
                        @this.GetValues(rec, cols);
                    else
                        mapper(index, rec, cols);

                    return cols.ToArray();
                }), headers, delimiter, EOLDelimiter, quoteChar, quoteAllFields);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFile(string filename, IEnumerable<string[]> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"', QuoteAllFields quoteAllFields = QuoteAllFields.None)
        {
            if (filename.IsNullOrWhiteSpace())
                throw new ArgumentException("Invalid filename passed.");

            using (FileStream f = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter s = new StreamWriter(f))
                {
                    Write(s, records, headers, delimiter, EOLDelimiter, quoteChar, quoteAllFields);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(StreamWriter writer, IEnumerable<string[]> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"', QuoteAllFields quoteAllFields = QuoteAllFields.None)
        {
            Write(writer as TextWriter, records, headers, delimiter, EOLDelimiter, quoteChar, quoteAllFields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TextWriter writer, IEnumerable<string[]> records, string[] headers = null,
            char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"', QuoteAllFields quoteAllFields = QuoteAllFields.None)
        {
            if (writer == null)
                throw new ArgumentException("Invalid stream writer passed.");
            this.Validate(delimiter, EOLDelimiter, quoteChar);

            WriteToFile(writer, records, headers, delimiter, EOLDelimiter, quoteChar, quoteAllFields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteToFile(TextWriter writer, IEnumerable<string[]> records, string[] headers, char delimiter, string EOLDelimiter, 
            char quoteChar, QuoteAllFields quoteAllFields = QuoteAllFields.None)
        {
            this.Validate(delimiter, EOLDelimiter, quoteChar);

            if (EOLDelimiter.IsNullOrEmpty())
                EOLDelimiter = Environment.NewLine;

            string doubleQuotesText = $"{quoteChar}{quoteChar}";
            string quotesText = $"{quoteChar}";
            int index = 0;
            int lineNo = 0;
            foreach (var rec in records)
            {
                if (index == 0)
                {
                    if (!headers.IsNullOrEmpty())
                    {
                        WriteRec(writer, delimiter, EOLDelimiter, quoteChar, doubleQuotesText, quotesText, headers, lineNo,
                            (quoteAllFields & QuoteAllFields.HeaderOnly) ==  QuoteAllFields.HeaderOnly);
                        lineNo++;
                    }
                }

                WriteRec(writer, delimiter, EOLDelimiter, quoteChar, doubleQuotesText, quotesText, rec, lineNo,
                    (quoteAllFields & QuoteAllFields.DataOnly) == QuoteAllFields.DataOnly);
                lineNo++;
                index++;
            }
            writer.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteRec(TextWriter writer, char delimiter, string EOLDelimiter, char quoteChar, string doubleQuotesText, string quotesText, string[] rec,
            int lineNo, bool quoteAllFields)
        {
            WriteLine(lineNo, writer, String.Empty, EOLDelimiter);
            for (int i = 0; i < rec.Length; i++)
            {
                var str = rec[i];
                bool quote = false;

                if (str.IndexOf(quoteChar) >= 0)
                {
                    quote = true;
                    str = str.Replace(quotesText, doubleQuotesText);
                }

                if (quote == false && EOLDelimiter != null && str.IndexOf(EOLDelimiter, _stringComparision == null ? StringComparison.Ordinal : _stringComparision.Value) >= 0)
                    quote = true;

                if (quote == false && str.IndexOf(delimiter) >= 0)
                    quote = true;

                if (quoteAllFields)
                    quote = true;

                if (quote)
                    writer.Write(quoteChar);
                writer.Write(str);
                if (quote)
                    writer.Write(quoteChar);

                if (i < rec.Length - 1)
                    writer.Write(delimiter);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteLine(int lineNo, TextWriter writer, string text, string EOLDelimiter)
        {
            if (EOLDelimiter == null)
            {
                if (lineNo == 0)
                    writer.Write(text);
                else
                    writer.Write($"{Environment.NewLine}{text}");
            }
            else
            {
                if (lineNo == 0)
                    writer.Write(text);
                else
                    writer.Write($"{EOLDelimiter}{text}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<T> GetRecords<T>(T firstRec, IEnumerator<T> records)
        {
            yield return firstRec;
            while (records.MoveNext())
                yield return records.Current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetFirstNotNullRecord<T>(IEnumerator<T> recEnum, ref T rec)
        {
            while (recEnum.MoveNext())
            {
                if (recEnum.Current != null)
                {
                    rec = recEnum.Current;
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
        }
    }

    public interface IChoCSVLiteWriter
    {
    }
}
