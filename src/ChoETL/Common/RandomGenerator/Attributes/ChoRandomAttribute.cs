using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ChoRandomAttribute : ChoRandomGenerator
    {
    }

    public abstract class ChoRandomGenerator : Attribute
    {
        public abstract object NextValue();
    }
}
