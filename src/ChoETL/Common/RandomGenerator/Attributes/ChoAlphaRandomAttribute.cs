using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoAlphaRandomAttribute : ChoAlphaRandomGenerator
    {
        public ChoAlphaRandomAttribute(int length) :
            base(length)
        {

        }
    }

    public class ChoAlphaRandomGenerator : ChoRandomAttribute
    {
        private int _length = 0;

        public ChoAlphaRandomGenerator(int length)
        {
            _length = length;
        }

        public override object NextValue()
        {
            return ChoAlphaRandom.Next(_length);
        }
    }
}
