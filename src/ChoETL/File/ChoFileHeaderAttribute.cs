using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ChoFileHeaderAttribute : Attribute
    {
        public static readonly ChoFileHeaderAttribute Default = new ChoFileHeaderAttribute();

        public bool IgnoreCase => true;

        public char FillChar
        {
            get;
            set;
        }

        public ChoFieldValueJustification Justification
        {
            get;
            set;
        }

        public ChoFieldValueTrimOption TrimOption
        {
            get;
            set;
        }

        public bool Truncate
        {
            get;
            set;
        }

        public ChoFileHeaderAttribute()
        {
            FillChar = ' ';
            Justification = ChoFieldValueJustification.None;
            TrimOption = ChoFieldValueTrimOption.Trim;
            Truncate = false;
        }
    }

}
