namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    #endregion NameSpaces

    [Serializable]
    public abstract class ChoBaseProfile : IDisposable
    {
        #region Instance Data Members (Private)

        private readonly int _indent = 0;
        private readonly string _msg = "Elapsed time taken by the profile:";
        private readonly ChoBaseProfile _outerProfile = null;
        private readonly bool _condition = ChoETLFramework.TraceSwitch.TraceVerbose;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _padLock = new object();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object _disposableLock = new object();

        private bool _started = false;
        private DateTime _startTime = DateTime.Now;
        private DateTime _endTime = DateTime.Now;
        private bool _isDisposed = false;

        #endregion Instance Data Members (Private)

        #region Constrctors

        public ChoBaseProfile(string msg, ChoBaseProfile outerProfile = null)
            : this(ChoETLFramework.TraceSwitch.TraceVerbose, msg, outerProfile)
        {
        }

        public ChoBaseProfile(bool condition, string msg, ChoBaseProfile outerProfile = null)
        {
            _condition = condition;
            _outerProfile = outerProfile;

            if (_condition)
            {
                if (!msg.IsNullOrEmpty())
                    _msg = msg;
            }

            if (outerProfile != null)
                _indent = outerProfile.Indent + 1;

            StartIfNotStarted();
        }

        #endregion Constrctors

        #region ChoBaseProfile Members (Public)

        public string AppendLine(string msg)
        {
            return AppendLineIf(_condition, msg);
        }

        public string AppendLineIf(bool condition, string msg)
        {
            if (!condition)
                return msg;
            if (msg == null)
                return AppendIf(condition, Environment.NewLine);
            else
                return AppendIf(condition, msg + Environment.NewLine);
        }

        public string Append(string msg)
        {
            return AppendIf(_condition, msg);
        }

        public string AppendIf(bool condition, string msg)
        {
            if (!condition)
                return msg;

            StartIfNotStarted();

            Write(msg, _indent + 1);

            return msg;
        }

        public void AppendIf(bool condition, Exception ex)
        {
            if (condition)
                AppendLineIf(condition, ex.ToString());
        }

        public void Append(Exception ex)
        {
            AppendIf(_condition, ex);
        }

        public string AppendIf(bool condition, string format, params object[] args)
        {
            return AppendIf(condition, String.Format(format, args));
        }

        public string Append(string format, params object[] args)
        {
            return Append(String.Format(format, args));
        }

        public string AppendLineIf(bool condition, string format, params object[] args)
        {
            return AppendLineIf(condition, String.Format(format, args));
        }

        public string AppendLine(string format, params object[] args)
        {
            return AppendLine(String.Format(format, args));
        }

        public TimeSpan ElapsedTimeTaken
        {
            get { return (_endTime - _startTime); }
        }

        public int Indent
        {
            get { return _indent; }
        }

        #endregion ChoBaseProfile Members (Public)

        #region Instance Members (Protected)

        protected abstract void Flush();
        protected abstract void Write(string msg);

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            try
            {
                StartIfNotStarted();

                if (_condition)
                {
                    Write(String.Format("}} [{0}] <---{1}", Convert.ToString(DateTime.Now - _startTime), Environment.NewLine), _indent);
                }

                Flush();
            }
            catch (Exception ex)
            {
                ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, ex.ToString());
            }
            finally
            {
                _isDisposed = true;
            }
        }

        protected void WriteToBackingStore(string msg)
        {
            if (_outerProfile != null)
                ((ChoBaseProfile)_outerProfile).Write(msg);
            else if (ChoETLFramework.GlobalProfile != null && ChoETLFramework.GlobalProfile != this)
                ChoETLFramework.GlobalProfile.Write(msg.Indent(1));
            else
            {
                ChoETLFrxBootstrap.Log(msg);
            }
        }

        #endregion Instance Members (Protected)

        #region Instance Members (Private)

        private void Write(string msg, int indent)
        {
            msg = msg.Indent(indent);
            Write(msg);
        }

        private void StartIfNotStarted()
        {
            if (_started)
                return;

            lock (_padLock)
            {
                if (_started)
                    return;

                _startTime = DateTime.Now;
                _started = true;

                if (_condition)
                {
                    Write(String.Format("{0} {{ [{1}]{2}", _msg, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss:ffff"), Environment.NewLine), _indent);
                }
            }
        }

        #endregion Instance Members (Private)

        #region Other Members

        public void Debug(object message)
        {
            if (message != null)
                AppendLineIf(ChoETLFramework.TraceSwitch.TraceVerbose, message.ToString());
        }

        public void Debug(Exception exception)
        {
            AppendIf(ChoETLFramework.TraceSwitch.TraceVerbose, exception);
        }

        public void Debug(object message, Exception exception)
        {
            Debug(message);
            Debug(exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceVerbose, String.Format(format, args));
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceVerbose, String.Format(provider, format, args));
        }

        public void Error(object message)
        {
            if (message != null)
                AppendLineIf(ChoETLFramework.TraceSwitch.TraceError, message.ToString());
        }

        public void Error(Exception exception)
        {
            AppendIf(ChoETLFramework.TraceSwitch.TraceError, exception);
        }

        public void Error(object message, Exception exception)
        {
            Error(message);
            Error(exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceError, String.Format(format, args));
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceError, String.Format(provider, format, args));
        }

        public void Info(object message)
        {
            if (message != null)
                AppendLineIf(ChoETLFramework.TraceSwitch.TraceInfo, message.ToString());
        }

        public void Info(Exception exception)
        {
            AppendIf(ChoETLFramework.TraceSwitch.TraceInfo, exception);
        }

        public void Info(object message, Exception exception)
        {
            Info(message);
            Info(exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceInfo, String.Format(format, args));
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceInfo, String.Format(provider, format, args));
        }

        public void Warn(object message)
        {
            if (message != null)
                AppendLineIf(ChoETLFramework.TraceSwitch.TraceWarning, message.ToString());
        }

        public void Warn(Exception exception)
        {
            AppendIf(ChoETLFramework.TraceSwitch.TraceWarning, exception);
        }

        public void Warn(object message, Exception exception)
        {
            Warn(message);
            Warn(exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceWarning, String.Format(format, args));
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            AppendLineIf(ChoETLFramework.TraceSwitch.TraceWarning, String.Format(provider, format, args));
        }

        #endregion

        public object Tag
        {
            get;
            set;
        }

        public void Dispose()
        {
            Dispose(false);
        }

        ~ChoBaseProfile()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }
}
