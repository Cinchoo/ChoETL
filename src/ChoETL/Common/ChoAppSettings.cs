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
        private static Configuration _configuation = null;
        public static Configuration Configuation
        {
            get { return _configuation; }
            set
            {
                if (value != null)
                    _configuation = value;
            }
        }

        static ChoAppSettings()
        {
            try
            {
                if (HttpContext.Current == null)
                    Configuation = ConfigurationManager.OpenExeConfiguration(null);
                else
                    Configuation = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~/");
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
            }
        }

        public static bool Contains(string key)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");

            try
            {
                return Configuation == null ? false : Configuation.AppSettings.Settings.AllKeys.Contains(key);
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
                return false;
            }
        }

        public static T GetValue<T>(string key, T defaultValue = default(T), bool saveDefaultValue = false)
        {
            return GetValue(key, defaultValue.ToNString(), saveDefaultValue).CastTo<T>(defaultValue);
        }

        public static string GetValue(string key, string defaultValue = null, bool saveDefaultValue = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");

            if (Configuation == null)
                return defaultValue;

            try
            {
                if (Configuation.AppSettings.Settings[key] == null)
                {
                    //_appConfig.AppSettings.Settings.Add(key, defaultValue == null ? String.Empty : defaultValue);
                    Configuation.AppSettings.Settings.Add(key, saveDefaultValue ? defaultValue : String.Empty);
                    if (saveDefaultValue)
                        Save();
                }
                //else if (_appConfig.AppSettings.Settings[key].Value.IsNullOrEmpty())
                //{
                //    _appConfig.AppSettings.Settings[key].Value = defaultValue == null ? String.Empty : defaultValue;
                //    Save();
                //}

                return Configuation.AppSettings.Settings[key].Value.IsNullOrWhiteSpace() ? defaultValue : Configuation.AppSettings.Settings[key].Value;
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
                return defaultValue;
            }
        }

        public static void SetValue(string key, string value)
        {
            ChoGuard.ArgumentNotNullOrEmpty(key, "Key");
            if (Configuation == null)
                return;

            try
            {
                if (value == null)
                    value = String.Empty;

                if (Configuation.AppSettings.Settings[key] == null)
                {
                    Configuation.AppSettings.Settings.Add(key, value);
                    Save();
                }
                else
                {
                    Configuation.AppSettings.Settings[key].Value = value;
                    Save();
                }
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
            }
        }

        private static void Save()
        {
            if (Configuation == null)
                return;

            try
            {
                Configuation.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
            }
        }
    }
}
