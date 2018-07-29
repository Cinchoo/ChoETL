using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChoFieldMapAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public ChoFieldMapAttribute(string name)
        {
            Name = name;
        }
    }
}
