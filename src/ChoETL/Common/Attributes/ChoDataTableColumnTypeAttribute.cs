using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ChoDataTableColumnTypeAttribute : Attribute
    {
        public Type Type
        {
            get;
            private set;
        }

        public ChoDataTableColumnTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
