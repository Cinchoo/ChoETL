using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoNotifyRecordRead
    {
        bool BeginLoad(object source);
        void EndLoad(object source);

        bool BeforeRecordLoad(object target, int index, ref object source);
        bool AfterRecordLoad(object target, int index, object source);
        bool RecordLoadError(object target, int index, object source, Exception ex);

        bool BeforeRecordFieldLoad(object target, int index, string propName, ref object value);
        bool AfterRecordFieldLoad(object target, int index, string propName, object value);
        bool RecordFieldLoadError(object target, int index, string propName, object value, Exception ex);
    }
}
