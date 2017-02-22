namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ComponentModel;
    using System.Reflection;

    #endregion NameSpaces

    public static class ChoMemberInfoEx
    {
        public static string GetFullName(this MemberInfo memberInfo)
        {
            return memberInfo == null ? null : "{0}.{1}".FormatString(memberInfo.DeclaringType.Name, memberInfo.Name);
        }

        public static bool IsReadOnly(this MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo && ((FieldInfo)memberInfo).IsInitOnly)
                return true;
            if (memberInfo is PropertyInfo && (((PropertyInfo)memberInfo).GetSetMethod(true) == null || ((PropertyInfo)memberInfo).GetSetMethod(true).IsPrivate))
                return true;

            return false;
        }

        public static Attribute GetCustomAttribute(this MemberInfo memberInfo, Type attributeType)
        {
            return GetCustomAttribute(memberInfo, attributeType, false);
        }

        public static Attribute GetCustomAttribute(this MemberInfo memberInfo, Type attributeType, bool inherit)
        {
            object[] attributes = memberInfo.GetCustomAttributes(attributeType, inherit);

            return attributes == null || attributes.Length == 0 ? null : attributes[0] as Attribute;
        }

        public static T GetCustomAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return GetCustomAttribute<T>(memberInfo, false);
        }

        public static T GetCustomAttribute<T>(this MemberInfo memberInfo, bool inherit) where T : Attribute
        {
            object[] attributes = memberInfo.GetCustomAttributes(typeof(T), inherit);

            return attributes == null || attributes.Length == 0 ? null : attributes[0] as T;
        }

        public static string GetDescription(this MemberInfo memberInfo)
        {
            DescriptionAttribute DescriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
            if (DescriptionAttribute == null)
                return null;
            return DescriptionAttribute.Description;
        }

        public static object GetConvertedValue(this MemberInfo memberInfo, object value)
        {
            return ChoConvert.ConvertFrom(value, memberInfo);
         //   return ChoConvert.ConvertTo(null, value, ChoType.GetMemberType(memberInfo),
         //ChoTypeDescriptor.GetTypeConverters(memberInfo), ChoTypeDescriptor.GetTypeConverterParams(memberInfo));
        }

        //public static object GetDefaultValue(this MemberInfo memberInfo)
        //{
        //    ChoPropertyInfoAttribute memberInfoAttribute = ChoType.GetMemberAttribute<ChoPropertyInfoAttribute>(memberInfo);
        //    if (memberInfoAttribute != null)
        //        return memberInfoAttribute.DefaultValue;
        //    else
        //    {
        //        DefaultValueAttribute defaultValueAttribute = ChoType.GetMemberAttribute<DefaultValueAttribute>(memberInfo);
        //        if (defaultValueAttribute != null)
        //            return defaultValueAttribute.Value;
        //        else
        //            return null;
        //    }
        //}

        //public static object GetConvertedDefaultValue(this MemberInfo memberInfo)
        //{
        //    ChoPropertyInfoAttribute memberInfoAttribute = ChoType.GetMemberAttribute<ChoPropertyInfoAttribute>(memberInfo);
        //    if (memberInfoAttribute != null)
        //        return ChoConvert.ConvertFrom(GetDefaultValue(memberInfo), memberInfo);
        //    else
        //    {
        //        DefaultValueAttribute defaultValueAttribute = ChoType.GetMemberAttribute<DefaultValueAttribute>(memberInfo);
        //        if (defaultValueAttribute != null)
        //            return ChoConvert.ConvertFrom(defaultValueAttribute.Value, memberInfo);
        //        else
        //            return null;
        //    }
        //    //if (memberInfoAttribute != null)
        //    //    return ChoConvert.ConvertTo(null, GetDefaultValue(memberInfo), ChoType.GetMemberType(memberInfo),
        //    //        ChoTypeDescriptor.GetTypeConverters(memberInfo), ChoTypeDescriptor.GetTypeConverterParams(memberInfo));
        //    //else
        //    //    return null;
        //}
    }
}
