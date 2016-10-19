using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoNumericRandomAttribute : ChoNumericRandomGenerator
    {
        public ChoNumericRandomAttribute(int length)
            : base(length)
        {
        }
    }

    public class ChoNumericRandomGenerator : ChoRandomAttribute
    {
        private int _length = 0;

        public ChoNumericRandomGenerator(int length)
        {
            _length = length;
        }

        public override object NextValue()
        {
            return ChoNumericRandom.Next(_length);
        }
    }
}
