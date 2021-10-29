using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ChoUseJSONSerializationAttribute : Attribute
	{
        public bool Flag
        {
            get;
            private set;
        }

        public ChoUseJSONSerializationAttribute()
        {
            Flag = true;
        }

        public ChoUseJSONSerializationAttribute(bool flag)
        {
            Flag = flag;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ChoTurnOffImplicitOpsAttribute : Attribute
    {
        public bool Flag
        {
            get;
            private set;
        }

        public ChoTurnOffImplicitOpsAttribute()
        {
            Flag = true;
        }

        public ChoTurnOffImplicitOpsAttribute(bool flag)
        {
            Flag = flag;
        }
    }
}
