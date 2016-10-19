using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Reflection;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoStringRandomAttribute : ChoStringRandomGenerator
    {
        public ChoStringRandomAttribute(string chars, int length)
            : base(chars, length)
        {
        }
    }

    public class ChoStringRandomGenerator : ChoRandomAttribute
    {
        private int _length = 0;
        private string _chars;

        public ChoStringRandomGenerator(string chars, int length)
        {
            _chars = chars;
            _length = length;
        }

        public override object NextValue()
        {
            return ChoStringRandom.Next(_chars, _length);
        }
    }
}
