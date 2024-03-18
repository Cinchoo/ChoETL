using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public unsafe struct ChoCSVLiteReader : IChoCSVLiteReader, IChoLiteParser, IDisposable
    {
        private const int MAX_LINE_SIZE = 32768;
        fixed char line[MAX_LINE_SIZE];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> ReadText<T>(string csv, bool hasHeader = false, char delimiter = ',', char quoteChar = '\"',
            bool mayContainEOLInData = true, Action<int, string[], T> mapper = null) where T : new()
        {
            this.Validate<T>();

            int lineNo = 0;
            T rec = default(T);
            bool skip = false;
            string[] headers = null;
            bool positionalMapping = this.IsPositionalMapping<T>();
            foreach (var cols in ReadText(csv, delimiter, quoteChar, mayContainEOLInData))
            {
                rec = this.Map(lineNo++, cols, mapper, hasHeader, ref skip, ref headers, positionalMapping);
                if (skip)
                    continue;
                yield return rec;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> ReadFile<T>(string filename, bool hasHeader = false, char delimiter = ',', char quoteChar = '\"',
            bool mayContainEOLInData = true, Action<int, string[], T> mapper = null) where T : new()
        {
            this.Validate<T>();

            int lineNo = 0;
            T rec = default(T);
            bool skip = false;
            string[] headers = null;
            bool positionalMapping = this.IsPositionalMapping<T>();
            using (var sr = new StreamReader(filename))
            {
                foreach (var cols in Read(sr, delimiter, null, quoteChar, mayContainEOLInData))
                {
                    rec = this.Map(lineNo++, cols, mapper, hasHeader, ref skip, ref headers, positionalMapping);
                    if (skip)
                        continue;
                    yield return rec;
                }
            }
        }

        public IEnumerable<T> Read<T>(StreamReader reader, bool hasHeader = false, char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"',
            bool mayContainEOLInData = true, Action<int, string[], T> mapper = null) where T : new()
        {
            this.Validate<T>();

            int lineNo = 0;
            T rec = default(T);
            bool skip = false;
            string[] headers = null;
            bool positionalMapping = this.IsPositionalMapping<T>();
            foreach (var cols in Read(reader, delimiter, EOLDelimiter, quoteChar, mayContainEOLInData))
            {
                rec = this.Map(lineNo++, cols, mapper, hasHeader, ref skip, ref headers, positionalMapping);
                if (skip)
                    continue;
                yield return rec;
            }
        }

        public IEnumerable<T> ReadLines<T>(IEnumerable<string> lines, bool hasHeader = false, char delimiter = ',', char quoteChar = '\"',
            Action<int, string[], T> mapper = null) where T : new()
        {
            this.Validate<T>();

            int lineNo = 0;
            T rec = default(T);
            bool skip = false;
            string[] headers = null;
            bool positionalMapping = this.IsPositionalMapping<T>();
            foreach (var cols in ReadLines(lines, delimiter, quoteChar))
            {
                rec = this.Map(lineNo++, cols, mapper, hasHeader, ref skip, ref headers, positionalMapping);
                if (skip)
                    continue;
                yield return rec;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string[]> ReadText(string csv, char delimiter = ',', char quoteChar = '\"', bool mayContainEOLInData = true)
        {
            if (csv.IsNullOrWhiteSpace())
                return Enumerable.Empty<string[]>();

            this.Validate(delimiter, null, quoteChar);

            return Read(new StreamReader(csv.ToStream()), delimiter, null, quoteChar, mayContainEOLInData);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public IEnumerable<string[]> ReadFile(string filename, char delimiter = ',', char quoteChar = '\"', bool mayContainEOLInData = true)
        //{
        //    if (filename.IsNullOrWhiteSpace())
        //        throw new ArgumentException("Invalid filename passed.");

        //    this.Validate(delimiter, null, quoteChar);

        //    if (mayContainEOLInData)
        //    {
        //        using (var r = new StreamReader(filename))
        //            return Read(r, delimiter, null, quoteChar, mayContainEOLInData);
        //    }
        //    else
        //        return ParseLines(File.ReadLines(filename), delimiter, quoteChar);
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string[]> Read(StreamReader reader, char delimiter = ',', string EOLDelimiter = null, char quoteChar = '\"',
            bool mayContainEOLInData = true)
        {
            if (reader == null)
                throw new ArgumentException("Invalid stream reader passed.");
            this.Validate(delimiter, EOLDelimiter, quoteChar);

            return ParseLines(ReadLines(reader, EOLDelimiter, quoteChar, mayContainEOLInData), delimiter, quoteChar);
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public IEnumerable<string[]> ReadLines(IEnumerable<string> lines, char delimiter = ',', char quoteChar = '\"')
        {
            if (lines == null)
                throw new ArgumentException("Invalid lines passed.");
            this.Validate(delimiter, null, quoteChar);

            return ParseLines(lines, delimiter, quoteChar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<string> ReadLines(StreamReader reader, string EOLDelimiter = null, char quoteChar = '\"',
            bool mayContainEOLInData = true, int maxLineSize = MAX_LINE_SIZE)
        {
            if (!mayContainEOLInData)
            {
                foreach (var line in ReadLinesFromStream(reader, EOLDelimiter, maxLineSize))
                    yield return line;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                bool insb = false;
                string cline = null;
                foreach (var line in ReadLinesFromStream(reader, EOLDelimiter, maxLineSize))
                {
                    var qc = CountOccurence(line, quoteChar);
                    bool multiline = qc % 2 == 1 || insb;

                    cline = line;
                    // if multiline add line to sb and continue
                    if (multiline)
                    {
                        insb = true;
                        if (sb.Length > 0)
                            sb.AppendLine();

                        sb.Append(line);
                        var s = sb.ToString();
                        qc = CountOccurence(s, quoteChar);
                        if (qc % 2 == 1)
                        {
                            continue;
                        }
                        cline = s;
                        sb.Clear();
                        insb = false;
                    }

                    yield return cline;
                }

                if (insb)
                    yield return sb.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<string> ReadLinesFromStream(StreamReader reader, string EOLDelimiter, int maxLineSize)
        {
            if (EOLDelimiter == null || EOLDelimiter == Environment.NewLine)
            {
                string l = null;
                while ((l = reader.ReadLine()) != null)
                {
                    yield return l;
                }
            }
            else
            {
                var delimCandidatePosition = 0;
                int index = 0;
                while (!reader.EndOfStream)
                {
                    delimCandidatePosition = LoadLine(reader, EOLDelimiter, maxLineSize, ref index);
                    yield return CreateNewLine(index - (delimCandidatePosition == EOLDelimiter.Length ? EOLDelimiter.Length : 0));
                    index = 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe string CreateNewLine(int len)
        {
            fixed (char* l = line)
                return new string(l, 0, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int LoadLine(StreamReader reader, string EOLDelimiter, int maxLineSize, ref int index)
        {
            int delimCandidatePosition = 0;
            int len = EOLDelimiter.Length;
            fixed (char* l = line)
            {
                fixed (char* s = EOLDelimiter)
                {
                    while (!reader.EndOfStream && delimCandidatePosition < len)
                    {
                        var c = (char)reader.Read();
                        if (c == *(s + delimCandidatePosition))
                        {
                            delimCandidatePosition++;
                        }
                        else
                        {
                            delimCandidatePosition = 0;
                        }
                        *(l + index) = c;
                        index++;
                        if (index > maxLineSize)
                            throw new ChoParserException("Large line found. Check and correct the end of line delimiter.");
                    }
                }
            }
            return delimCandidatePosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<string[]> ParseLines(IEnumerable<string> lines, char delimiter, char quoteChar)
        {
            string[] cols = null;
            int linenum = -1;
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                try
                {
                    linenum++;
                    if (linenum == 0)
                    {
                        int cc = CountOccurence(line, delimiter);
                        cols = new string[cc + 1];
                    }
                    ParseLine(line, delimiter, cols, quoteChar);
                }
                catch (Exception ex)
                {
                    throw new ChoParserException("Error on line " + linenum, ex);
                }
                yield return cols;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int CountOccurence(string text, char c)
        {
            int count = 0;
            int len = text.Length;
            int index = -1;
            fixed (char* s = text)
            {
                while (index++ < len)
                {
                    char ch = *(s + index);
                    if (ch == c)
                        count++;
                }
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ParseLine(string line, char delimiter, string[] columns, char quoteChar)
        {
            int col = 0;
            int linelen = line.Length;
            int index = 0;

            for (int i = 0; i < columns.Length; i++)
                columns[i] = null;

            fixed (char* l = line)
            {
                while (index < linelen)
                {
                    if (*(l + index) != quoteChar)
                    {
                        // non quoted
                        var next = line.IndexOf(delimiter, index);
                        if (next < 0)
                        {
                            if (col < columns.Length)
                                columns[col++] = new string(l, index, linelen - index);
                            else
                                columns[col - 1] = columns[col - 1] + new string(l, index, linelen - index);
                            break;
                        }
                        columns[col++] = l[index] == quoteChar && l[next - index - 1] == quoteChar ? new string(l, index + 1, next - index - 2)
                            : new string(l, index, next - index);
                        index = next + 1;
                    }
                    else
                    {
                        // quoted string change "" -> "
                        int qc = 1;
                        int start = index;
                        char c = *(l + ++index);
                        while (index++ < linelen)
                        {
                            if (c == quoteChar)
                                qc++;
                            if (c == delimiter && qc % 2 == 0)
                                break;
                            c = *(l + index);
                        }
                        if (qc % 2 == 0)
                            columns[col++] = new string(l, start + 1, index - start - 3).Replace($"{quoteChar}{quoteChar}", $"{quoteChar}");
                        else
                            columns[col++] = new string(l, start + 1, index - start - 1);
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }

    internal static class ChoLiteParserEx
    {
        private readonly static ConcurrentDictionary<Type, PropertyInfo[]> _propInfoCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private readonly static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propInfoDictionaryCache = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();
        private readonly static ConcurrentDictionary<Type, bool> _positionalMappingTypeCache = new ConcurrentDictionary<Type, bool>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Map<T>(this IChoCSVLiteReader parser, int lineNo, string[] cols, Action<int, string[], T> mapper,
            bool hasHeader, ref bool skip, ref string[] headers, bool positionalMapping) where T : new()
        {
            skip = false;
            if (mapper != null)
            {
                if (hasHeader)
                {
                    if (lineNo == 0)
                    {
                        skip = true;
                        return default(T);
                    }
                }

                var rec = parser.CreateInstance<T>();
                mapper(lineNo, cols, rec);
                return rec;
            }
            else
            {
                if (hasHeader)
                {
                    if (lineNo == 0)
                    {
                        headers = cols.Select((c, i) => c).ToArray();
                        skip = true;
                        return default(T);
                    }
                }
                else
                {
                    if (lineNo == 0)
                        headers = cols.Select((c, i) => $"Column{i}").ToArray();
                }

                var recType = typeof(T);
                if (recType == typeof(ChoDynamicObject) || recType == typeof(ExpandoObject))
                {
                    if (recType == typeof(ChoDynamicObject))
                    {
                        var rec = parser.CreateInstance<T>();
                        dynamic dObj = rec;

                        var dict = headers.Select((h, i) => new { Key = h, Value = cols[i] }).ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                        dObj.SetDictionary(dict);
                        return rec;
                    }
                    else
                    {
                        var expando = parser.CreateInstance<T>();
                        var expandoDic = (IDictionary<string, object>)expando;
                        int index = 0;
                        foreach (var header in headers)
                            expandoDic.Add(header, cols[index++]);

                        return expando;
                    }
                }
                else
                {
                    var rec = parser.CreateInstance<T>();

                    if (positionalMapping)
                    {
                        PropertyInfo[] props = GetPropertyInfos<T>();
                        int index = 0;
                        string col = null;
                        Type propType = null;
                        foreach (var prop in props)
                        {
                            if (index < cols.Length)
                            {
                                propType = props[index].PropertyType;
                                col = cols[index++];
                                ChoType.SetPropertyValue(rec, prop, ChoConvert.ConvertTo(col, propType));
                            }
                            else
                                break;
                        }
                    }
                    else
                    {
                        Dictionary<string, PropertyInfo> propDict = GetPropertyInfoDictionary<T>();

                        int index = 0;
                        string col = null;
                        Type propType = null;
                        PropertyInfo prop = null;
                        foreach (var header in headers)
                        {
                            if (propDict.ContainsKey(header))
                            {
                                prop = propDict[header];
                                if (prop != null)
                                {
                                    propType = prop.PropertyType;
                                    col = cols[index++];
                                    ChoType.SetPropertyValue(rec, prop, ChoConvert.ConvertTo(col, propType));
                                }
                            }
                        }
                    }
                    return rec;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateInstance<T>(this IChoCSVLiteReader parser)
        {
            var rec = ChoActivator.CreateInstance<T>();
            return rec;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Validate(this IChoLiteParser parser, char delimiter, string EOLDelimiter, char quoteChar)
        {
            if (delimiter == ChoCharEx.NUL)
                throw new ArgumentException("Invalid delimiter passed.");
            if (EOLDelimiter != null && EOLDelimiter.IsNullOrWhiteSpace())
                throw new ArgumentException("Invalid EOLDelimiter passed.");
            if (quoteChar == ChoCharEx.NUL)
                throw new ArgumentException("Invalid Quote character passed.");

            if (EOLDelimiter != null && (EOLDelimiter.Contains(delimiter) || EOLDelimiter.Contains(quoteChar)))
                throw new ArgumentException("Invalid EOLDelimiter passed.");
            if (delimiter == quoteChar)
                throw new ArgumentException("Invalid Quote character passed.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Validate<T>(this IChoLiteParser parser)
        {
            var recType = typeof(T);
            if (recType.IsNullableType())
                throw new NotSupportedException($"{recType} is not supported.");
            if (recType.IsSimple())
                throw new NotSupportedException($"{recType} is not supported.");
            if (recType != typeof(ChoDynamicObject) && recType != typeof(ExpandoObject) && (typeof(IEnumerable).IsAssignableFrom(recType) || typeof(IList).IsAssignableFrom(recType)))
                throw new NotSupportedException($"{recType} is not supported.");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetValues<T>(this IChoCSVLiteWriter parser, T rec, IList<string> cols)
        {
            if (rec is IDictionary<string, object>)
            {
                foreach (var col in ((IDictionary<string, object>)rec).Values)
                    cols.Add(col.ToNString());
            }
            else
            {
                PropertyInfo[] props = GetPropertyInfos<T>();
                foreach (var prop in props)
                    cols.Add(ChoType.GetPropertyValue(rec, prop).ToNString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] GetHeaders<T>(this IChoCSVLiteWriter parser, T rec = default(T))
        {
            if (typeof(IDictionary<string, object>).IsAssignableFrom(typeof(T)) && rec != null)
                return ((IDictionary<string, object>)rec).Keys.ToArray();
            else
            {
                PropertyInfo[] props = GetPropertyInfos<T>();
                return props.Select(p => GetDisplayName(p, p.Name)).ToArray();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositionalMapping<T>(this IChoLiteParser parser)
        {
            bool flag = false;
            if (!_positionalMappingTypeCache.TryGetValue(typeof(T), out flag))
            {
                var recType = typeof(T);
                if (recType == typeof(ChoDynamicObject) || recType == typeof(ExpandoObject))
                    flag = false;
                else
                {
                    var pis = GetPropertyInfos<T>();
                    flag = !pis.Where(pi => ChoType.GetAttribute<DisplayNameAttribute>(pi) != null || ChoType.GetAttribute<DisplayAttribute>(pi) != null).Any();
                    _positionalMappingTypeCache.AddOrUpdate(typeof(T), flag);
                }
            }

            return flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PropertyInfo[] GetPropertyInfos<T>()
        {
            PropertyInfo[] props = null;
            if (!_propInfoCache.TryGetValue(typeof(T), out props))
            {
                props = ChoType.GetProperties(typeof(T)).OrderBy(pi => ChoType.GetAttribute<ColumnAttribute>(pi) == null ? 0 : ChoType.GetAttribute<ColumnAttribute>(pi).Order).ToArray();
                _propInfoCache.AddOrUpdate(typeof(T), props);
            }

            return props;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, PropertyInfo> GetPropertyInfoDictionary<T>()
        {
            Dictionary<string, PropertyInfo> propInfoDict = null;
            if (!_propInfoDictionaryCache.TryGetValue(typeof(T), out propInfoDict))
            {
                PropertyInfo[] props = GetPropertyInfos<T>();

                propInfoDict = props.ToDictionary(p => p.GetCustomAttribute<DisplayNameAttribute>() != null ?
                p.GetCustomAttribute<DisplayNameAttribute>().DisplayName : (p.GetCustomAttribute<DisplayAttribute>() != null ?
                p.GetCustomAttribute<DisplayAttribute>().Name : p.Name), StringComparer.InvariantCultureIgnoreCase);
                
                _propInfoDictionaryCache.AddOrUpdate(typeof(T), propInfoDict);
            }

            return propInfoDict;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetDisplayName(PropertyInfo pi, string defaultValue = null)
        {
            if (pi != null)
            {
                DisplayNameAttribute dnAttr = ChoType.GetAttribute<DisplayNameAttribute>(pi);
                if (dnAttr != null && !dnAttr.DisplayName.IsNullOrWhiteSpace())
                {
                    return dnAttr.DisplayName.Trim();
                }
                else
                {
                    DisplayAttribute dpAttr = ChoType.GetAttribute<DisplayAttribute>(pi);
                    if (dpAttr != null)
                    {
                        if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                            return dpAttr.ShortName.Trim();
                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                            return dpAttr.Name.Trim();
                    }
                }

                return defaultValue == null ? pi.Name : defaultValue;
            }
            else
                return defaultValue;
        }
    }

    public interface IChoCSVLiteReader
    {
    }

    public interface IChoLiteParser
    {
    }
}
