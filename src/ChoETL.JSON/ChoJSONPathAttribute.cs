using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ChoJSONPathAttribute : Attribute
    {
        public string JSONPath { get; private set; }

        public bool AllowComplexJSONPath
        {
            get; set;
        }
        public ChoJSONPathAttribute(string jsonPath)
        {
            JSONPath = jsonPath;
        }
    }
}
