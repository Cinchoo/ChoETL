using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoParquetRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public int FieldPosition
        {
            get;
            internal set;
        }

        public string AltFieldNames
        {
            get;
            set;
        }

        public ChoParquetRecordFieldAttribute()
        {
        }

        public ChoParquetRecordFieldAttribute(int fieldPosition)
        {
            FieldPosition = fieldPosition;
        }
    }
}
