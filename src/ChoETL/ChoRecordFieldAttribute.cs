using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ChoRecordFieldAttribute : Attribute
    {
        public ChoErrorMode? ErrorMode
        {
            get;
            set;
        }
        public ChoIgnoreFieldValueMode? IgnoreFieldValueMode
        {
            get;
            set;
        }
        public Type FieldType => typeof(string);

        public ChoRecordFieldAttribute()
        {
        }
    }
}
