using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoOrderedAttribute : ChoMemberAttribute
    {
        public int Order
        {
            get;
            set;
        }

        public ChoOrderedAttribute()
        {
            Order = Int32.MinValue;
        }
    }
}
