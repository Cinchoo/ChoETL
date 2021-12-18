using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoAllowMultipleAttribute : Attribute
    {
        public override object TypeId => this;
    }
}
