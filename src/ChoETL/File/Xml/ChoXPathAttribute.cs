using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ChoXPathAttribute : Attribute
    {
        public string XPath { get; private set; }

        public bool AllowComplexXPath
        {
            get; set;
        }
        public ChoXPathAttribute(string xPath)
        {
            XPath = xPath;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ChoUseXmlProxyAttribute : Attribute
    {
        public bool Flag { get; private set; }
        public ChoUseXmlProxyAttribute(bool flag = true)
        {
            Flag = flag;
        }
    }
}
