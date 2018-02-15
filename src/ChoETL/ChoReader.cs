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
        public bool IsValid { get; set; } = true;
        public event EventHandler<ChoBeginLoadEventArgs> BeginLoad;
        public event EventHandler<ChoEndLoadEventArgs> EndLoad;

        public event EventHandler<ChoSkipUntilEventArgs> SkipUntil;
        public event EventHandler<ChoDoWhileEventArgs> DoWhile;
        public event EventHandler<ChoAfterRecordLoadEventArgs> AfterRecordLoad;
        public event EventHandler<ChoBeforeRecordLoadEventArgs> BeforeRecordLoad;
        public event EventHandler<ChoRecordLoadErrorEventArgs> RecordLoadError;

        public event EventHandler<ChoBeforeRecordFieldLoadEventArgs> BeforeRecordFieldLoad;
        public event EventHandler<ChoAfterRecordFieldLoadEventArgs> AfterRecordFieldLoad;
        public event EventHandler<ChoRecordFieldLoadErrorEventArgs> RecordFieldLoadError;

        protected Type ResolveRecordType(Type recordType)
        {
            if (typeof(ICollection).IsAssignableFrom(recordType)
                || recordType.IsSimple())
                throw new ChoParserException("Invalid record type passed.");
            else
                return recordType.GetUnderlyingType();
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

        public void RaiseEndLoad(object source)
        {
            EventHandler<ChoEndLoadEventArgs> eh = EndLoad;
            if (eh == null)
                return;

            ChoEndLoadEventArgs e = new ChoEndLoadEventArgs() { Source = source };
            eh(this, e);
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

        public bool? RaiseDoWhile(long index, object source)
        {
            EventHandler<ChoDoWhileEventArgs> eh = DoWhile;
            if (eh == null)
                return null;

            ChoDoWhileEventArgs e = new ChoDoWhileEventArgs() { Index = index, Source = source };
            eh(this, e);
            return e.Stop;
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

        public bool RaiseAfterRecordLoad(object record, long index, object source, ref bool skip)
        {
            EventHandler<ChoAfterRecordLoadEventArgs> eh = AfterRecordLoad;
            if (eh == null)
                return true;

            ChoAfterRecordLoadEventArgs e = new ChoAfterRecordLoadEventArgs() { Record = record, Index = index, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool RaiseRecordLoadError(object record, long index, object source, Exception ex)
        {
            EventHandler<ChoRecordLoadErrorEventArgs> eh = RecordLoadError;
            if (eh == null)
                return true;

            ChoRecordLoadErrorEventArgs e = new ChoRecordLoadErrorEventArgs() { Record = record, Index = index, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
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

        public bool RaiseAfterRecordFieldLoad(object record, long index, string propName, object source)
        {
            EventHandler<ChoAfterRecordFieldLoadEventArgs> eh = AfterRecordFieldLoad;
            if (eh == null)
                return true;

            ChoAfterRecordFieldLoadEventArgs e = new ChoAfterRecordFieldLoadEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source };
            eh(this, e);
            return !e.Stop;
        }

        public bool RaiseRecordFieldLoadError(object record, long index, string propName, object source, Exception ex)
        {
            EventHandler<ChoRecordFieldLoadErrorEventArgs> eh = RecordFieldLoadError;
            if (eh == null)
                return false;

            ChoRecordFieldLoadErrorEventArgs e = new ChoRecordFieldLoadErrorEventArgs() { Record = record, Index = index, PropertyName = propName, Source = source, Exception = ex };
            eh(this, e);
            source = e.Source;
            return e.Handled;
        }

        public virtual bool RaiseMapColumn(int colPos, string colName, out string newColName)
        {
            newColName = null;
            return false;
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
    }
}
