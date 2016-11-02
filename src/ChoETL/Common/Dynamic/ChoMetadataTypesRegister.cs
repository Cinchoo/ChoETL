using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoMetadataTypesRegister
    {
        static ChoMetadataTypesRegister()
        {
            foreach (Type type in ChoType.GetTypes(typeof(MetadataTypeAttribute)))
            {
                MetadataTypeAttribute attrib = type.GetCustomAttribute<MetadataTypeAttribute>();
                if (attrib == null)
                    continue;

                TypeDescriptor.AddProviderTransparent(
                    new AssociatedMetadataTypeTypeDescriptionProvider(type, attrib.MetadataClassType), type);
            }
        }

        public static void Init()
        {

        }
    }
}
