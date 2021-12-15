using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChoIsNullableAttribute : Attribute
    {
        public readonly bool Flag;

        public ChoIsNullableAttribute(bool flag)
        {
            Flag = flag;
        }
    }
}
