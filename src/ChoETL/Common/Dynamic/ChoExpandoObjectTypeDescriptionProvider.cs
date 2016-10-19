using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoExpandoObjectTypeDescriptionProvider : TypeDescriptionProvider
    {
        private static readonly TypeDescriptionProvider m_Default = TypeDescriptor.GetProvider(typeof(ExpandoObject));

        public ChoExpandoObjectTypeDescriptionProvider()
            : base(m_Default)
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            var defaultDescriptor = base.GetTypeDescriptor(objectType, instance);

            return instance == null ? defaultDescriptor :
                       new ChoExpandoObjectTypeDescriptor(instance);
        }
    }
}
