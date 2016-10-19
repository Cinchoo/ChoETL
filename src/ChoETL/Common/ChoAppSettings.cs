using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ChoETL
{
    public static class ChoAppSettings
    {
        private static Configuration _appConfig = null;

        static ChoAppSettings()
        {
            if (HttpContext.Current == null)
                _appConfig = ConfigurationManager.OpenExeConfiguration(null);
            else
                _appConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);

            Trace.WriteLine("Config File Path: " + _appConfig.FilePath);
        }

        public static bool Contains(string key)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
            return _appConfig.AppSettings.Settings.AllKeys.Contains(key);
        }

        public static T GetValue<T>(string key, T defaultValue = default(T), bool saveDefaultValue = false)
        {
            return GetValue(key, defaultValue.ToNString(), saveDefaultValue).CastTo<T>();
        }

        public static string GetValue(string key, string defaultValue = null, bool saveDefaultValue = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");

            if (_appConfig.AppSettings.Settings[key] == null)
            {
                //_appConfig.AppSettings.Settings.Add(key, defaultValue == null ? String.Empty : defaultValue);
                _appConfig.AppSettings.Settings.Add(key, saveDefaultValue ? defaultValue : String.Empty);
                if (saveDefaultValue)
                    Save();
            }
            //else if (_appConfig.AppSettings.Settings[key].Value.IsNullOrEmpty())
            //{
            //    _appConfig.AppSettings.Settings[key].Value = defaultValue == null ? String.Empty : defaultValue;
            //    Save();
            //}

            return _appConfig.AppSettings.Settings[key].Value.IsNullOrWhiteSpace() ? defaultValue : _appConfig.AppSettings.Settings[key].Value;
        }

        public static void SetValue(string key, string value)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
            if (value == null)
                value = String.Empty;

            if (_appConfig.AppSettings.Settings[key] == null)
            {
                _appConfig.AppSettings.Settings.Add(key, value);
                Save();
            }
            else
            {
                _appConfig.AppSettings.Settings[key].Value = value;
                Save();
            }
        }

        private static void Save()
        {
            try
            {
                _appConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ChoETLFramework.Switch.TraceError, ex.ToString());
            }
        }
    }
}
