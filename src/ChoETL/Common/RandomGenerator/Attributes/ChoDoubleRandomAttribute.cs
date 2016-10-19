using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoDoubleRandomAttribute : ChoDoubleRandomGenerator
    {
        public ChoDoubleRandomAttribute(double minValue = double.MinValue, double maxValue = double.MaxValue)
            : base(minValue, maxValue)
        {

        }
    }

    public class ChoDoubleRandomGenerator : ChoRandomAttribute
    {
        private double _minValue = 0;
        private double _maxValue = 0;

        public ChoDoubleRandomGenerator(double minValue = double.MinValue, double maxValue = double.MaxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override object NextValue()
        {
            return ChoDoubleRandom.Next(_minValue, _maxValue);
        }
    }
}