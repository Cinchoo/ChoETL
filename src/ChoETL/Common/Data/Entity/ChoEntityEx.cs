using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoEntityEx
    {
        public static void ScanAndDefineKeyToEntity(this Type type)
        {
            bool hasIdProperty = ChoTypeDescriptor.GetProperties(type).Where(p => String.Compare(p.Name, "id", true) == 0).Any();
            if (hasIdProperty)
                return;

            bool hasKeyDefined = ChoTypeDescriptor.GetProperties(type).Where(pd => pd.Attributes.OfType<KeyAttribute>().Any()).Any();
            if (hasKeyDefined)
                return;

            PropertyDescriptor firstPd = ChoTypeDescriptor.GetProperties(type).FirstOrDefault();
            if (firstPd == null)
                return;
            PropertyDescriptor pd2 = TypeDescriptor.CreateProperty(type, firstPd, new KeyAttribute());
            ChoCustomTypeDescriptor ctd = new ChoCustomTypeDescriptor(TypeDescriptor.GetProvider(type).GetTypeDescriptor(type));
            ctd.OverrideProperty(pd2);
            TypeDescriptor.AddProvider(new ChoTypeDescriptionProvider(ctd), type);
        }
    }
}
