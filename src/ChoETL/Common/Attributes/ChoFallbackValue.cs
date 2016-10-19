using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChoFallbackValueAttribute : Attribute
    {
        public readonly object Value;

        public ChoFallbackValueAttribute(object value)
        {
            Value = value;
        }
    }
}
