using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoIgnoreMemberAttribute : Attribute
    {

    }

    public abstract class ChoMemberAttribute : Attribute
    {
        public bool IsRequired
        {
            get;
            set;
        }
    }
}
