using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ChoETL
{
    public static class ChoPath
    {
        public static string EntryAssemblyBaseDirectory = null;
        public static string EntryAssemblyName = null;

        private static readonly string _fileNameCleanerExpression = "[" + string.Join("", Array.ConvertAll(Path.GetInvalidFileNameChars(), x => Regex.Escape(x.ToString()))) + "]";
        private static readonly Regex _fileNameCleaner = new Regex(_fileNameCleanerExpression); //, RegexOptions.Compiled);
        private static readonly string _pathCleanerExpression = "[" + string.Join("", Array.ConvertAll(Path.GetInvalidPathChars(), x => Regex.Escape(x.ToString()))) + "]";
        private static readonly Regex _pathCleaner = new Regex(_pathCleanerExpression); //, RegexOptions.Compiled);

        static ChoPath()
        {
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                _Initialize();
        }

        private static void _Initialize()
        {
#if !NETSTANDARD2_0
            if (System.Web.HttpContext.Current == null)
            {
#endif
                string loc = Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly().Location : Assembly.GetCallingAssembly().Location;
                EntryAssemblyBaseDirectory = Path.GetDirectoryName(loc);
                EntryAssemblyName = Path.GetFileNameWithoutExtension(loc);
#if !NETSTANDARD2_0
            }
            else
            {
                EntryAssemblyBaseDirectory = System.Web.HttpRuntime.AppDomainAppPath;
                EntryAssemblyName = new DirectoryInfo(System.Web.HttpRuntime.AppDomainAppPath).Name;
            }
#endif
        }

        public static string CleanPath(string path)
        {
            return _pathCleaner.Replace(path, "_");
        }

        public static string CleanFileName(string fileName)
        {
            return _fileNameCleaner.Replace(fileName, "_");
        }

        public static string GetFullPath(string path, string baseDirectory = null)
        {
            if (path.IsNullOrWhiteSpace())
                return path;

            if (Path.IsPathRooted(path))
                return path;
            else if (!baseDirectory.IsNullOrWhiteSpace())
                return GetFullPath(Path.Combine(baseDirectory, path));
            else if (!EntryAssemblyBaseDirectory.IsNullOrEmpty())
                return GetFullPath(Path.Combine(EntryAssemblyBaseDirectory, path));
            else
                return Path.GetFullPath(path);
        }
    }
}
