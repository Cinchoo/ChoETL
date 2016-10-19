using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDirectory
    {
        public static IEnumerable<string> GetFilesBeginWith(string filePath, SearchOption searchOption = SearchOption.AllDirectories)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            string dir = Path.GetDirectoryName(filePath);
            string fnWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            foreach (string sfp in Directory.GetFiles(dir, "{0}*{1}".FormatString(fnWithoutExt, ext)))
                yield return sfp;
        }

        public static IEnumerable<string> GetFilesEndWith(string filePath, SearchOption searchOption = SearchOption.AllDirectories)
        {
            ChoGuard.ArgumentNotNullOrEmpty(filePath, "FilePath");

            string dir = Path.GetDirectoryName(filePath);
            string fnWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            foreach (string sfp in Directory.GetFiles(dir, "*{0}{1}".FormatString(fnWithoutExt, ext)))
                yield return sfp;
        }
    }
}
