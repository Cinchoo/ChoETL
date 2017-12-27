using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoCustomSerializable
    {
        void Deserialize(object payload);
        object Serialize();
    }

    public static class ChoCustomSerializable
    {
        public static bool ToTextIfCustomSerialization(this object rec, out string recText)
        {
            recText = null;
            if (rec.GetType().IsDynamicType())
                return false;

            if (typeof(IChoCustomSerializable).IsAssignableFrom(rec.GetType()))
            {
                IChoCustomSerializable obj = rec as IChoCustomSerializable;
                recText = obj.Serialize().ToNString();
                return true;
            }

            return false;
        }

        public static bool FillIfCustomSerialization(this object rec, object payload)
        {
            if (rec.GetType().IsDynamicType())
                return false;

            if (typeof(IChoCustomSerializable).IsAssignableFrom(rec.GetType()))
            {
                IChoCustomSerializable obj = rec as IChoCustomSerializable;
                obj.Deserialize(payload);
                return true;
            }

            return false;
        }

        public static bool IsKeyValueType(this Type type)
        {
            bool isKVPObject = false;
            if (typeof(IChoKeyValueType).IsAssignableFrom(type))
                isKVPObject = true;
            else
            {
                var isKVPAttrDefined = type.GetCustomAttribute<ChoKeyValueTypeAttribute>() != null;
                if (isKVPAttrDefined)
                {
                    var kP = type.GetProperties().Where(p => p.GetCustomAttribute<ChoKeyAttribute>() != null).FirstOrDefault();
                    var vP = type.GetProperties().Where(p => p.GetCustomAttribute<ChoValueAttribute>() != null).FirstOrDefault();
                    if (kP != null && vP != null)
                        isKVPObject = true;
                }
            }

            return isKVPObject;
        }
    }
}