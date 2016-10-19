using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ChoCharRandomAttribute : ChoCharRandomGenerator
    {
        public ChoCharRandomAttribute(string chars = null)
            : base(chars)
        {

        }
    }

    public class ChoCharRandomGenerator : ChoRandomAttribute
    {
        private string _chars;

        public ChoCharRandomGenerator(string chars = null)
        {
            _chars = chars;
        }

        public override object NextValue()
        {
            if (_chars.IsNullOrWhiteSpace())
                return ChoCharRandom.Next();
            else
                return ChoCharRandom.Next(_chars);
        }
    }
}
