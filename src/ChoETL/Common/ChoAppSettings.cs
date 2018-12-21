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
        private static Configuration _configuration = null;
        public static Configuration Configuration
        {
            get { return _configuration; }
            set
            {
                if (value != null)
                    _configuration = value;
            }
        }

        static ChoAppSettings()
        {
            try
            {
                if (ChoETLFrxBootstrap.Configuration == null)
                {
#if !NETSTANDARD2_0
                    if (HttpContext.Current == null)
#endif
                        Configuration = ConfigurationManager.OpenExeConfiguration(null);
#if !NETSTANDARD2_0
                    else
                        Configuration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~/");
#endif
                }
                else
                    Configuration = ChoETLFrxBootstrap.Configuration;
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
                return Configuration == null ? false : Configuration.AppSettings.Settings.AllKeys.Contains(key);
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

            if (Configuration == null)
                return defaultValue;

            try
            {
                if (Configuration.AppSettings.Settings[key] == null)
                {
                    //_appConfig.AppSettings.Settings.Add(key, defaultValue == null ? String.Empty : defaultValue);
                    Configuration.AppSettings.Settings.Add(key, saveDefaultValue ? defaultValue : String.Empty);
                    if (saveDefaultValue)
                        Save();
                }
                //else if (_appConfig.AppSettings.Settings[key].Value.IsNullOrEmpty())
                //{
                //    _appConfig.AppSettings.Settings[key].Value = defaultValue == null ? String.Empty : defaultValue;
                //    Save();
                //}

                return Configuration.AppSettings.Settings[key].Value.IsNullOrWhiteSpace() ? defaultValue : Configuration.AppSettings.Settings[key].Value;
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
            if (Configuration == null)
                return;

            try
            {
                if (value == null)
                    value = String.Empty;

                if (Configuration.AppSettings.Settings[key] == null)
                {
                    Configuration.AppSettings.Settings.Add(key, value);
                    Save();
                }
                else
                {
                    Configuration.AppSettings.Settings[key].Value = value;
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
            if (Configuration == null)
                return;

            try
            {
                Configuration.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                ChoETLLog.Error(ex.ToString());
            }
        }
    }
}
