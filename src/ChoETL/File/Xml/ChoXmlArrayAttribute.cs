using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ChoXmlArrayAttribute : Attribute
	{
        public string ArrayNodeName
        {
            get;
            set;
        }
        public bool Flag
        {
            get;
            private set;
        }

        public ChoXmlArrayAttribute()
        {
            Flag = true;
        }

        public ChoXmlArrayAttribute(bool flag)
        {
            Flag = flag;
        }
    }
}
