using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoXmlDocumentRootAttribute : Attribute
    {
        public string Name
        {
            get;
            private set;
        }

        public ChoXmlDocumentRootAttribute(string name)
        {
            Name = name;
        }
    }
}
