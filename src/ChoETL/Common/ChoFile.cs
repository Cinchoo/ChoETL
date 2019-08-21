using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoFile
    {
        public static Encoding GetEncodingFromFile(string fileName)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return GetEncodingFromStream(file);
            }
        }

        public static Encoding GetEncodingFromStream(Stream file)
        {
            // Read the BOM
            var bom = new byte[4];
            if (file.CanSeek)
                file.Seek(0, SeekOrigin.Begin);

            file.Read(bom, 0, 4);
            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.Default;
        }

        public static IEnumerable<string> GetNextSequencedFileNameLike(string filePath, int startIndex = 1, int step = 1, int width = 5, string Separator = "_")
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");
            if (startIndex <= 0)
                throw new ArgumentException("StartIndex must be > 0.");
            if (step <= 0)
                throw new ArgumentException("Step must be > 0");
            if (width <= 0)
                throw new ArgumentException("Width must be > 0");

            string dir = Path.GetDirectoryName(filePath);
            string fnWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);
            string format = "D{0}".FormatString(width);

            while (true)
            {
                yield return Path.Combine(dir, "{0}{1}{2}{3}".FormatString(fnWithoutExt, Separator, startIndex.ToString(format), ext));
                startIndex += step;
            }
        }

        public static string PostFix(string filePath, string postFix)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");
            ChoGuard.ArgumentNotNullOrEmpty(postFix, "PostFix");

            string dir = Path.GetDirectoryName(filePath);
            string fnWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            return Path.Combine(dir, "{0}{1}{2}".FormatString(fnWithoutExt, postFix, ext));
        }

        public static string PreFix(string filePath, string preFix)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");
            ChoGuard.ArgumentNotNullOrEmpty(preFix, "PreFix");

            string dir = Path.GetDirectoryName(filePath);
            string fnWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            return Path.Combine(dir, "{0}{1}{2}".FormatString(preFix, fnWithoutExt, ext));
        }

        public static void AppendFileTo(string srcFilePath, string destFilePath, int bufferSize = 4096)
        {
            ChoGuard.ArgumentNotNullOrEmpty(srcFilePath, "srcFilePath");
            ChoGuard.ArgumentNotNullOrEmpty(destFilePath, "destFilePath");

            using (var outputStream = File.OpenWrite(destFilePath))
            {
                using (var inputStream = File.OpenRead(srcFilePath))
                {
                    // Buffer size can be passed as the second argument.
                    inputStream.CopyTo(outputStream, bufferSize);
                }
            }
        }

        public static void CombineFiles(string srcFilePathPattern, string destFilePath, int bufferSize = 4096, string EOFDelimiter = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(srcFilePathPattern, "srcFilePathPattern");
            ChoGuard.ArgumentNotNullOrEmpty(destFilePath, "destFilePath");

            if (EOFDelimiter == null)
                EOFDelimiter = Environment.NewLine;

            using (var outputStream = File.OpenWrite(destFilePath))
            {
                foreach (string fp in ChoDirectory.GetFilesBeginWith(srcFilePathPattern).OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase))
                {
                    if (new FileInfo(fp).Length == 0) continue;
                    using (var inputStream = File.OpenRead(fp))
                    {
                        if (outputStream.Position != 0)
                            outputStream.Write(EOFDelimiter);

                        // Buffer size can be passed as the second argument.
                        inputStream.CopyTo(outputStream, bufferSize);
                    }
                }
            }
        }

        public static void DeleteAll(string srcFilePathPattern)
        {
            ChoGuard.ArgumentNotNullOrEmpty(srcFilePathPattern, "srcFilePathPattern");

            foreach (string fp in ChoDirectory.GetFilesBeginWith(srcFilePathPattern).OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase))
            {
                File.Delete(fp);
            }
        }

        public static void Move(string srcFilePath, string destFilePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(srcFilePath, "SrcFilePath");
            ChoGuard.ArgumentNotNullOrEmpty(destFilePath, "destFilePath");

            if (File.Exists(destFilePath))
                File.Delete(destFilePath);

            File.Move(srcFilePath, destFilePath);
        }

        public static string ReadAllText(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");
            filePath = ChoPath.GetFullPath(filePath);

            if (File.Exists(filePath))
                return File.ReadAllText(filePath);
            else
            {
                try
                {
                    File.CreateText(filePath);
                }
                catch { }
                return String.Empty;
            }
        }

        public static string[] ReadAllLines(string filePath)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");
            filePath = ChoPath.GetFullPath(filePath);

            if (File.Exists(filePath))
                return File.ReadAllLines(filePath);
            else
            {
                try
                {
                    File.CreateText(filePath);
                }
                catch { }
                return new string[] {};
            }
        }
    }
}
