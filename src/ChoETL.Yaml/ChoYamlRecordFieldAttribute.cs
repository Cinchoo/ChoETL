using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoYamlRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public string YamlPath
        {
            get;
            set;
        }

        public ChoYamlRecordFieldAttribute()
        {

        }

        internal bool? UseYamlSerializationInternal = null;
        public bool UseYamlSerialization
        {
            get { return UseYamlSerializationInternal == null ? false : UseYamlSerializationInternal.Value; }
            set { UseYamlSerializationInternal = value; }
        }
    }
}
