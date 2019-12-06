using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoDictionaryKeyAttribute : Attribute
    {
        public string Keys { get; private set; }
        public ChoDictionaryKeyAttribute(string keys)
        {
            Keys = keys;
        }
    }
}