using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoCSVRecordFieldAttribute : ChoFileRecordFieldAttribute
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

        public ChoCSVRecordFieldAttribute(int fieldPosition)
        {
            FieldPosition = fieldPosition;
        }
    }
}
