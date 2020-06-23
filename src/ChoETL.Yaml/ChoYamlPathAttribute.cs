using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChoYamlPathAttribute : Attribute
    {
        public string YamlPath { get; private set; }

        public ChoYamlPathAttribute(string yamlPath)
        {
            YamlPath = yamlPath;
        }
    }
}
