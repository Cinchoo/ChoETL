using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoMemberValueSourceAttribute : ChoOrderedAttribute
    {
        public abstract object GetValue(MemberInfo mi);
    }
}
