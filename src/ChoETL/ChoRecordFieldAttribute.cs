using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ChoRecordFieldAttribute : Attribute
    {
        internal ChoErrorMode? ErrorModeInternal;
        public ChoErrorMode ErrorMode
        {
            get { return ErrorModeInternal.CastTo<ChoErrorMode>(0); }
            set { ErrorModeInternal = value; }
        }
        internal ChoIgnoreFieldValueMode? IgnoreFieldValueModeInternal;
        public ChoIgnoreFieldValueMode IgnoreFieldValueMode
        {
            get { return IgnoreFieldValueModeInternal.CastTo<ChoIgnoreFieldValueMode>(); }
            set { IgnoreFieldValueModeInternal = value; }
        }
        public Type FieldType
        {
            get;
            set;
        }
        internal bool? IsNullableInternal;
        public bool IsNullable
        {
            get { return IsNullableInternal.CastTo<bool>(); }
            set { IsNullableInternal = value; }
        }
        public string SourceFormat
        {
            get;
            set;
        }
        public string FormatText
        {
            get;
            set;
        }

        public ChoRecordFieldAttribute()
        {
        }
    }
}
