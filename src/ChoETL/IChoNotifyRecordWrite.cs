using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoNotifyRecordWrite
    {
        bool BeginWrite(object source);
        void EndWrite(object source);

        bool BeforeRecordWrite(object target, int index, ref object source);
        bool AfterRecordWrite(object target, int index, object source);
        bool RecordWriteError(object target, int index, object source, Exception ex);

        bool BeforeRecordFieldWrite(object target, int index, string propName, ref object value);
        bool AfterRecordFieldWrite(object target, int index, string propName, object value);
        bool RecordFieldWriteError(object target, int index, string propName, object value, Exception ex);
    }
}
