using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoCSVRecordObjectAttribute : ChoFileRecordObjectAttribute
    {
        public string Delimiter
        {
            get;
            private set;
        }
        internal bool? HasExcelSeparatorInternal;
        public bool HasExcelSeparator
        {
            get { throw new NotSupportedException(); }
            set { HasExcelSeparatorInternal = value; }
        }

        public ChoCSVRecordObjectAttribute(string delimiter = null)
        {
            Delimiter = delimiter;
        }
    }
}