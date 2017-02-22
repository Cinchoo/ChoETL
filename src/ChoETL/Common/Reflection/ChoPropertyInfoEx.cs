namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ComponentModel;
    using System.Reflection.Emit;
    using System.Reflection;

    #endregion NameSpaces

    public delegate object MethodHandler(object target, params object[] args);

    public static class ChoPropertyInfoEx
    {
        #region Shared Data Members (Private)

        private static readonly Module _module = typeof(ChoPropertyInfoEx).Module;
        static readonly Type[] _singleObject = new[] { typeof(object) };
        static readonly Type[] _twoObjects = new[] { typeof(object), typeof(object) };
        static readonly Type[] _manyObjects = new[] { typeof(object), typeof(object[]) };

        #endregion Shared Data Members (Private)

        public static bool IsReadOnly(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetSetMethod(true) == null || propertyInfo.GetSetMethod(true).IsPrivate)
                return true;

            return false;
        }

        public static Attribute GetCustomAttribute(this PropertyInfo propertyInfo, Type attributeType)
        {
            return GetCustomAttribute(propertyInfo, attributeType, false);
        }

        public static Attribute GetCustomAttribute(this PropertyInfo propertyInfo, Type attributeType, bool inherit)
        {
            object[] attributes = propertyInfo.GetCustomAttributes(attributeType, inherit);

            return attributes == null || attributes.Length == 0 ? null : attributes[0] as Attribute;
        }

        public static T GetCustomAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute
        {
            return GetCustomAttribute<T>(propertyInfo, false);
        }

        public static T GetCustomAttribute<T>(this PropertyInfo propertyInfo, bool inherit) where T : Attribute
        {
            object[] attributes = propertyInfo.GetCustomAttributes(typeof(T), inherit);

            return attributes == null || attributes.Length == 0 ? null : attributes[0] as T;
        }

        public static string GetDescription(this PropertyInfo propertyInfo)
        {
            DescriptionAttribute DescriptionAttribute = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            if (DescriptionAttribute == null)
                return null;
            return DescriptionAttribute.Description;
        }

        public static Action<object, object> CreateSetMethod(this PropertyInfo propertyInfo)
        {
            return ChoEmitHelper.CreatePropertySetterHandler(propertyInfo);
        }

        public static Func<object, object> CreateGetMethod(this PropertyInfo propertyInfo)
        {
            return ChoEmitHelper.CreatePropertyGetterHandler(propertyInfo);
        }

        #region Commented

        //#region Delegates

        //public delegate void ChoPropertySetter(object target, object value);
        //public delegate object ChoPropertyGetter(object target);

        //#endregion Delegates
        /////
        ///// Creates a dynamic getter for the property
        /////
        //public static ChoPropertyGetter CreateGetMethod(PropertyInfo propertyInfo)
        //{
        //    /*
        //    * If there's no getter return null
        //    */
        //    MethodInfo getMethod = propertyInfo.GetGetMethod();
        //    if (getMethod == null)
        //        return null;

        //    /*
        //    * Create the dynamic method
        //    */
        //    Type[] arguments = new Type[1];
        //    arguments[0] = typeof(object);

        //    DynamicMethod getter = new DynamicMethod(
        //      String.Concat("_Get", propertyInfo.Name, "_"),
        //      typeof(object), arguments, propertyInfo.DeclaringType);
        //    ILGenerator generator = getter.GetILGenerator();
        //    generator.DeclareLocal(typeof(object));
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
        //    generator.EmitCall(OpCodes.Callvirt, getMethod, null);

        //    if (!propertyInfo.PropertyType.IsClass)
        //        generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

        //    generator.Emit(OpCodes.Ret);

        //    /*
        //    * Create the delegate and return it
        //    */
        //    return (ChoPropertyGetter)getter.CreateDelegate(typeof(ChoPropertyGetter));
        //}
        ///
        /// Creates a dynamic setter for the property
        ///

        //public static ChoPropertySetter CreateSetMethod(PropertyInfo propertyInfo)
        //{
        //    /*
        //    * If there's no setter return null
        //    */
        //    MethodInfo setMethod = propertyInfo.GetSetMethod();
        //    if (setMethod == null)
        //        return null;

        //    /*
        //    * Create the dynamic method
        //    */
        //    Type[] arguments = new Type[2];
        //    arguments[0] = arguments[1] = typeof(object);

        //    DynamicMethod setter = new DynamicMethod(
        //      String.Concat("_Set", propertyInfo.Name, "_"),
        //      typeof(void), arguments, propertyInfo.DeclaringType);
        //    ILGenerator generator = setter.GetILGenerator();
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
        //    generator.Emit(OpCodes.Ldarg_1);

        //    if (propertyInfo.PropertyType.IsClass)
        //        generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
        //    else
        //        generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

        //    generator.EmitCall(OpCodes.Callvirt, setMethod, null);
        //    generator.Emit(OpCodes.Ret);

        //    /*
        //    * Create the delegate and return it
        //    */
        //    return (ChoPropertySetter)setter.CreateDelegate(typeof(ChoPropertySetter));
        //}

        #endregion Commented
    }
}
