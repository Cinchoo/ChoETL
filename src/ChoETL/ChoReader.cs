using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoReader : IChoReader, IChoValidatable
    {
        public virtual dynamic Context { get; }

        public bool IsValid { get; set; } = true;
        public ChoContractResolverState ContractResolverState { get; set; }

        public event EventHandler<ChoBeginLoadEventArgs> BeginLoad;
        public event EventHandler<ChoEndLoadEventArgs> EndLoad;

        public event EventHandler<ChoAfterRecordLoadEventArgs> AfterRecordLoad;
        public event EventHandler<ChoBeforeRecordLoadEventArgs> BeforeRecordLoad;
        public event EventHandler<ChoRecordLoadErrorEventArgs> RecordLoadError;

        public event EventHandler<ChoBeforeRecordFieldLoadEventArgs> BeforeRecordFieldLoad;
        public event EventHandler<ChoAfterRecordFieldLoadEventArgs> AfterRecordFieldLoad;
        public event EventHandler<ChoRecordFieldLoadErrorEventArgs> RecordFieldLoadError;

        public event EventHandler<ChoSkipUntilEventArgs> SkipUntil;
        public event EventHandler<ChoDoWhileEventArgs> DoWhile;

        public event EventHandler<ChoRecordFieldSerializeEventArgs> RecordFieldDeserialize;

        public bool HasBeginLoadSubscribed
        {
            get
            {
                EventHandler<ChoBeginLoadEventArgs> eh = BeginLoad;
                return (eh != null);
            }
        }

        public bool RaiseBeginLoad(object source)
        {
            EventHandler<ChoBeginLoadEventArgs> eh = BeginLoad;
            if (eh == null)
                return true;

            ChoBeginLoadEventArgs e = new ChoBeginLoadEventArgs() { Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool HasEndLoadSubscribed
        {
            get
            {
                EventHandler<ChoEndLoadEventArgs> eh = EndLoad;
                return (eh != null);
            }
        }

        public void RaiseEndLoad(object source)
        {
            EventHandler<ChoEndLoadEventArgs> eh = EndLoad;
            if (eh == null)
                return;

            ChoEndLoadEventArgs e = new ChoEndLoadEventArgs() { Source = source };
            eh(this, e);
        }

        public bool HasSkipUntilSubscribed
        {
            get
            {
                EventHandler<ChoSkipUntilEventArgs> eh = SkipUntil;
                return (eh != null);
            }
        }

        public bool? RaiseSkipUntil(long index, object source)
        {
            EventHandler<ChoSkipUntilEventArgs> eh = SkipUntil;
            if (eh == null)
                return null;

            ChoSkipUntilEventArgs e = new ChoSkipUntilEventArgs() { Index = index, Source = source };
            eh(this, e);
            return e.Skip;
        }

        public bool HasDoWhileSubscribed
        {
            get
            {
                EventHandler<ChoDoWhileEventArgs> eh = DoWhile;
                return (eh != null);
            }
        }

        public bool? RaiseDoWhile(long index, object source)
        {
            EventHandler<ChoDoWhileEventArgs> eh = DoWhile;
            if (eh == null)
                return null;

            ChoDoWhileEventArgs e = new ChoDoWhileEventArgs() { Index = index, Source = source };
            eh(this, e);
            return e.Stop;
        }

        public bool HasBeforeRecordLoadSubscribed
        {
            get
            {
                EventHandler<ChoBeforeRecordLoadEventArgs> eh = BeforeRecordLoad;
                return (eh != null);
            }
        }

        public bool RaiseBeforeRecordLoad(object record, long index, ref object source)
        {
            EventHandler<ChoBeforeRecordLoadEventArgs> eh = BeforeRecordLoad;
            if (eh == null)
                return true;

            ChoBeforeRecordLoadEventArgs e = new ChoBeforeRecordLoadEventArgs() { Record = record, Index = index, Source = source };
            eh(this, e);
            source = e.Source;
            return !e.Skip;
        }

        public bool HasAfterRecordLoadSubscribed
        {
            get
            {
                EventHandler<ChoBeforeRecordLoadEventArgs> eh = BeforeRecordLoad;
                return (eh != null);
            }
        }

        public bool RaiseAfterRecordLoad(object record, long index, object source, ref bool skip)
        {
            EventHandler<ChoAfterRecordLoadEventArgs> eh = AfterRecordLoad;
            if (eh == null)
                return true;

            ChoAfterRecordLoadEventArgs e = new ChoAfterRecordLoadEventArgs() { Record = record, Index = index, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool HasRecordLoadErrorSubscribed
        {
            get
            {
                EventHandler<ChoRecordLoadErrorEventArgs> eh = RecordLoadError;
                return (eh != null);
            }
        }

        public bool RaiseRecordLoadError(object record, long index, object source, Exception ex)
        {
            EventHandler<ChoRecordLoadErrorEventArgs> eh = RecordLoadError;
            if (eh == null)
                return false;

            ChoRecordLoadErrorEventArgs e = new ChoRecordLoadErrorEventArgs() { Record = record, Index = index, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }

        public bool HasBeforeRecordFieldLoadSubscribed
        {
            get
            {
                EventHandler<ChoBeforeRecordFieldLoadEventArgs> eh = BeforeRecordFieldLoad;
                return (eh != null);
            }
        }

        public bool RaiseBeforeRecordFieldLoad(object record, long index, string propName, ref object source)
        {
            EventHandler<ChoBeforeRecordFieldLoadEventArgs> eh = BeforeRecordFieldLoad;
            if (eh == null)
                return true;

            ChoBeforeRecordFieldLoadEventArgs e = new ChoBeforeRecordFieldLoadEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            source = e.Source;
            return !e.Skip;
        }

        public bool HasAfterRecordFieldLoadSubscribed
        {
            get
            {
                EventHandler<ChoAfterRecordFieldLoadEventArgs> eh = AfterRecordFieldLoad;
                return (eh != null);
            }
        }

        public bool RaiseAfterRecordFieldLoad(object record, long index, string propName, object source)
        {
            EventHandler<ChoAfterRecordFieldLoadEventArgs> eh = AfterRecordFieldLoad;
            if (eh == null)
                return true;

            ChoAfterRecordFieldLoadEventArgs e = new ChoAfterRecordFieldLoadEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool HasRecordFieldLoadErrorSubscribed
        {
            get
            {
                EventHandler<ChoRecordFieldLoadErrorEventArgs> eh = RecordFieldLoadError;
                return (eh != null);
            }
        }

        public bool RaiseRecordFieldLoadError(object record, long index, string propName, ref object source, Exception ex)
        {
            EventHandler<ChoRecordFieldLoadErrorEventArgs> eh = RecordFieldLoadError;
            if (eh == null)
                return true;

            ChoRecordFieldLoadErrorEventArgs e = new ChoRecordFieldLoadErrorEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }

        public virtual bool HasMapColumnSubscribed
        {
            get { return false; }
        }

        public virtual bool RaiseMapColumn(int colPos, string colName, out string newColName)
        {
            newColName = null;
            return false;
        }

        public virtual bool HasReportEmptyLineSubscribed
        {
            get { return false; }
        }

        public virtual bool RaiseReportEmptyLine(long lineNo)
        {
            return true;
        }

        public virtual bool TryValidate(object target, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public virtual bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults)
        {
            throw new NotSupportedException();
        }

        public bool HasRecordFieldDeserializeSubcribed => RecordFieldDeserialize != null;

        public bool RaiseRecordFieldDeserialize(object record, long index, string propName, ref object source)
        {
            EventHandler<ChoRecordFieldSerializeEventArgs> eh = RecordFieldDeserialize;
            if (eh == null)
                return false;

            ChoRecordFieldSerializeEventArgs e = new ChoRecordFieldSerializeEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }
    }
}
