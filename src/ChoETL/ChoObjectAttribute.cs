using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class ChoObjectAttribute : Attribute
    {
        public ChoObjectValidationMode ObjectValidationMode
        {
            get;
            set;
        }

        public ChoObjectAttribute()
        {
            ObjectValidationMode = ChoObjectValidationMode.Off;
        }
    }
}