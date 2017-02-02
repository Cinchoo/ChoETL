using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoRecordTypeCodeAttribute : Attribute
    {
        public string Code { get; private set; }

        public ChoRecordTypeCodeAttribute(object code)
        {
            ChoGuard.ArgumentNotNullOrEmpty(code, "RecordTypeCode");

            if (code.GetType().IsEnum)
            {
                Code = Convert.ChangeType(code, Enum.GetUnderlyingType(code.GetType())).ToNString();
            }
            else
                Code = code.ToNString();
        }
    }

}
