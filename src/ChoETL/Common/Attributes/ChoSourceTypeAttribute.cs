using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class ChoSourceTypeAttribute : Attribute
    {
        public Type Type { get; private set; }

        public ChoSourceTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
