using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoWriter
    {
        event EventHandler<ChoBeginWriteEventArgs> BeginWrite;
        event EventHandler<ChoEndWriteEventArgs> EndWrite;

        event EventHandler<ChoBeforeRecordWriteEventArgs> BeforeRecordWrite;
        event EventHandler<ChoAfterRecordWriteEventArgs> AfterRecordWrite;
        event EventHandler<ChoRecordWriteErrorEventArgs> RecordWriteError;

        event EventHandler<ChoBeforeRecordFieldWriteEventArgs> BeforeRecordFieldWrite;
        event EventHandler<ChoAfterRecordFieldWriteEventArgs> AfterRecordFieldWrite;
        event EventHandler<ChoRecordFieldWriteErrorEventArgs> RecordFieldWriteError;
    }

    public class ChoBeginWriteEventArgs : EventArgs
    {
        public object Source
        {
            get;
            internal set;
        }

        public bool Stop
        {
            get;
            set;
        }
    }

    public class ChoEndWriteEventArgs : EventArgs
    {
        public object Source
        {
            get;
            internal set;
        }
    }

    public class ChoBeforeRecordWriteEventArgs : EventArgs
    {
        public object Record
        {
            get;
            internal set;
        }
        public long Index
        {
            get;
            internal set;
        }
        public object Source
        {
            get;
            set;
        }
        public bool Skip
        {
            get;
            set;
        }
    }

    public class ChoBeforeRecordFieldWriteEventArgs : ChoBeforeRecordWriteEventArgs
    {
        public string PropertyName
        {
            get;
            internal set;
        }
    }

    public class ChoAfterRecordWriteEventArgs : EventArgs
    {
        public object Record
        {
            get;
            internal set;
        }
        public long Index
        {
            get;
            internal set;
        }
        public object Source
        {
            get;
            internal set;
        }
        public bool Stop
        {
            get;
            set;
        }
    }

    public class ChoAfterRecordFieldWriteEventArgs : ChoAfterRecordWriteEventArgs
    {
        public string PropertyName
        {
            get;
            internal set;
        }
    }

    public class ChoRecordWriteErrorEventArgs : EventArgs
    {
        public object Record
        {
            get;
            internal set;
        }
        public long Index
        {
            get;
            internal set;
        }
        public object Source
        {
            get;
            internal set;
        }
        public Exception Exception
        {
            get;
            internal set;
        }
        public bool Handled
        {
            get;
            set;
        }
    }

    public class ChoRecordFieldWriteErrorEventArgs : ChoRecordWriteErrorEventArgs
    {
        public string PropertyName
        {
            get;
            internal set;
        }
    }

    public class ChoFileHeaderEventArgs : EventArgs
    {
        public string HeaderText
        {
            get;
            set;
        }
        public bool Skip
        {
            get;
            set;
        }
    }

    public class ChoFileHeaderArrangeEventArgs : EventArgs
    {
        public List<string> Fields
        {
            get;
            set;
        }
    }

    public class ChoCustomNodeNameOverrideEventArgs : EventArgs
    {
        public long Index
        {
            get;
            set;
        }
        public object Record
        {
            get;
            set;
        }
        public string NodeName
        {
            get;
            set;
        }
    }
}
