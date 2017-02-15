using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoXmlRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public string XPath
        {
            get;
            private set;
        }

        public string FieldName
        {
            get;
            set;
        }

        public ChoXmlRecordFieldAttribute(string xPath)
        {
            XPath = xPath;
        }
    }
}
