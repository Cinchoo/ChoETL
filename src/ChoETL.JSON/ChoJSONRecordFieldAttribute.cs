using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public class ChoJSONRecordFieldAttribute : ChoFileRecordFieldAttribute
    {
        public string JSONPath
        {
            get;
            set;
        }

        public ChoJSONRecordFieldAttribute()
        {

        }

        internal bool? UseJSONSerializationInternal = null;
        public bool UseJSONSerialization
        {
            get { return UseJSONSerializationInternal == null ? false : UseJSONSerializationInternal.Value; }
            set { UseJSONSerializationInternal = value; }
        }
    }
}
