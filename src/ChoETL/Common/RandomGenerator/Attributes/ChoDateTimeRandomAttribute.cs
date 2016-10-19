using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoDateTimeRandomAttribute : ChoDateTimeRandomGenerator
    {
        public ChoDateTimeRandomAttribute()
        {
        }

        public ChoDateTimeRandomAttribute(DateTime from, DateTime to)
            : base(from, to)
        {
        }

    }

    public class ChoDateTimeRandomGenerator : ChoRandomAttribute
    {
        private DateTime _minValue = DateTime.Today;
        private DateTime _maxValue = DateTime.Today.AddYears(50);

        public string Format
        {
            get;
            set;
        }

        public ChoDateTimeRandomGenerator()
        {
        }

        public ChoDateTimeRandomGenerator(DateTime from, DateTime to)
        {
            _minValue = from;
            _maxValue = to;
        }

        public override object NextValue()
        {
            if (Format.IsNullOrWhiteSpace())
                return ChoDateTimeRandom.Next(_minValue, _maxValue);
            else
                return ChoDateTimeRandom.NextAsText(_minValue, _maxValue, Format);
        }
    }
}
