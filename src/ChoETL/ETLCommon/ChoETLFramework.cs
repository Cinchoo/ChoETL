using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

//[assembly: System.Security.Permissions.FileIOPermission( System.Security.Permissions.SecurityAction.RequestRefuse, All =@"C:\")]

namespace ChoETL
{
    // An enumerated type for the control messages 
    // sent to the handler routine.
    public enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }
    public delegate Boolean ConsoleCtrlMessageHandler(CtrlTypes CtrlType);

    public static class ChoETLFrxBootstrap
    {
        public static bool IsSandboxEnvironment
        {
            get;
            set;
        }

        public static TraceLevel? TraceLevel
        {
            get;
            set;
        }

        public static void LogIf(bool condition, string msg)
        {
            if (!condition)
                return;

            if (msg == null)
                msg = String.Empty;

            Log(msg);
        }

        private static Action<string> _defaultLog = ((m) =>
        {
            Console.WriteLine(m);
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                Trace.WriteLine(m);
        });
        public static Action<string> Log
        {
            get { return _defaultLog; }
            set
            {
                if (value != null)
                    _defaultLog = value;
            }
        }

        public static string ConfigDirectory
        {
            get;
            set;
        }

        private static string _applicationName;
        public static string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (value.IsNullOrWhiteSpace()) return;
                _applicationName = value;
            }
        }

        private static string _logFileName = null;
        public static string LogFileName
        {
            get
            {
                if (_logFileName.IsNullOrWhiteSpace())
                    return ApplicationName;
                else
                    return _logFileName;
            }
            set
            {
                _logFileName = value;
            }
        }

        private static string _logFolder = null;
        public static string LogFolder
        {
            get
            {
                if (_logFolder.IsNullOrWhiteSpace())
                    return Path.Combine(ChoPath.EntryAssemblyBaseDirectory, "Log");
                else
                    return _logFolder;
            }
            set
            {
                _logFolder = value;
            }
        }

        public static bool EnableLoadingReferencedAssemblies { get; set; }
        public static readonly HashSet<string> IgnoreLoadingAssemblies = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        public static Configuration Configuration { get; set; }
    }

    public static class ChoETLFramework
    {
        public static readonly TraceSwitch TraceSwitchOff = new TraceSwitch("_ChoETLSwitchOff_", "ChoETL Trace Switch", "Off");
        private static TraceSwitch _switch = new TraceSwitch("ChoETLSwitch", "ChoETL Trace Switch", "Info");
        public static TraceSwitch TraceSwitch
        {
            get { return _switch; }
        }

        private static TraceLevel? _traceLevel;
        private static TraceLevel TraceLevel
        {
            get { return _switch.Level; }
            set 
            {
                _traceLevel = value;
            }
        }

        private static Lazy<string> _batchId = new Lazy<string>(() =>
        {
            string BatchId = "%BATCH_ID%".ExpandProperties();
            if (BatchId.IsNullOrWhiteSpace() || BatchId == "%BATCH_ID%")
                BatchId = new ChoCryptoRandom().Next().ToString();

            ChoETLFramework.WriteLog("BATCH_ID: {0}".FormatString(BatchId));
            return BatchId;
        });

        public static string BatchId
        {
            get { return _batchId.Value; }
        }

        public static ChoProfile GlobalProfile
        {
            get;
            private set;
        }

#if !NETSTANDARD2_0
        private static EventLog _elApplicationEventLog;
#endif
        private readonly static Action<string> _defaultLog = (msg) =>
        {
            if (GlobalProfile == null)
                return;

            if (msg.IsNullOrWhiteSpace())
                GlobalProfile.AppendIf(true, msg);
            else
                GlobalProfile.AppendIf(true, String.Format("{0} {1}".FormatString(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"), msg)));
        };
        private static Action<string> _log;
        private static Action<string> Log
        {
            get { return _log ?? _defaultLog; }
            set { _log = value; }
        }

        private static ConsoleCtrlMessageHandler _consoleCtrlHandler;
        private static ChoTextWriterTraceListener _frxTextWriterTraceListener;
        public static event EventHandler<ChoEventArgs<object>> ObjectInitialize;

        private static ChoIniFile _iniFile;
        internal static ChoIniFile IniFile
        {
            get { return _iniFile; }
            set { _iniFile = value; }
        }

#if !NETSTANDARD2_0
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlMessageHandler consoleCtrlRoutine, bool Add);
#endif

        static ChoETLFramework()
        {
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                _Initialize();
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            GlobalProfile.Dispose();
        }

        public static void Initialize()
        {

        }

        private static void _Initialize()
        {
            try
            {
                if (ChoETLFrxBootstrap.TraceLevel != null)
                    TraceLevel = ChoETLFrxBootstrap.TraceLevel.Value;
                if (ChoETLFrxBootstrap.Log != null)
                    Log = ChoETLFrxBootstrap.Log;

                if (ChoETLFrxBootstrap.ApplicationName.IsNullOrWhiteSpace())
                    ChoETLFrxBootstrap.ApplicationName = ChoPath.EntryAssemblyName;

                if (IniFile == null)
                    IniFile = ChoIniFile.New(ChoPath.EntryAssemblyName);

//#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
//#endif
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                if (GetConfigValue<bool>("TurnOnConsoleCtrlHandler", false))
                    RegisterConsoleControlHandler();

#if !NETSTANDARD2_0

                try
                {
                    _elApplicationEventLog = new EventLog("Application", Environment.MachineName, ChoETLFrxBootstrap.ApplicationName);
                    _elApplicationEventLog.Log = "Application";
                    _elApplicationEventLog.Source = ChoETLFrxBootstrap.ApplicationName;
                }
                catch (Exception ex)
                {
                    WriteLog(ChoETLFramework.TraceSwitch.TraceError, ex.ToString());
                }
#endif

                if (_traceLevel == null)
                {
#if DEBUG
                    _switch.Level = GetConfigValue<TraceLevel>("TraceLevel", TraceLevel.Verbose);
#else
                    _switch.Level = GetConfigValue<TraceLevel>("TraceLevel", TraceLevel.Off);
#endif
                }
                else
                    _switch.Level = GetConfigValue<TraceLevel>("TraceLevel", _traceLevel.Value);

                var x = GetConfigValue<string>("LogFileName", ChoETLFrxBootstrap.ApplicationName);
                if (ChoETLFramework.TraceLevel != TraceLevel.Off)
                {
                    ChoTextWriterTraceListener frxTextWriterTraceListener = new ChoTextWriterTraceListener("ChoETL",
                        String.Format("BASEFILENAME={0};DIRECTORYNAME={1};FILEEXT={2};TIMESTAMP=false",
                        GetConfigValue<string>("LogFileName", ChoETLFrxBootstrap.LogFileName),
                        GetConfigValue<string>("LogFileDir", ChoETLFrxBootstrap.LogFolder), "log"));

                    if (_frxTextWriterTraceListener != null)
                        System.Diagnostics.Trace.Listeners.Remove(_frxTextWriterTraceListener);

                    _frxTextWriterTraceListener = frxTextWriterTraceListener;
                    System.Diagnostics.Trace.Listeners.Add(_frxTextWriterTraceListener);
                }
                GlobalProfile = new ChoProfile(ChoETLFramework.TraceSwitch.TraceVerbose, "Time taken to run the application...");

                if (ChoAppSettings.Configuration != null)
                    ChoETLLog.Info("Configuration File Path: " + ChoAppSettings.Configuration.FilePath);
            }
            catch (Exception ex)
            {
                if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                    Exit(ex);
            }
            finally
            {
            }
        }

        public static void InitializeObject(object value)
        {
            ObjectInitialize.Raise(null, new ChoEventArgs<object>(value));
        }

#if !NETSTANDARD2_0
        public static void WriteToEventLog(string message, EventLogEntryType type)
        {
            CheckInitCalled();

            try
            {
                if (type == EventLogEntryType.Error ||
                    type == EventLogEntryType.FailureAudit)
                    WriteLog(ChoETLFramework.TraceSwitch.TraceError, message);
                else
                    WriteLog(message);

                try
                {
                    if (!GetConfigValue<bool>("DisableEventLog"))
                    {
                        if (_elApplicationEventLog != null)
                            _elApplicationEventLog.WriteEntry(message, type);
                    }
                }
                catch (SecurityException sEx)
                {
                    WriteLog(ChoETLFramework.TraceSwitch.TraceError, sEx.ToString());
                }
            }
            catch { } //If the event log is full or any other errors while writing to event log, we dont need to let the service to stop working or die
        }
#endif
        public static void WriteLog(string msg)
        {
            if (Log == null) return;
            WriteLog(ChoETLFramework.TraceSwitch.TraceVerbose, msg);
        }

        public static void WriteLog(bool condition, string msg)
        {
            CheckInitCalled();

            if (!condition) return;
            if (Log == null) return;
            Log(msg);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;
            Exit(exception);
        }

        private static void Exit(Exception exception)
        {
            string message = "An application error occurred. Please contact the administrator with the following information:\n\n";
            if (exception != null)
            {
                message += ChoException.ToString(exception);
                exception = (Exception)null;
            }
            else
                message += "Unknown exception occurred.";

            if (ChoETLFramework.TraceSwitch.TraceError)
                WriteLog(ChoETLFramework.TraceSwitch.TraceError, message);
            else
                Console.WriteLine(message);
            //System.Diagnostics.Trace.WriteLine(message);
            Environment.Exit(-1);
        }

        private static void RegisterConsoleControlHandler()
        {
#if !NETSTANDARD2_0
            _consoleCtrlHandler = new ConsoleCtrlMessageHandler(ConsoleCtrlHandler);
            GC.KeepAlive((object)_consoleCtrlHandler);
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);
#endif
        }

        internal static bool ConsoleCtrlHandler(CtrlTypes ctrlType)
        {
            string message = "This message should never be seen!";
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    message = "A CTRL_C_EVENT was raised by the user.";
                    break;
                case CtrlTypes.CTRL_BREAK_EVENT:
                    message = "A CTRL_BREAK_EVENT was raised by the user.";
                    break;
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    message = "A CTRL_CLOSE_EVENT was raised by the user.";
                    break;
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                    message = "A CTRL_LOGOFF_EVENT was raised by the user.";
                    break;
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    message = "A CTRL_SHUTDOWN_EVENT was raised by the user.";
                    break;
            }
            throw new ChoConsoleCtrlException(message);
        }

        public static bool HandleException(ref Exception ex)
        {
            if (ex is ChoFatalApplicationException)
            {
                WriteLog(ChoETLFramework.TraceSwitch.TraceError, ex.ToString());
                Environment.Exit(-100);
                return false;
            }
            else if (ex is TargetInvocationException)
                ex = ex.InnerException;

            return true;
        }

        public static T GetConfigValue<T>(string key, T defaultValue = default(T))
        {
            return GetConfigValue(key, defaultValue.ToNString()).CastTo<T>();
        }

        public static string GetConfigValue(string key, string defaultValue = null)
        {
            CheckInitCalled();

            if (IniFile != null && IniFile.Contains(key))
                return IniFile.GetValue(key, defaultValue, false);
            else
                return ChoAppSettings.GetValue(key, defaultValue);
        }

        public static void SetConfigValue(string key, string value)
        {
            CheckInitCalled();

            if (IniFile != null && IniFile.Contains(key))
                IniFile.SetValue(key, value);
            else
                ChoAppSettings.SetValue(key, value);
        }

        public static T GetIniValue<T>(string key, T defaultValue = default(T))
        {
            return GetIniValue(key, defaultValue.ToNString()).CastTo<T>();
        }

        public static string GetIniValue(string key, string defaultValue = null)
        {
            CheckInitCalled();

            if (IniFile != null)
                return IniFile.GetValue(key, defaultValue, true);
            else
                return defaultValue;
        }

        public static void SetIniValue(string key, string value)
        {
            CheckInitCalled();

            if (IniFile != null)
                IniFile.SetValue(key, value);
        }

        public static ChoIniFile OpenIniSection(string sectionName = null)
        {
            CheckInitCalled();

            if (sectionName == null)
                return IniFile;
            else
                return ChoIniFile.New(IniFile.FilePath, sectionName);
        }

        private static void CheckInitCalled()
        {
            //if (!_isInitialized)
            //    throw new ChoFatalApplicationException("ChoFramework.Initialize() method not called to initialize. Please invoke at the beginning of the application.");
        }

        public static void Shutdown()
        {
            if (_frxTextWriterTraceListener != null)
            {
                _frxTextWriterTraceListener.Flush();
                System.Diagnostics.Trace.Listeners.Remove(_frxTextWriterTraceListener);
            }
        }
    }
}
