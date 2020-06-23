using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ChoUseYamlSerializationAttribute : Attribute
	{
        public bool Flag
        {
            get;
            private set;
        }

        public ChoUseYamlSerializationAttribute()
        {
            Flag = true;
        }

        public ChoUseYamlSerializationAttribute(bool flag)
        {
            Flag = flag;
        }
    }
}
