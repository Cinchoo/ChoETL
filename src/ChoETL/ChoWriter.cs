using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoWriter : IChoWriter
    {
        public event EventHandler<ChoBeginWriteEventArgs> BeginWrite;
        public event EventHandler<ChoEndWriteEventArgs> EndWrite;

        public event EventHandler<ChoAfterRecordWriteEventArgs> AfterRecordWrite;
        public event EventHandler<ChoBeforeRecordWriteEventArgs> BeforeRecordWrite;
        public event EventHandler<ChoRecordWriteErrorEventArgs> RecordWriteError;

        public event EventHandler<ChoBeforeRecordFieldWriteEventArgs> BeforeRecordFieldWrite;
        public event EventHandler<ChoAfterRecordFieldWriteEventArgs> AfterRecordFieldWrite;
        public event EventHandler<ChoRecordFieldWriteErrorEventArgs> RecordFieldWriteError;

        public event EventHandler<ChoFileHeaderEventArgs> FileHeaderWrite;
        public event EventHandler<ChoRecordFieldSerializeEventArgs> RecordFieldSerialize;

        public bool RaiseBeginWrite(object source)
        {
            EventHandler<ChoBeginWriteEventArgs> eh = BeginWrite;
            if (eh == null)
                return true;

            ChoBeginWriteEventArgs e = new ChoBeginWriteEventArgs() { Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public void RaiseEndWrite(object source)
        {
            EventHandler<ChoEndWriteEventArgs> eh = EndWrite;
            if (eh == null)
                return;

            ChoEndWriteEventArgs e = new ChoEndWriteEventArgs() { Source = source };
            eh(this, e);
        }

        public bool RaiseBeforeRecordWrite(object record, long index, ref object source)
        {
            EventHandler<ChoBeforeRecordWriteEventArgs> eh = BeforeRecordWrite;
            if (eh == null)
                return true;

            ChoBeforeRecordWriteEventArgs e = new ChoBeforeRecordWriteEventArgs() { Record = record, Index = index, Source = source };
            eh(this, e);
            source = e.Source;
            return !e.Skip;
        }

        public bool RaiseAfterRecordWrite(object record, long index, object source)
        {
            EventHandler<ChoAfterRecordWriteEventArgs> eh = AfterRecordWrite;
            if (eh == null)
                return true;

            ChoAfterRecordWriteEventArgs e = new ChoAfterRecordWriteEventArgs() { Record = record, Index = index, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool RaiseRecordWriteError(object record, long index, object source, Exception ex)
        {
            EventHandler<ChoRecordWriteErrorEventArgs> eh = RecordWriteError;
            if (eh == null)
                return true;

            ChoRecordWriteErrorEventArgs e = new ChoRecordWriteErrorEventArgs() { Record = record, Index = index, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }

        public bool RaiseBeforeRecordFieldWrite(object record, long index, string propName, ref object source)
        {
            EventHandler<ChoBeforeRecordFieldWriteEventArgs> eh = BeforeRecordFieldWrite;
            if (eh == null)
                return true;

            ChoBeforeRecordFieldWriteEventArgs e = new ChoBeforeRecordFieldWriteEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            source = e.Source;
            return !e.Skip;
        }

        public bool RaiseAfterRecordFieldWrite(object record, long index, string propName, object source)
        {
            EventHandler<ChoAfterRecordFieldWriteEventArgs> eh = AfterRecordFieldWrite;
            if (eh == null)
                return true;

            ChoAfterRecordFieldWriteEventArgs e = new ChoAfterRecordFieldWriteEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool RaiseRecordFieldWriteError(object record, long index, string propName, object source, Exception ex)
        {
            EventHandler<ChoRecordFieldWriteErrorEventArgs> eh = RecordFieldWriteError;
            if (eh == null)
                return true;

            ChoRecordFieldWriteErrorEventArgs e = new ChoRecordFieldWriteErrorEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }
        public bool RaiseFileHeaderWrite(ref string headerText)
        {
            EventHandler<ChoFileHeaderEventArgs> eh = FileHeaderWrite;
            if (eh == null)
                return false;

            ChoFileHeaderEventArgs e = new ChoFileHeaderEventArgs() { HeaderText = headerText };
            eh(this, e);
            headerText = e.HeaderText;
            return e.Skip;
        }

        public bool RaiseRecordFieldSerialize(object record, long index, string propName, ref object source)
        {
            EventHandler<ChoRecordFieldSerializeEventArgs> eh = RecordFieldSerialize;
            if (eh == null)
                return false;

            ChoRecordFieldSerializeEventArgs e = new ChoRecordFieldSerializeEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }
    }
}
