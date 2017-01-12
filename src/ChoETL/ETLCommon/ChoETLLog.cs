using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoETLLog
    {
        public static void Verbose(string msg)
        {
            ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceVerbose, msg);
        }

        public static void Info(string msg)
        {
            ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceInfo, msg);
        }

        public static void Warning(string msg)
        {
            ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceWarning, msg);
        }

        public static void Error(string msg)
        {
            ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, msg);
        }

        public static void Write(string msg)
        {
            ChoETLFramework.WriteLog(msg);
        }

        public static void Write(bool condition, string msg)
        {
            ChoETLFramework.WriteLog(condition, msg);
        }

    }
}
