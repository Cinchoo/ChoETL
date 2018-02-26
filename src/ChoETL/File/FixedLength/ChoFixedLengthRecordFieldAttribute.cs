using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoFixedLengthRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public int StartIndex
        {
            get;
            private set;
        }

        public string AltFieldNames
        {
            get;
            set;
        }

        public ChoFixedLengthRecordFieldAttribute(int startIndex, int size)
        {
            StartIndex = startIndex;
            Size = size;
        }
    }
}
