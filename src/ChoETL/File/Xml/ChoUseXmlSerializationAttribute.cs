using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ChoUseXmlSerializationAttribute : Attribute
	{
        public bool Flag
        {
            get;
            private set;
        }

        public ChoUseXmlSerializationAttribute()
        {
            Flag = true;
        }

        public ChoUseXmlSerializationAttribute(bool flag)
        {
            Flag = flag;
        }
    }
}
