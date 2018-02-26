using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal enum ChoXmlNodeRecordFieldType { Element, Attribute };

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoXmlNodeRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public string XPath
        {
            get;
            set;
        }
        public bool? EncodeValue
        {
            get;
            set;
        }
        public bool UseXmlSerialization
        {
            get;
            set;
        }
        internal ChoXmlNodeRecordFieldType XmlNodeRecordFieldType
        {
            get;
            set;
        }

        public ChoXmlNodeRecordFieldAttribute()
        {

        }
    }
}
