using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChoFieldPositionAttribute : Attribute
    {
        public readonly int Position;

        public ChoFieldPositionAttribute(int position)
        {
            Position = position;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChoArrayIndexAttribute : Attribute
    {
        public readonly int Position;

        public ChoArrayIndexAttribute(int position)
        {
            Position = position;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ChoArrayAttribute : Attribute
    {
        public ChoArrayAttribute()
        {
        }
    }
}
