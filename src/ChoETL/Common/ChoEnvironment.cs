using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEnvironment
    {
        public static T GetCmdLineArgValue<T>(string cmdLineSwitch, T defaultValue = default(T))
        {
            if (typeof(bool) == typeof(T))
            {
                string value = GetCmdLineArgValue(cmdLineSwitch);
                if (value == String.Empty)
                    return (T)Convert.ChangeType(true, typeof(T));
                else if (value == null)
                {
                    value = GetCmdLineArgValue("{0}-".FormatString(cmdLineSwitch));
                    if (value == String.Empty)
                        return (T)Convert.ChangeType(false, typeof(T));
                }
            }
            return GetCmdLineArgValue(cmdLineSwitch).CastTo<T>(defaultValue);
        }
        
        //public static object GetCmdLineArgValue(Type memberType, string cmdLineSwitch, object defaultValue)
        //{
        //    if (typeof(bool) == memberType)
        //    {
        //        string value = GetCmdLineArgValue(cmdLineSwitch);
        //        if (value == String.Empty)
        //            return Convert.ChangeType(true, memberType);
        //        else if (value == null)
        //        {
        //            value = GetCmdLineArgValue("{0}-".FormatString(cmdLineSwitch));
        //            if (value == String.Empty)
        //                return Convert.ChangeType(false, memberType);
        //        }
        //    }
        //    return GetCmdLineArgValue(cmdLineSwitch).CastTo<T>(defaultValue);
        //}

        public static string GetCmdLineArgValue(string cmdLineSwitch)
        {
            //Check if the property is passed as cmdline arg
            var z = (from x in Environment.GetCommandLineArgs()
                     where (String.Compare(x, "/{0}".FormatString(cmdLineSwitch), true) == 0 ||
                     x.StartsWith("/{0}:".FormatString(cmdLineSwitch), StringComparison.CurrentCultureIgnoreCase))
                     select x).FirstOrDefault();

            if (z != null)
            {
                string[] tokens = z.SplitNTrim(":");
                if (tokens.Length == 2)
                    return tokens[1].Trim();
                else
                    return String.Empty;
            }

            return null;
        }

        public static bool IsCmdLineArgExists(string cmdLineSwitch)
        {
            return GetCmdLineArgValue(cmdLineSwitch) != null;
        }
    }
}
