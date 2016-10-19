using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ChoAlphaNumericRandomAttribute : ChoAlphaNumericRandomGenerator
    {
        public ChoAlphaNumericRandomAttribute(int length)
            : base(length)
        {

        }
    }

    public class ChoAlphaNumericRandomGenerator : ChoRandomAttribute
    {
        private int _length = 0;

        public ChoAlphaNumericRandomGenerator(int length)
        {
            _length = length;
        }

        public override object NextValue()
        {
            return ChoAlphaNumericRandom.Next(_length);
        }
    }
}
