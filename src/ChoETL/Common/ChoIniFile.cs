using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoIniKeyValueArgs : EventArgs
    {
        public string IniFilePath
        {
            get;
            set;
        }
                
        public string SectionName
        {
            get;
            set;
        }

        public string IniKey
        {
            get;
            set;
        }
        public string IniValue
        {
            get;
            set;
        }
    }

    [DebuggerDisplay("Key = {Key}")]
    public class ChoIniFile : IDisposable
    {
        private readonly static object _iniFileLockObjDictLock = new object();
        private readonly static Dictionary<string, object> _iniFileLockObjDict = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
        private readonly static Dictionary<string, ChoIniFile> _iniFiles = new Dictionary<string, ChoIniFile>(StringComparer.CurrentCultureIgnoreCase);

        public static event EventHandler<ChoIniKeyValueArgs> IniKeyValueOverride;

        private readonly object _padLock = new object();
        private readonly string _iniFilePath;
        public string IniFilePath
        {
            get { return _iniFilePath; }
        }
        private readonly string _sectionName;
        public string SectionName
        {
            get { return _sectionName; }
        }
        private readonly Dictionary<string, string> _keyValues = new Dictionary<string, string>();
        public readonly string Key;
        public string FilePath
        {
            get { return _iniFilePath; }
        }

        private static string IniFileKey(string iniFilePath, string sectionName = null)
        {
            return String.Format("{0}_{1}", iniFilePath, sectionName);
        }

        private ChoIniFile(string iniFilePath, string sectionName = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(iniFilePath, "IniFilePath");

            _iniFilePath = GetFullPath(iniFilePath);
            if (!_iniFilePath.EndsWith(".ini", StringComparison.InvariantCultureIgnoreCase))
                _iniFilePath = _iniFilePath + ".ini";

            _padLock = GetIniFileLockObject(_iniFilePath);
            _sectionName = sectionName;

            LoadIniFile();
            Key = IniFileKey(_iniFilePath, sectionName);
        }

        public static ChoIniFile New(string iniFilePath, string sectionName = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(iniFilePath, "IniFilePath");

            string key = IniFileKey(iniFilePath, sectionName);
            if (_iniFiles.ContainsKey(key))
                return _iniFiles[key];

            lock (_iniFileLockObjDictLock)
            {
                if (_iniFiles.ContainsKey(key))
                    return _iniFiles[key];

                ChoIniFile iniFile = new ChoIniFile(iniFilePath, sectionName);
                _iniFiles.Add(key, iniFile);
                return _iniFiles[key];
            }
        }

        public ChoIniFile GetSection(string sectionName = null)
        {
            return New(_iniFilePath, sectionName);
        }

        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            path = ChoPath.GetFullPath(path, ChoETLFrxBootstrap.ConfigDirectory);
            return Path.Combine(Path.Combine(Path.GetDirectoryName(path), "Config"), Path.GetFileName(path));
        }

        private object GetIniFileLockObject(string name)
        {
            if (_iniFileLockObjDict.ContainsKey(name))
                return _iniFileLockObjDict[name];

            lock (_iniFileLockObjDictLock)
            {
                if (_iniFileLockObjDict.ContainsKey(name))
                    return _iniFileLockObjDict[name];

                _iniFileLockObjDict.Add(name, new object());
                return _iniFileLockObjDict[name];
            }
        }

        private void LoadIniFile()
        {
            string key;
            string value;
            foreach (string line in GetSection().Split(Environment.NewLine))
            {
                foreach (KeyValuePair<string, string> kvp in line.ToKeyValuePairs())
                {
                    if (!kvp.Key.IsNullOrWhiteSpace())
                    {
                        key = kvp.Key.Trim();
                        value = kvp.Value.Trim();

                        if (!_keyValues.ContainsKey(key))
                            _keyValues.Add(key, CleanValue(value));
                        else
                        {
                            if (!SectionName.IsNullOrWhiteSpace())
                                throw new ApplicationException("Duplicate '{0}' ini key found in '{1}' section.".FormatString(key, SectionName));
                            else
                                throw new ApplicationException("Duplicate '{0}' ini key found in root section.".FormatString(key, SectionName));
                        }
                    }
                    break;
                }

                //string[] keyValue = line.SplitNTrim("=", ChoStringSplitOptions.None);
                //if (keyValue.Length != 2
                //    || keyValue[0].IsNullOrWhiteSpace()) continue;

                //if (!_keyValues.ContainsKey(keyValue[0]))
                //    _keyValues.Add(keyValue[0], CleanValue(keyValue[1]));
                //else
                //{
                //    if (!SectionName.IsNullOrWhiteSpace())
                //        throw new ApplicationException("Duplicate '{0}' ini key found in '{1}' section.".FormatString(keyValue[0], SectionName));
                //    else
                //        throw new ApplicationException("Duplicate '{0}' ini key found in root section.".FormatString(keyValue[0], SectionName));
                //}
                //_keyValues[keyValue[0]] = CleanValue(keyValue[1]);
            }
        }

        private string CleanValue(string value)
        {
            if (value == null) return value;
            value = value.Trim();

            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);
            else if (value.StartsWith("'") && value.EndsWith("'"))
                return value.Substring(1, value.Length - 2);
            else
                return value;
        }

        private string NormalizeValue(string value)
        {
            if (value == null) return value;
            value = value.Trim();

            if (value.Contains("="))
                return "'{0}'".FormatString(value);
            else
                return value;
        }

        public string this[string key]
        {
            get { return GetValue(key); }
            set { SetValue(key, value); }
        }

        public bool Contains(string key)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
            return _keyValues.ContainsKey(key);
        }

        public T GetValue<T>(string key, T defaultValue = default(T), bool saveDefaultValue = false)
        {
            try
            {
                return GetValue(key, defaultValue.ToNString(), saveDefaultValue).CastTo<T>(defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string GetValue(string key, string defaultValue = null, bool saveDefaultValue = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");

            if (!_keyValues.ContainsKey(key))
            {
                if (!saveDefaultValue)
                    _keyValues.Add(key, null);
                else
                    _keyValues.Add(key, defaultValue);

                Save();
            }

            //if (!saveDefaultValue)
            EventHandler<ChoIniKeyValueArgs> iniKeyValueResolve = IniKeyValueOverride;
            if (iniKeyValueResolve != null)
            {
                ChoIniKeyValueArgs a = new ChoIniKeyValueArgs()
                {
                    IniFilePath = FilePath,
                    SectionName = SectionName,
                    IniKey = key,
                    IniValue = _keyValues[key].IsNullOrEmpty() ? defaultValue : _keyValues[key]
                };
                iniKeyValueResolve(this, a);
                return a.IniValue;
            }
            else
                return _keyValues[key].IsNullOrEmpty() ? defaultValue : _keyValues[key];
            //else
            //    return _keyValues[key];
        }

        public void SetValue(string key, string value)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
            if (value == null)
                value = String.Empty;

            if (!_keyValues.ContainsKey(key))
            {
                _keyValues.Add(key, value);
                Save();
            }
            else if (String.Compare(_keyValues[key], value, true) != 0)
            {
                _keyValues[key] = value;
                Save();
            }
        }

        private string GetSection()
        {
            lock (_padLock)
            {
                try
                {
                    if (File.Exists(_iniFilePath))
                    {
                        string iniContents = File.ReadAllText(_iniFilePath);
                        if (_sectionName.IsNullOrWhiteSpace())
                        {
                            Match m = Regex.Match(iniContents, @"^(?<ini>[^\[]*)");
                            if (m.Success)
                                return m.Groups["ini"].Value;
                        }
                        else
                        {
                            var match = (from m in Regex.Matches(iniContents, @"^\[[^\]\r\n]+](?:\r?\n(?:[^[\r\n].*)?)*", RegexOptions.Multiline).AsTypedEnumerable<Match>()
                                        where m.Value.StartsWith("[{0}]".FormatString(_sectionName), StringComparison.CurrentCultureIgnoreCase)
                                        select m).FirstOrDefault();
                            return match != null ? match.Value : String.Empty;
                            //Match m = Regex.Match(iniContents, @"(?<ini>\[{0}\][^\[]*)".FormatString(_sectionName), RegexOptions.IgnoreCase);
                            //if (m.Success)
                            //    return m.Groups["ini"].Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ChoETLLog.Error(ex.ToString());
                }
            }
            return String.Empty;
        }

        private void Save()
        {
            lock (_padLock)
            {
                try
                {
                    if (ChoIniFile.New(this.IniFilePath).GetValue<bool>("IsLocked", false))
                        return;

                    Directory.CreateDirectory(Path.GetDirectoryName(_iniFilePath));

                    string iniContents = String.Empty;
                    if (File.Exists(_iniFilePath))
                        iniContents = File.ReadAllText(_iniFilePath);

                    if (_sectionName.IsNullOrWhiteSpace())
                    {
                        iniContents = Regex.Replace(iniContents, @"^(?<ini>[^\[]*)", Matcher, RegexOptions.IgnoreCase);
                        File.WriteAllText(_iniFilePath, iniContents);
                    }
                    else
                    {
                        if (!Regex.IsMatch(iniContents, @"(?<ini>\[{0}\][^\[]*)".FormatString(_sectionName)))
                            iniContents += "{0}[{1}]{0}".FormatString(Environment.NewLine, _sectionName);

                        iniContents = Regex.Replace(iniContents, @"(?<ini>\[{0}\][^\[]*)".FormatString(_sectionName), Matcher, RegexOptions.IgnoreCase);
                        File.WriteAllText(_iniFilePath, iniContents);
                    }
                }
                catch (Exception ex)
                {
                    ChoETLLog.Error(ex.ToString());
                }
            }
        }

        string Matcher(Match m)
        {
            if (m.Groups.Count != 2)
            {
                return m.Value;
            }

            return string.Join("", m.Groups
                                     .OfType<Group>() //for LINQ
                                     .Select((g, i) => i == 1 ? ToIniKeyValues() : g.Value)
                                     .Skip(1) //for Groups[0]
                                     .ToArray());
        }

        private string ToIniKeyValues()
        {
            StringBuilder msg = new StringBuilder();

            if (!_sectionName.IsNullOrWhiteSpace())
                msg.AppendLine("[{0}]".FormatString(_sectionName));

            foreach (string key in _keyValues.Keys)
                msg.AppendLine("{0}={1}".FormatString(key, NormalizeValue(_keyValues[key])));

            return msg.ToString();
        }

        public IEnumerable<KeyValuePair<string, string>> KeyValues
        {
            get 
            { 
                return _keyValues;  
            }
        }

        public Dictionary<string, string> KeyValuesDict
        {
            get
            {
                return _keyValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.CurrentCultureIgnoreCase);
            }
        }

        public override string ToString()
        {
            return ToIniKeyValues();
        }

        public void Dispose()
        {
        }
    }
}
