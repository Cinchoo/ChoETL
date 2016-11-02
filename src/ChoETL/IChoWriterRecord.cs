using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoWriterRecord
    {
        bool BeginLoad(object source);
        void EndLoad(object source);

        bool BeforeRecordLoad(int index, ref object source);
        bool AfterRecordLoad(int index, object source);
        bool RecordLoadError(int index, object source, Exception ex);

        bool BeforeRecordFieldLoad(int index, string propName, ref object value);
        bool AfterRecordFieldLoad(int index, string propName, object value);
        bool RecordFieldLoadError(int index, string propName, object value, Exception ex);
    }
}
