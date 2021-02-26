using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoAvroRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public bool Nullable
        {
            get;
            set;
        }

        public ChoAvroRecordFieldAttribute()
        {
        }
    }
}
