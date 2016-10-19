using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoIntRandomAttribute : ChoIntRandomGenerator
    {
        public ChoIntRandomAttribute(int minValue = int.MinValue, int maxValue = int.MaxValue)
            : base(minValue, maxValue)
        {

        }
    }

    public class ChoIntRandomGenerator : ChoRandomAttribute
    {
        private int _minValue = 0;
        private int _maxValue = 0;

        public ChoIntRandomGenerator(int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override object NextValue()
        {
            return ChoIntRandom.Next(_minValue, _maxValue);
        }
    }
}
