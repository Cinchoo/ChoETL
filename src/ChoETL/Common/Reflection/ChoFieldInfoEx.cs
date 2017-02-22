namespace ChoETL
{
	#region NameSpaces

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Reflection;
	using System.ComponentModel;
    using System.Reflection.Emit;

	#endregion NameSpaces

	public static class ChoFieldInfoEx
	{
        public static bool IsReadOnly(this FieldInfo fieldInfo)
        {
            return fieldInfo.IsInitOnly;
        }
        
        public static Attribute GetCustomAttribute(this FieldInfo fieldInfo, Type attributeType)
		{
			return GetCustomAttribute(fieldInfo, attributeType, false);
		}

		public static Attribute GetCustomAttribute(this FieldInfo fieldInfo, Type attributeType, bool inherit)
		{
			object[] attributes = fieldInfo.GetCustomAttributes(attributeType, inherit);

			return attributes == null || attributes.Length == 0 ? null : attributes[0] as Attribute;
		}

		public static T GetCustomAttribute<T>(this FieldInfo fieldInfo) where T : Attribute
		{
			return GetCustomAttribute<T>(fieldInfo, false);
		}

		public static T GetCustomAttribute<T>(this FieldInfo fieldInfo, bool inherit) where T : Attribute
		{
			object[] attributes = fieldInfo.GetCustomAttributes(typeof(T), inherit);

			return attributes == null || attributes.Length == 0 ? null : attributes[0] as T;
		}

		public static string GetDescription(this FieldInfo fieldInfo)
		{
			DescriptionAttribute DescriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
			if (DescriptionAttribute == null)
				return null;
			return DescriptionAttribute.Description;
		}

        public static Action<object, object> CreateSetMethod(this FieldInfo fieldInfo)
        {
            return ChoEmitHelper.CreateFieldSetterHandler(fieldInfo);
        }

        public static Func<object, object> CreateGetMethod(this FieldInfo fieldInfo)
        {
            return ChoEmitHelper.CreateFieldGetterHandler(fieldInfo);
        }
    }
}
