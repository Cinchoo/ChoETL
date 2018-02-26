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
                if (attrib == null || attrib.MetadataClassType == null)
                    continue;

                TypeDescriptor.AddProviderTransparent(
                    new AssociatedMetadataTypeTypeDescriptionProvider(type, attrib.MetadataClassType), type);
            }

			foreach (Type type in ChoType.GetTypes(typeof(ChoMetadataRefTypeAttribute)))
			{
				ChoMetadataRefTypeAttribute attrib = type.GetCustomAttribute<ChoMetadataRefTypeAttribute>();
				if (attrib == null || attrib.MetadataRefClassType == null)
					continue;

				TypeDescriptor.AddProviderTransparent(
					new AssociatedMetadataTypeTypeDescriptionProvider(attrib.MetadataRefClassType, type), attrib.MetadataRefClassType);
			}
		}

		public static void Init()
        {

        }

		public static void Register(Type type, Type metaDataType)
		{
			if (type == null || metaDataType == null)
				return;

			TypeDescriptor.AddProviderTransparent(
				new AssociatedMetadataTypeTypeDescriptionProvider(type, metaDataType), type);
		}
	}
}
