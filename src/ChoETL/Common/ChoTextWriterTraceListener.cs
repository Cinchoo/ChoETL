namespace ChoETL
{
    #region Namespaces

    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    #endregion

    public class ChoTextWriterTraceListener : TextWriterTraceListener
    {
        #region TextTraceMsg Struct

        private enum FileOperation { CreateNew, Rename, WriteLine, Write, Flush }

        private class TextTraceMsg
        {
            public FileOperation Operation;
            public string Msg;
            public string LogFilePath;
            public string PrevLogFilePath; 

            public TextTraceMsg(string logFileName, string msg, FileOperation operation)
            {
                LogFilePath = logFileName;
                Msg = msg;
                Operation = operation;
                PrevLogFilePath = null;
            }

            public TextTraceMsg(string logFileName, string msg, string prevLogFileName)
            {
                LogFilePath = logFileName;
                Msg = msg;
                Operation = FileOperation.Rename;
                PrevLogFilePath = prevLogFileName;
            }

            public static TextTraceMsg FlushTraceMsg
            {
                get { return new TextTraceMsg(null, null, FileOperation.Flush); }
            }

            public bool IsFlushMsg
            {
                get { return Operation == FileOperation.Flush; }
            }
        }

        #endregion

        #region Instance Data Members (Private)

        private readonly string _name;
        private Queue _messageQ = Queue.Synchronized(new Queue());
        private AutoResetEvent _newMsgArrived = new AutoResetEvent(false);
        private Thread _fileWriterThread;

        /// <summary>
        /// Cyclic flag
        /// </summary>
        bool _cyclic = true;	
        /// <summary>
        /// Max log file size.
        /// </summary>
        long _maxFileSize = ChoStorageScale.MB;	
        /// <summary>
        /// Max log files to be generated. if the files crossed over limit, will be autoBackup and restart from 0.
        /// </summary>
        int	_maxFileCount = 100;
        /// <summary>
        /// Directory location where log files will be created.
        /// </summary>
        string _directoryName = Path.Combine(ChoPath.EntryAssemblyBaseDirectory, "Log");
        /// <summary>
        /// Log file name pattern.
        /// </summary>
        string _baseFileName = "{0}.log".FormatString(ChoPath.EntryAssemblyName);
        string BaseFileName
        {
            get { return _baseFileName; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;
                value = value.Trim();
                if (!value.EndsWith(".log"))
                    _baseFileName = "{0}.log".FormatString(value);
            }
        }

        /// <summary>
        /// Log file extension.
        /// </summary>
        string _fileExt = "log";
        /// <summary>
        /// It contains list of fully qualified class names whose log entried to be logged to file.
        /// </summary>
        string[] _includeList = new string[0];
        /// <summary>
        /// It contains list of fully qualified class names whose log entried shouldn't be logged to file.
        /// </summary>
        string[] _excludeList = new string[0];
        /// <summary>
        /// If this flag is off, only one log file will be created and appended thereafter.
        /// </summary>
        bool _autoBackup = false;			

        bool _allowSplitMsg = false;
        bool _timeStamp = true;
        bool _processInfo = false;
        bool _callerInfo = false;
        string _Separator=",";

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ChoTextWriterTraceListener()
        {
            Init();
        }

        /// <summary>
        /// Constructor with Initialize data
        /// </summary>
        /// <param name="initializeData">Semi colon seperated Initialize information</param>
        public ChoTextWriterTraceListener(string name, string initializeData)
        {
            _name = !name.IsNullOrWhiteSpace() ? name : "{0}_{1}".FormatString(GetType().Name, new ChoCryptoRandom().Next());
            string [] initDatas=initializeData.Split(';');

            foreach (string initData in initDatas)
            {
                string [] nameValues=initData.Split('=');
                if (nameValues.Length==2)
                {
                    switch (nameValues.GetValue(0).ToString().ToUpper().Trim())
                    {
                        case "CYCLIC":
                            try
                            {
                                _cyclic = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "DIRECTORYNAME":
                            _directoryName = nameValues.GetValue(1).ToString().Trim();
                            break;
                        case "BASEFILENAME":
                            string baseFileName = nameValues.GetValue(1).ToString().Trim();
                            if (!baseFileName.IsNullOrWhiteSpace())
                                BaseFileName = baseFileName;
                            break;
                        case "MAXFILECOUNT":
                            try
                            {
                                _maxFileCount = Convert.ToInt32(nameValues.GetValue(1));
                            }
                            catch 
                            {
                            }
                            break;
                        case "FILEEXT":
                            FileExt = nameValues.GetValue(1).ToString().Trim();
                            break;
                        case "MAXFILESIZE":
                            try
                            {
                                _maxFileSize = ChoStorageScale.Parse(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch 
                            {
                            }
                            break;
                        case "INCLUDE":
                            _includeList = nameValues.GetValue(1).ToString().Trim().Split(',');
                            break;
                        case "EXCLUDE":
                            _excludeList = nameValues.GetValue(1).ToString().Trim().Split(',');
                            break;
                        case "AUTOBACKUP":
                            try
                            {
                                _autoBackup = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "ALLOWSPLITMSG":
                            try
                            {
                                _allowSplitMsg = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "TIMESTAMP":
                            try
                            {
                                _timeStamp = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "PROCESSINFO":
                            try
                            {
                                _processInfo = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "CALLERINFO":
                            try
                            {
                                _callerInfo = Convert.ToBoolean(nameValues.GetValue(1).ToString().Trim());
                            }
                            catch
                            {
                            }
                            break;
                        case "Separator":
                            _Separator = nameValues.GetValue(1).ToString().Trim();
                            break;
                    }
                }
            }

            Init();
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="directoryName">Log directory.</param>
        /// <param name="maxFileSize">Max size of each log file.</param>
        /// <param name="baseFileName">Log file name.</param>
        /// <param name="fileExt">Log file extension.</param>
        /// <param name="maxFileCount">Max log file count. Once it reach the top count, it will automatically backup all the files and restart from 0</param>
        /// <param name="includeList">It contains list of fully qualified class names whose log entried to be logged to file.</param>
        /// <param name="excludeList">It contains list of fully qualified class names whose log entried shouldn't logged to file.</param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup">If this flag is off, only one log file will be created and appended thereafter.</param>
        /// <param name="allowSplitMsg"></param>
        /// <param name="Separator"></param>
        public ChoTextWriterTraceListener(string directoryName,
            long maxFileSize,
            string baseFileName,
            string fileExt,
            int maxFileCount,
            string[] includeList,
            string[] excludeList,
            bool cyclic,
            bool autoBackup,
            bool allowSplitMsg, 
            string Separator
            )
        {
            _directoryName = directoryName;
            _maxFileSize = maxFileSize;
            BaseFileName = baseFileName;
            FileExt = fileExt;
            _maxFileCount = maxFileCount;
            _includeList = includeList;
            _excludeList = excludeList;
            _cyclic = cyclic;
            _autoBackup = autoBackup;
            _Separator = Separator;

            Init();
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        /// <param name="directoryName">Log directory.</param>
        /// <param name="maxFileSize">Max size of each log file.</param>
        /// <param name="baseFileName">Log file name.</param>
        /// <param name="fileExt">Log file extension.</param>
        /// <param name="maxFileCount">Max log file count. Once it reach the top count, it will automatically backup all the files and restart from 0</param>
        /// <param name="includeList">It contains list of fully qualified class names whose log entried to be logged to file.</param>
        /// <param name="excludeList">It contains list of fully qualified class names whose log entried shouldn't logged to file.</param>
        /// <param name="cyclic"></param>
        /// <param name="autoBackup">If this flag is off, only one log file will be created and appended thereafter.</param>
        /// <param name="allowSplitMsg"></param>
        /// <param name="Separator"></param>
        /// <param name="timeStamp"></param>
        /// <param name="processInfo"></param>
        /// <param name="callerInfo"></param>
        public ChoTextWriterTraceListener(string directoryName,
            long maxFileSize,
            string baseFileName,
            string fileExt,
            int maxFileCount,
            string[] includeList,
            string[] excludeList,
            bool cyclic,
            bool autoBackup,
            bool allowSplitMsg,
            string Separator,
            bool timeStamp,
            bool processInfo,
            bool callerInfo
            )
        {
            _directoryName = directoryName;
            _maxFileSize = maxFileSize;
            BaseFileName = baseFileName;
            FileExt = fileExt;
            _maxFileCount = maxFileCount;
            _includeList = includeList;
            _excludeList = excludeList;
            _cyclic = cyclic;
            _autoBackup = autoBackup;
            _timeStamp = timeStamp;
            _processInfo = processInfo;
            _callerInfo = callerInfo;
            _Separator = Separator;

            Init();
        }

        private void Init()
        {
            Directory.CreateDirectory(_directoryName);
            Start();
        }

        #endregion

        #region TextWriterTraceListener Overrides

        StringBuilder _buffer = new StringBuilder();

        /// <summary>
        /// Writes a message to this instance's Writer
        /// </summary>
        /// <param name="message"></param>
        public override void Write(string message) 
        {	
            Trace.AutoFlush = false;
            if (Traceable)
                _buffer.Append(message);
                //Push2Queue(FormatMessage(message), FileOperation.Write);
        }

        /// <summary>
        /// Writes a message to this instance's Writer followed by a line terminator.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message) 
        {	
            Trace.AutoFlush = false;
            if (Traceable)
            {
                if (_buffer.Length > 0)
                {
                    _buffer.Append(message);
                    Push2Queue(FormatMessage(_buffer.ToString()), FileOperation.WriteLine);
                    _buffer.Clear();
                }
                else
                    Push2Queue(FormatMessage(message), FileOperation.WriteLine);
            }
        } 

        #endregion

        #region Instance Memeber function (Private)

        private string FormatMessage(string message)
        {
            StringBuilder formattedMessage = new StringBuilder();

            if (_timeStamp)
                formattedMessage.AppendFormat("{0:o}{1} ", DateTime.Now, _Separator);
            if (_processInfo)
                formattedMessage.AppendFormat("{0}/{1}{2} ", Process.GetCurrentProcess().Id, 
                    Thread.CurrentThread.GetHashCode(), _Separator);

            if (_callerInfo)
                formattedMessage.AppendFormat("{0}{1} ", CallerMethod(), _Separator);

            formattedMessage.Append(message);

            return formattedMessage.ToString();
        }

        private string CallerMethod()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame;
            MethodBase stackFrameMethod;

            string typeName;
            for (int frameCount=0; frameCount < stackTrace.FrameCount; frameCount++)
            {
                stackFrame = stackTrace.GetFrame(frameCount);
                stackFrameMethod = stackFrame.GetMethod();
                typeName = stackFrameMethod.ReflectedType.FullName;

                if (!typeName.StartsWith(typeof(ChoTextWriterTraceListener).Namespace) 
                    && !typeName.StartsWith("System"))
                    return stackFrameMethod.ReflectedType.FullName + "." + stackFrameMethod.Name;
            } 

            return String.Empty;
        }

        #region Push2Queue Overloads

        private void Push2Queue(TextTraceMsg traceMsg)
        {
            Start();
            _messageQ.Enqueue(traceMsg);
            _newMsgArrived.Set();
        }

        private void Push2Queue(string msg, FileOperation operation)
        {
            Start();
            _messageQ.Enqueue(new TextTraceMsg(FilePath, msg, operation));
            _newMsgArrived.Set();
        }

        private void Push2Queue(string msg, string prevLogFileName)
        {
            Start();
            _messageQ.Enqueue(new TextTraceMsg(FilePath, msg, prevLogFileName));
            _newMsgArrived.Set();
        }

        #endregion Push2Queue Overloads

        private void Start()
        {
            lock (typeof(ChoTextWriterTraceListener))
            {
                if (_fileWriterThread != null && _fileWriterThread.IsAlive) return;

                try
                {
                    _fileWriterThread = new Thread(new ThreadStart(SyncFileWriter));
                    _fileWriterThread.IsBackground = true;
                    _fileWriterThread.Start();
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, ex.ToString());
                }
            }
        }

        private void SyncFileWriter()
        {
            while (true)
            {
                TextTraceMsg item = null;

                if (_isDisposed)
                    break;

                try
                {
                    object queueObject = null;
                    if (_messageQ.Count > 0)
                        queueObject = _messageQ.Dequeue();
                    else
                    {
                        _newMsgArrived.WaitOne();
                        continue;
                    }

                    item = (TextTraceMsg)queueObject;
                    if (item.IsFlushMsg)
                    {
                        if (base.Writer != null)
                        {
                            base.Writer.Close();
                            base.Writer = null;
                        }
                        break;
                    }

                    switch (item.Operation)
                    {
                        case FileOperation.WriteLine:
                            if (base.Writer == null)
                            {
                                base.Writer = new StreamWriter(new ChoFileStreamWithBackup(FilePath,
                                    _maxFileSize, _maxFileCount, FileMode.Append, _cyclic, _autoBackup, _allowSplitMsg));
                            }
                            base.Writer.WriteLine(item.Msg);
                            base.Writer.Flush();
                            break;
                        case FileOperation.Write:
                            if (base.Writer == null)
                            {
                                base.Writer = new StreamWriter(new ChoFileStreamWithBackup(FilePath,
                                    _maxFileSize, _maxFileCount, FileMode.Append, _cyclic, _autoBackup, _allowSplitMsg));
                            }
                            base.Writer.Write(item.Msg);
                            base.Writer.Flush();
                            break;
                        case FileOperation.CreateNew:
                            base.Writer = new StreamWriter(new ChoFileStreamWithBackup(item.LogFilePath,
                                _maxFileSize, _maxFileCount, FileMode.Append, _cyclic, _autoBackup, _allowSplitMsg));
                            break;
                        case FileOperation.Rename:
                            base.Writer = new StreamWriter(new ChoFileStreamWithBackup(item.LogFilePath, _maxFileSize, _maxFileCount, FileMode.Append, _cyclic, _autoBackup, _allowSplitMsg));
                            if (File.Exists(item.PrevLogFilePath))
                            {
                                using (StreamReader reader = File.OpenText(item.PrevLogFilePath))
                                    base.Writer.WriteLine(reader.ReadToEnd());
                            }
                            break;
                    }
                }
                catch (IOException ioEx)
                {
                    string errMsg;
                    if (item != null)
                        errMsg = String.Format("Error while writing the below message.{0}{1}{0}Exception: {2}", Environment.NewLine,
                            item.ToString(), ioEx.ToString());
                    else
                        errMsg = ioEx.ToString();

#if !NETSTANDARD2_0
                    ChoETLFramework.WriteToEventLog(errMsg, EventLogEntryType.Error);
#endif
                    //Environment.Exit(-100);
                }
                catch (Exception ex)
                {
                    if (ex is ThreadAbortException)
                    {
                        Thread.ResetAbort();
                    }
                    else
                    {
                        string errMsg;
                        if (item != null)
                            errMsg = String.Format("Error while writing the below message.{0}{1}{0}Exception: {2}", Environment.NewLine,
                                item.ToString(), ex.ToString());
                        else
                            errMsg = ex.ToString();

#if !NETSTANDARD2_0
                    ChoETLFramework.WriteToEventLog(errMsg, EventLogEntryType.Error);
#endif
                    }
                }
            }
        }

#endregion

#region Instance Properties (Public)

        public string FilePath
        {
            get { return Path.Combine(_directoryName, BaseFileName); }
        }

#endregion

#region Instance Properties (Private)

        /// <summary>
        /// Property tells you the whether the message is eligible to write to log file or not.
        /// </summary>
        private bool Traceable
        {
            get 
            {
                if (_includeList.Length==0 && _excludeList.Length==0 ) return true;

                StackTrace stackTrace = new StackTrace();
                StackFrame stackFrame;
                MethodBase stackFrameMethod;

                string typeName;
                for (int frameCount=0; frameCount < stackTrace.FrameCount; frameCount++)
                {
                    stackFrame = stackTrace.GetFrame(frameCount);
                    stackFrameMethod = stackFrame.GetMethod();
                    typeName = stackFrameMethod.ReflectedType.FullName;

                    //First check to see the calling method class exists in the ExcludeList
                    foreach (string excludeItem in _excludeList)
                    {
                        if (typeName.StartsWith(excludeItem))
                            return false;
                    }

                    //Next check to see the calling method class exists in the IncludeList
                    foreach (string includeItem in _includeList)
                    {
                        if (typeName.StartsWith(includeItem))
                            return true;
                    }
                } 

                return false;
            }
        }

        /// <summary>
        /// Property to validate and set the log file extension 
        /// </summary>
        private string FileExt
        {
            get { return _fileExt; }
            set 
            {
                if (String.IsNullOrEmpty(value)) return;
                _fileExt = value.Replace(".", String.Empty);
            }
        }

#endregion

#region Events

        public event Func<string> OnGetCustomLogFileName
        {
            add
            {
                string prevBaseFileName = BaseFileName;

                string customBaseFileName = null;
                if (value != null) customBaseFileName = value();
                if (String.IsNullOrEmpty(customBaseFileName)) return;
                
                BaseFileName = customBaseFileName;
                Push2Queue(String.Empty, prevBaseFileName);
            }
            remove
            {
            }
        }

#endregion

#region Instance Members (Public)

        public override void Flush()
        {
            while (true)
            {
                ChoETLLog.Info("Trace.Flush...");
                
                Push2Queue(TextTraceMsg.FlushTraceMsg);
                if (_fileWriterThread.Join(1000)) break;
            }
            base.Flush();
        }

#endregion

#region Shared Members (Public)

        public static ChoTextWriterTraceListener GetFirst()
        {
            foreach (TraceListener traceListener in Trace.Listeners)
            {
                if (traceListener == null) continue;
                if (traceListener is ChoTextWriterTraceListener) return traceListener as ChoTextWriterTraceListener;
            }
            return null;
        }

        public static ChoTextWriterTraceListener Get(string name)
        {
            foreach (TraceListener traceListener in Trace.Listeners)
            {
                if (traceListener == null) continue;
                if (traceListener is ChoTextWriterTraceListener && traceListener.Name == name ) return traceListener as ChoTextWriterTraceListener;
            }
            return null;
        }

        public static ChoTextWriterTraceListener[] GetAll()
        {
            ArrayList traceListeners = new ArrayList();
            foreach (TraceListener traceListener in Trace.Listeners)
            {
                if (traceListener == null) continue;
                if (traceListener is ChoTextWriterTraceListener)
                    traceListeners.Add(traceListener);
            }
            return traceListeners.ToArray(typeof(ChoTextWriterTraceListener)) as ChoTextWriterTraceListener[];
        }

#endregion

        private bool _isDisposed = false;
        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;
            base.Dispose(disposing);
        }
    }

    public static class ChoStorageScale
    {
#region Constants

        public const long KB = 1024;
        public const long MB = KB * KB;
        public const long GB = MB * KB;
        public const long TB = GB * KB;

#endregion

#region Shared Data Memebers (Private)

        private static Regex _sizeRegEx = new Regex("(?<Size>\\d+)(?<Scale>[KMGT]B)*", RegexOptions.Compiled);

#endregion

#region Shated Member Functions (Public)


        /// <summary>
        /// Converts the string representation of a storage scale to its 64-bit signed long
        /// equivalent. A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="value">A string containing a storage scale to convert.</param>
        /// <returns>
        /// Contains the 64-bit signed long value equivalent
        /// to the storage value, if the conversion succeeded, or throws exception.
        /// </returns>
        public static long Parse(string value)
        {
            //Initialize it as MB
            long size = MB;

            if (value == null)
                throw new ArgumentNullException("value");

            Match match = _sizeRegEx.Match(value);
            if (match.Success)
            {
                size = Int32.Parse(match.Groups["Size"].ToString());
                switch (match.Groups["Scale"].ToString())
                {
                    case "KB":
                        size *= KB;
                        break;
                    case "MB":
                        size *= MB;
                        break;
                    case "GB":
                        size *= GB;
                        break;
                    case "TB":
                        size *= TB;
                        break;
                    default:
                        throw new FormatException(String.Format("Invalid `{0}` scale is passed.", match.Groups["Scale"].ToString()));
                }
            }
            else
                throw new FormatException("Invalid storage scale is passed.");

            if (size < 0)
                throw new ArgumentException(String.Format("Storage scale value should be non-negative. Passed: {0}.", size));

            return size;
        }

        /// <summary>
        /// Converts the string representation of a storage scale to its 64-bit signed long
        /// equivalent. A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="value">A string containing a storage scale to convert.</param>
        /// <param name="result">
        /// When this method returns, contains the 64-bit signed long value equivalent
        /// to the storage value, if the conversion succeeded, or zero if the
        /// conversion failed. The conversion fails if the value parameter is null, is not
        /// of the correct format, or represents a number less than 0
        /// or greater than System.Int64.MaxValue. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if value was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string value, out long result)
        {
            result = 0;

            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

#endregion
    }
}
