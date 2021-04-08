using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoXmlRecordObjectAttribute : ChoFileRecordObjectAttribute
    {
        public string XPath { get; set; }
    }
}