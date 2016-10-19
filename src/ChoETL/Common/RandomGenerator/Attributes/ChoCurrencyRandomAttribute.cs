using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoCurrencyRandomAttribute : ChoCurrencyRandomGenerator
    {
        public ChoCurrencyRandomAttribute()
        {
        }

        public ChoCurrencyRandomAttribute(double minValue, double maxValue)
            : base(minValue, maxValue)
        {
        }

    }

    public class ChoCurrencyRandomGenerator : ChoRandomAttribute
    {
        private double _minValue = double.MinValue + 100;
        private double _maxValue = double.MinValue - 100;

        public string Format
        {
            get;
            set;
        }

        public ChoCurrencyRandomGenerator()
        {
        }

        public ChoCurrencyRandomGenerator(double minValue, double maxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public override object NextValue()
        {
            if (Format.IsNullOrWhiteSpace())
                return ChoCurrencyRandom.Next(_minValue, _maxValue);
            else
                return ChoCurrencyRandom.NextAsText(_minValue, _maxValue, Format);
        }
    }
}
