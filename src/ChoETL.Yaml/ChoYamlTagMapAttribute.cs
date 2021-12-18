using System;
using System.Collections.Generic;
using System.Text;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChoYamlTagMapAttribute : ChoAllowMultipleAttribute
    {
        public string TagMap
        {
            get;
            private set;
        }
        public bool Alias
        {
            get;
            private set;
        }
        public ChoYamlTagMapAttribute(string tagMap, bool alias = false)
        {
            TagMap = tagMap;
            Alias = alias;
        }
    }
}
