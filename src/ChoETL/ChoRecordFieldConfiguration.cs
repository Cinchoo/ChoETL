using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordFieldConfiguration
    {
        public string Name
        {
            get;
            private set;
        }
        public ChoErrorMode? ErrorMode
        {
            get;
            set;
        }
        public ChoIgnoreFieldValueMode? IgnoreFieldValueMode
        {
            get;
            set;
        }

        public Type FieldType
        {
            get;
            set;
        }

        public ChoRecordFieldConfiguration(string name, ChoRecordFieldAttribute attr = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            Name = name;
            FieldType = typeof(string);

            if (attr != null)
            {
                ErrorMode = attr.ErrorMode;
                IgnoreFieldValueMode = attr.IgnoreFieldValueMode;
                FieldType = attr.FieldType;
            }
        }
    }
}
