using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	public interface IChoNotifyRecordFieldRead
	{
		bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value);
		bool AfterRecordFieldLoad(object target, long index, string propName, object value);
		bool RecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex);
	}

    public interface IChoNotifyFileRead
    {
        bool BeginLoad(object source);
        void EndLoad(object source);

        bool SkipUntil(long index, object source);
        bool DoWhile(long index, object source);
    }

    public interface IChoNotifyRecordRead
    {
        bool BeforeRecordLoad(object target, long index, ref object source);
        bool AfterRecordLoad(object target, long index, object source, ref bool skip);
        bool RecordLoadError(object target, long index, object source, Exception ex);
    }

    public interface IChoNotifyRecordConfigurable
    {
        void RecondConfigure(ChoRecordConfiguration configuration);
    }

    public interface IChoNotifyRecordFieldConfigurable
    {
        void RecondFieldConfigure(ChoRecordFieldConfiguration fieldConfiguration);
    }
}
