using System;
using System.Collections.Concurrent;
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
		private static readonly ConcurrentDictionary<Type, AssociatedMetadataTypeTypeDescriptionProvider> _cache = new ConcurrentDictionary<Type, AssociatedMetadataTypeTypeDescriptionProvider>();

        static ChoMetadataTypesRegister()
        {
            foreach (Type type in ChoType.GetTypes(typeof(MetadataTypeAttribute)))
            {
                MetadataTypeAttribute attrib = type.GetCustomAttribute<MetadataTypeAttribute>();
                if (attrib == null || attrib.MetadataClassType == null)
                    continue;

				var prov = new AssociatedMetadataTypeTypeDescriptionProvider(type, attrib.MetadataClassType);
				_cache.AddOrUpdate(type, prov);

				TypeDescriptor.AddProviderTransparent(prov, type);
            }

			foreach (Type type in ChoType.GetTypes(typeof(ChoMetadataRefTypeAttribute)))
			{
				ChoMetadataRefTypeAttribute attrib = type.GetCustomAttribute<ChoMetadataRefTypeAttribute>();
				if (attrib == null || attrib.MetadataRefClassType == null)
					continue;

				var prov = new AssociatedMetadataTypeTypeDescriptionProvider(attrib.MetadataRefClassType, type);
				_cache.AddOrUpdate(attrib.MetadataRefClassType, prov);
				
				TypeDescriptor.AddProviderTransparent(prov, attrib.MetadataRefClassType);
			}
		}

		public static void Init()
        {

        }

		public static void Register(Type type)
		{
			if (type == null)
				return;

			MetadataTypeAttribute attrib = type.GetCustomAttribute<MetadataTypeAttribute>();
			if (attrib != null && attrib.MetadataClassType != null)
			{
				Register(type, attrib.MetadataClassType);
			}
			else
            {
				ChoMetadataRefTypeAttribute attrib1 = type.GetCustomAttribute<ChoMetadataRefTypeAttribute>();
				if (attrib1 != null && attrib1.MetadataRefClassType != null)
                {
					Register(attrib1.MetadataRefClassType, type);
				}
			}
		}

		public static void Register(Type type, Type metaDataType)
        {
			if (type == null || metaDataType == null)
				return;

			var prov = new AssociatedMetadataTypeTypeDescriptionProvider(type, metaDataType);
			_cache.AddOrUpdate(type, prov);

			TypeDescriptor.AddProviderTransparent(prov, type);
		}

		public static void Unregister(Type type)
		{
			if (type == null)
                return;

            try
            {
                AssociatedMetadataTypeTypeDescriptionProvider prov = null;
                if (_cache.TryGetValue(type, out prov))
                {
                    TypeDescriptor.RemoveProviderTransparent(prov, type);
                }
            }
			catch
            {

            }

        }
    }
}
