using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoXmlElementRecordFieldAttribute : ChoXmlNodeRecordFieldAttribute
    {
        public new string XPath
        {
            get;
            private set;
        }

        public ChoXmlElementRecordFieldAttribute()
        {
        }
    }
}
