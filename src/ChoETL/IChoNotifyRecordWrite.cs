using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoNotifyRecordFieldWrite
    {
        bool BeforeRecordFieldWrite(object target, long index, string propName, ref object value);
        bool AfterRecordFieldWrite(object target, long index, string propName, object value);
        bool RecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex);
    }

    public interface IChoNotifyFileWrite
    {
        bool BeginWrite(object source);
        void EndWrite(object source);
    }

    public interface IChoNotifyFileHeaderArrange
    {
        bool FileHeaderArrange(List<string> fields);
    }

    public interface IChoNotifyFileHeaderWrite
    {
        bool FileHeaderWrite(ref string headerText);
    }

    public interface IChoNotifyRecordWrite
    {
        bool BeforeRecordWrite(object target, long index, ref object source);
        bool AfterRecordWrite(object target, long index, object source);
        bool RecordWriteError(object target, long index, object source, Exception ex);
    }

    public interface IChoArrayItemFieldNameOverrideable
    {
        string GetFieldName(string declaringMemberName, string memberName, char separator, int index);
    }

    public interface IChoItemConvertable
    {
        object ItemConvert(string propName, object value);
    }

    public interface IChoRecordTypeSelector
    {
        Type SelectRecordType(string propName, object value);
    }
}
