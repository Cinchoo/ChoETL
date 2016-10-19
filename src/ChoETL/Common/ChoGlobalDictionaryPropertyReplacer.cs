namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    using System.Diagnostics;
    using System.Threading;
    using System.Configuration;

    #endregion NameSpaces

    [Serializable]
    public class ChoGlobalDictionaryPropertyReplacer : IChoKeyValuePropertyReplacer
    {
        public static readonly ChoGlobalDictionaryPropertyReplacer Instance = new ChoGlobalDictionaryPropertyReplacer();

        #region Instance Data Members (Private)

        private readonly Dictionary<string, string> _availPropeties = new Dictionary<string,string>()
            {
                { "APPLICATION_NAME", "Current application name." },
                { "PROCESS_ID", "Current process identifier." },
                { "THREAD_ID", "Current thread identifier." },
                { "THREAD_NAME", "Current thread name." },
                { "RANDOM_NO", "Random number." },
                { "TODAY", "Today's date." },
                { "NOW", "Current time." }
            };

        #endregion Instance Data Members (Private)

        #region IChoPropertyReplacer Members

        public bool ContainsProperty(string propertyName)
        {
            bool isFound = _availPropeties.ContainsKey(propertyName);
            if (isFound)
                return true;
            else if (IsPropertyExistsInSources(propertyName))
                return true;
            else
                return false; // ResolveNGetPropetyValue(null, propertyName) != propertyName;
        }

        public string ReplaceProperty(string propertyName, string format)
        {
            if (String.IsNullOrEmpty(propertyName)) return propertyName;

            switch (propertyName)
            {
                case "APPLICATION_NAME":
                    return ChoUtility.Format(format, Process.GetCurrentProcess().ProcessName);
                case "PROCESS_ID":
                    return ChoUtility.Format(format, Process.GetCurrentProcess().Id);
                case "THREAD_ID":
                    return ChoUtility.Format(format, Thread.CurrentThread.ManagedThreadId);
                case "THREAD_NAME":
                    return ChoUtility.Format(format, Thread.CurrentThread.Name);
                case "RANDOM_NO":
                    ChoCryptoRandom rnd = new ChoCryptoRandom();
                    return ChoUtility.Format(format, rnd.Next());
                case "TODAY":
                    if (String.IsNullOrEmpty(format))
                        return GetTodaysDate().ToShortDateString();
                    else
                        return ChoUtility.Format(format, GetTodaysDate());
                case "NOW":
                    if (String.IsNullOrEmpty(format))
                        return GetNowTime().ToShortTimeString();
                    else
                        return ChoUtility.Format(format, GetNowTime());
                default:
                    return ResolveNGetPropetyValue(format, propertyName);
            }
        }

        #endregion

        #region IChoPropertyReplacer Members

        private readonly Dictionary<string, bool> _discoveredProperties = new Dictionary<string, bool>();
        private bool IsPropertyExistsInSources(string propertyName)
        {
            if (_discoveredProperties.ContainsKey(propertyName)) return _discoveredProperties[propertyName];

            string cmdLineArgValue = ChoEnvironment.GetCmdLineArgValue(propertyName);

            if (!cmdLineArgValue.IsNullOrWhiteSpace())
            {
                ChoETLFramework.WriteLog("'{0}' property discovered via command line argument.".FormatString(propertyName));
                _discoveredProperties.Add(propertyName, true);
                return true;
            }
            if (ChoETLFramework.IniFile != null && ChoETLFramework.IniFile.Contains(propertyName))
            {
                ChoETLFramework.WriteLog("'{0}' property discovered via application INI file.".FormatString(propertyName));
                _discoveredProperties.Add(propertyName, true);
                return true;
            }
            else if (ChoAppSettings.Contains(propertyName))
            {
                ChoETLFramework.WriteLog("'{0}' property discovered via application config file.".FormatString(propertyName));
                _discoveredProperties.Add(propertyName, true);
                return true;
            }

            ChoETLFramework.WriteLog("'{0}' property NOT found in any sources.".FormatString(propertyName));
            _discoveredProperties.Add(propertyName, false);
            return false;
            //return cmdLineArgValue.IsNullOrWhiteSpace() ? ChoAppSettings.Contains(propertyName) : true;
        }

        private string ResolveNGetPropetyValue(string format, string propertyName)
        {
            string configValue = null;
            string cmdLineArgValue = ChoEnvironment.GetCmdLineArgValue(propertyName);
            if (!cmdLineArgValue.IsNullOrWhiteSpace())
            {
                return cmdLineArgValue;
            }

            configValue = ChoETLFramework.GetConfigValue(propertyName);
            if (!configValue.IsNullOrWhiteSpace())
            {
                try
                {
                    if (format.IsNullOrWhiteSpace())
                        return configValue;
                    else
                        return ChoUtility.Format(format, configValue);
                }
                catch { }
            }

            return propertyName;
        }

        private DateTime GetTodaysDate()
        {
            DateTime today = DateTime.Today;

            string todayText = ChoETLFramework.GetConfigValue("TODAY");
            if (!todayText.IsNullOrWhiteSpace())
            {
                try
                {
                    today = Convert.ToDateTime(todayText);
                }
                catch { }
            }

            return today;
        }

        private DateTime GetNowTime()
        {
            DateTime now = DateTime.Now;

            string nowText = ChoETLFramework.GetConfigValue("NOW");
            if (!nowText.IsNullOrWhiteSpace())
            {
                try
                {
                    now = Convert.ToDateTime(nowText);
                }
                catch { }
            }

            return now;
        }

        public string Name
        {
            get { return this.GetType().FullName; }
        }

        public IEnumerable<KeyValuePair<string, string>> AvailablePropeties
        {
            get
            {
                foreach (KeyValuePair<string, string> keyValue in _availPropeties)
                    yield return keyValue;
            }
        }

        public string GetPropertyDescription(string propertyName)
        {
            if (_availPropeties.ContainsKey(propertyName))
                return _availPropeties[propertyName];
            else
                return null;
        }

        #endregion
    }
}
