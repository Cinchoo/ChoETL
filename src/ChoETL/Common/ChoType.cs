namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;
    using System.Xml.Serialization;

    #endregion NameSpaces

    public static class ChoType
    {
        #region TypeInfo Class (Private)

        private class TypeInfo
        {
            public MemberInfo[] MemberInfos;
            public FieldInfo[] FieldInfos;
            //public PropertyInfo[] PropertyInfos;
        }

        #endregion TypeInfo Class (Private)

        #region Shared Data Members (Private)

        private static readonly Dictionary<Type, Type[]> _attributeTypesCache = new Dictionary<Type, Type[]>();
        private static readonly object _attributeTypesCacheLockObject = new object();

        private static Hashtable _typeInfos = Hashtable.Synchronized(new Hashtable());
        private static Dictionary<Type, Attribute[]> _typeAttributesCache = new Dictionary<Type, Attribute[]>();
        private static readonly object _typeAttributesCacheLockObject = new object();

        private static Dictionary<MemberInfo, Dictionary<Type, List<Attribute>>> _typeMemberAttributesCache = new Dictionary<MemberInfo, Dictionary<Type, List<Attribute>>>();
        private static Dictionary<MemberInfo, List<Attribute>> _typeMemberAllAttributesCache = new Dictionary<MemberInfo, List<Attribute>>();
        private static readonly object _typeMemberAttributesCacheLockObject = new object();

        private static Dictionary<Type, Dictionary<Type, MemberInfo[]>> _typeMembersDictionaryCache = new Dictionary<Type, Dictionary<Type, MemberInfo[]>>();
        private static readonly object _typeMembersDictionaryCacheLockObject = new object();

        private static readonly Dictionary<string, MethodInfo> _typeMethodsCache = new Dictionary<string, MethodInfo>();

        private static readonly object _padLock = new object();
        //private readonly Dictionary<PointerPair, Attribute> attributeCache = new Dictionary<PointerPair, Attribute>();
        private static readonly Dictionary<IntPtr, MemberInfo[]> _memberCache = new Dictionary<IntPtr, MemberInfo[]>();
        private static readonly Dictionary<IntPtr, Func<object>> _constructorCache = new Dictionary<IntPtr, Func<object>>();
        private static readonly Dictionary<IntPtr, Func<object, object>> _getterCache = new Dictionary<IntPtr, Func<object, object>>();
        private static readonly Dictionary<IntPtr, Action<object, object>> _setterCache = new Dictionary<IntPtr, Action<object, object>>();
        //private static readonly Dictionary<IntPtr, MethodHandler> _methodCache = new Dictionary<IntPtr, MethodHandler>();

        #endregion

        #region Constructors

        static ChoType()
        {
        }

        #endregion

        #region Shared Members (Public)

        private static readonly Dictionary<Tuple<Type, Type>, bool> _dictTypesDefinedGenericType = new Dictionary<Tuple<Type, Type>, bool>();
        private static readonly object _dictTypesDefinedGenericTypeLock = new object();

        //public static bool IsDynamicRecord(this Type type)
        //{
        //    return ChoType.GetAttribute<ChoDynamicRecordAttribute>(type) != null && typeof(IChoDynamicRecord).IsAssignableFrom(type);
        //}

        public static string GetDisplayName(this PropertyDescriptor pd, string defaultValue = null)
        {
            if (pd != null)
            {
                DisplayNameAttribute dnAttr = pd.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                if (dnAttr != null && !dnAttr.DisplayName.IsNullOrWhiteSpace())
                {
                    return dnAttr.DisplayName.Trim();
                }
                else
                {
                    DisplayAttribute dpAttr = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                    if (dpAttr != null)
                    {
                        if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                            return dpAttr.ShortName.Trim();
                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                            return dpAttr.Name.Trim();
                    }
                }

                return defaultValue == null ? pd.Name : defaultValue;
            }
            else
                return defaultValue;
        }

        public static bool IsDynamicType(this Type type)
        {
            return type == null ? true : type == typeof(ExpandoObject) || typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type) || type == typeof(object) || type.IsAnonymousType()
                || typeof(IDictionary).IsAssignableFrom(type) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        public static bool IsAnonymousType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsImplGenericTypeDefinition(this Type type, Type genericType)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            Tuple<Type, Type> tuple = new Tuple<Type, Type>(type, genericType);
            bool isDefined = false;
            if (_dictTypesDefinedGenericType.TryGetValue(tuple, out isDefined))
                return isDefined;

            lock (_dictTypesDefinedGenericTypeLock)
            {
                if (_dictTypesDefinedGenericType.TryGetValue(tuple, out isDefined))
                    return isDefined;

                isDefined = type.GetInterfaces().Any(x =>
                    x.IsGenericType &&
                    x.GetGenericTypeDefinition() == genericType);

                _dictTypesDefinedGenericType.Add(tuple, isDefined);

                return isDefined;
            }
        }

        #region GetConstructor Overloads (Public)

        public static ConstructorInfo GetConstructor(string typeName, Type[] parameterTypes)
        {
            return GetConstructor(GetType(typeName), parameterTypes);
        }

        public static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes)
        {
            if (type == null)
                throw new NullReferenceException("Missing type.");

            return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
        }

        #endregion GetConstructor Overloads (Public)

        #region GetDefaultConstructor Overloads (Public)

        public static ConstructorInfo GetDefaultConstructor(string typeName)
        {
            return GetDefaultConstructor(GetType(typeName));
        }

        public static ConstructorInfo GetDefaultConstructor(Type type)
        {
            if (type == null)
                throw new NullReferenceException("Missing type.");

            return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
        }

        #endregion GetDefaultConstructor Overloads (Public)

        #region Other Members

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            return System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == typeName);
        }

        public static string GetMemberName(MemberInfo memberInfo)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");
            return "{0}.{1}".FormatString(memberInfo.ReflectedType.Name, memberInfo.Name);
        }

        public static string GetMemberName(PropertyDescriptor pd)
        {
            ChoGuard.ArgumentNotNull(pd, "PropertyDescriptor");
            return "{0}.{1}".FormatString(pd.ComponentType.Name, pd.Name);
        }

        #endregion Other Members

        #region HasConstructor Overloads

        public static bool HasDefaultConstructor(Type type)
        {
            ConstructorInfo constructorInfo = type.GetConstructor(new Type[] { });
            return constructorInfo != null;
        }

        public static bool HasConstructor(Type type, object[] parameters)
        {
            Type[] types = ChoType.ConvertToTypes(parameters);
            ConstructorInfo constructorInfo = type.GetConstructor(types);
            return constructorInfo != null;
        }

        #endregion HasConstructor Overloads

        #region HasProperty Overloads

        public static PropertyInfo GetProperty(Type type, string name)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            return type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }

        public static bool TryGetProperty(Type type, string name, out PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return propertyInfo != null;
        }

        public static bool HasProperty(Type type, string name)
        {
            PropertyInfo propertyInfo = null;
            return HasProperty(type, name, out propertyInfo);
        }

        public static bool HasProperty(Type type, string name, out PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return propertyInfo != null;
        }

        #endregion HasProperty Overloads

        #region HasGetProperty Overloads

        public static bool HasGetProperty(Type type, string name)
        {
            PropertyInfo propertyInfo = null;
            return HasGetProperty(type, name, out propertyInfo);
        }

        public static bool HasGetProperty(Type type, string name, out PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return propertyInfo != null && propertyInfo.CanRead;
        }

        #endregion HasGetProperty Overloads

        #region HasSetProperty Overloads

        public static bool HasSetProperty(Type type, string name)
        {
            PropertyInfo propertyInfo = null;
            return HasSetProperty(type, name, out propertyInfo);
        }

        public static bool HasSetProperty(Type type, string name, out PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return propertyInfo != null && propertyInfo.CanWrite;
        }

        #endregion HasSetProperty Overloads

        #region HasField Overloads

        public static bool HasField(Type type, string name)
        {
            FieldInfo fieldInfo = null;
            return HasField(type, name, out fieldInfo);
        }

        public static bool HasField(Type type, string name, out FieldInfo fieldInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return fieldInfo != null;
        }

        public static bool IsReadonlyField(Type type, string name)
        {
            FieldInfo fieldInfo = null;
            return HasField(type, name, out fieldInfo) ? fieldInfo.IsInitOnly : false;
        }

        #endregion HasField Overloads

        #region CreateInstanceWithReflectionPermission Overloads

        public static object CreateInstanceWithReflectionPermission(string typeName)
        {
            return CreateInstanceWithReflectionPermission(GetType(typeName));
        }

        //[ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        public static object CreateInstanceWithReflectionPermission(Type type)
        {
            return ChoActivator.CreateInstance(type, true);
        }

        //[ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        public static object CreateInstanceWithReflectionPermission(Type type, object[] args)
        {
            return ChoActivator.CreateInstance(type, args);
        }

        #endregion CreateInstanceWithReflectionPermission Overloads

        #region InvokeMethod Overloads

        public static bool HasMethod(Type objType, string name, Type[] argsTypes, Type returnType = null)
        {
            ChoGuard.ArgumentNotNull(objType, "Type");

            try
            {
                MethodInfo methodInfo = objType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, argsTypes, null);
                if (methodInfo == null)
                    return false;

                if (returnType != null)
                    return methodInfo.ReturnType == returnType;
                else
                    return true;
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", objType.FullName, name), ex.InnerException);
            }
        }

        public static bool HasMethod(Type objType, string name, Type[] argsTypes, Type returnType, out MethodInfo methodInfo)
        {
            ChoGuard.ArgumentNotNull(objType, "Type");
            methodInfo = null;

            try
            {
                MethodInfo methodInfo1 = objType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, argsTypes, null);
                if (methodInfo1 == null)
                    return false;

                if (returnType != null)
                {
                    if (methodInfo.ReturnType == returnType)
                    {
                        methodInfo = methodInfo1;
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    methodInfo = methodInfo1;
                    return true;
                }
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", objType.FullName, name), ex.InnerException);
            }
        }

        public static bool HasMethod(object target, string name, Type[] argsTypes)
        {
            ChoGuard.ArgumentNotNull(target, "Target");

            try
            {
                return target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, argsTypes, null) != null;
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, name), ex.InnerException);
            }
        }

        public static bool HasMethod(object target, string name, object[] args)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            try
            {
                return target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, ConvertToTypesArray(args), null) != null;
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, name), ex.InnerException);
            }
        }

        public static object InvokeMethod(object target, MethodInfo methodInfo, object[] args)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull(methodInfo, "MethodInfo");

            try
            {
                return methodInfo.Invoke(target, args);
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, methodInfo.Name), ex.InnerException);
            }
        }

        public static object InvokeMethod(object target, string name, object[] args)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            try
            {
                MethodInfo methodInfo = target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, ConvertToTypesArray(args), null);
                if (methodInfo != null)
                    return methodInfo.Invoke(target, args);
                else
                    throw new ApplicationException(String.Format("Can't find {0} method in {1} type.", name, target.GetType().FullName));
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, name), ex.InnerException);
            }
        }

        public static bool HasMethod(Type type, string name, object[] args, out MethodInfo methodInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            methodInfo = null;

            try
            {
                methodInfo = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, ConvertToTypesArray(args), null);
                return methodInfo != null;
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, name), ex.InnerException);
            }
        }

        public static bool HasMethod(Type type, string name, object[] args)
        {
            MethodInfo methodInfo = null;
            return HasMethod(type, name, args, out methodInfo);
        }

        public static object InvokeMethod(Type type, string name, object[] args)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            try
            {
                MethodInfo methodInfo = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, ConvertToTypesArray(args), null);
                if (methodInfo != null)
                    return methodInfo.Invoke(null, args);
                else
                    throw new ApplicationException(String.Format("Can't find {0} method in {1} type.", name, type.FullName));
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, name), ex.InnerException);
            }
        }

        #endregion InvokeMethod Overloads

        #region Get & Set Field Value methods

        public static object GetFieldValue(object target, string name)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (target is Type) return GetStaticFieldValue(target as Type, name);

            FieldInfo fieldInfo = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new ApplicationException(String.Format("Missing {0} field in {1} object.", name, target.GetType().FullName));

            return GetFieldValue(target, fieldInfo);
        }

        public static object GetFieldValue(object target, FieldInfo fieldInfo)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(fieldInfo, "FieldInfo");

            if (target is Type) return GetStaticFieldValue(target as Type, fieldInfo);

            try
            {
#if _DYNAMIC_
                Func<object, object> getter;
                if (!_getterCache.TryGetValue(fieldInfo.FieldHandle.Value, out getter))
                {
                    lock (_padLock)
                    {
                        if (!_getterCache.TryGetValue(fieldInfo.FieldHandle.Value, out getter))
                            _getterCache.Add(fieldInfo.FieldHandle.Value, getter = fieldInfo.CreateGetMethod());
                    }
                }
                return getter(target);
#else
                return fieldInfo.GetValue(target);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, fieldInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static void ConvertNSetFieldValue(object target, string name, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == (FieldInfo)null)
                throw new ApplicationException(string.Format("Can't find {0} field in {1} object.", (object)name, (object)target.GetType().FullName));
            ConvertNSetFieldValue(target, field, val, culture);
        }

        public static void ConvertNSetFieldValue(object target, FieldInfo fieldInfo, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty((object)fieldInfo, "FieldInfo");
            if ((val == null || (val is string && ((string)val).IsEmpty())) && fieldInfo.FieldType.IsValueType)
            {
                if (fieldInfo.FieldType.IsNullableType())
                    val = null;
                else
                    val = fieldInfo.FieldType.Default();
            }

            if (target is Type)
            {
                ConvertNSetStaticFieldValue(target as Type, fieldInfo, val, culture);
            }
            else
            {
                try
                {
                    val = ChoUtility.ConvertValueToObjectMemberType(target, (MemberInfo)fieldInfo, val, culture);

#if _DYNAMIC_
                Action<object, object> setter;
                if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                {
                    lock (_padLock)
                    {
                        if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                            _setterCache.Add(fieldInfo.FieldHandle.Value, setter = fieldInfo.CreateSetMethod());
                    }
                }
                setter(target, val);
#else
                    fieldInfo.SetValue(target, val);
#endif
                }
                catch (TargetInvocationException ex)
                {
                    throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, fieldInfo.Name), ex.InnerException);
                    //throw ex.InnerException;
                }
            }
        }

        public static void SetFieldValue(object target, string name, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            FieldInfo fieldInfo = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} field in {1} object.", name, target.GetType().FullName));

            SetFieldValue(target, fieldInfo, val);
        }

        public static void SetFieldValue(object target, FieldInfo fieldInfo, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(fieldInfo, "FieldInfo");

            if ((val == null || (val is string && ((string)val).IsEmpty())) && fieldInfo.FieldType.IsValueType)
            {
                if (fieldInfo.FieldType.IsNullableType())
                    val = null;
                else
                    val = fieldInfo.FieldType.Default();
            }

            if (target is Type)
            {
                SetStaticFieldValue(target as Type, fieldInfo, val);
                return;
            }

            //***
            //ChoValidation.Validate(fieldInfo as MemberInfo, val);

            try
            {
#if _DYNAMIC_
                Action<object, object> setter;
                if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                {
                    lock (_padLock)
                    {
                        if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                            _setterCache.Add(fieldInfo.FieldHandle.Value, setter = fieldInfo.CreateSetMethod());
                    }
                }
                setter(target, val);
#else
                fieldInfo.SetValue(target, val);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, fieldInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        #endregion Get & Set Field Value methods

        #region Get & Set Static Field Value methods

        public static object GetStaticFieldValue(Type type, string name)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} field in {1} object.", name, type.FullName));

            return GetStaticFieldValue(type, fieldInfo);
        }

        public static object GetStaticFieldValue(Type type, FieldInfo fieldInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(fieldInfo, "FieldInfo");

            try
            {
#if _DYNAMIC_
                Func<object, object> getter;
                if (!_getterCache.TryGetValue(fieldInfo.FieldHandle.Value, out getter))
                {
                    lock (_padLock)
                    {
                        if (!_getterCache.TryGetValue(fieldInfo.FieldHandle.Value, out getter))
                            _getterCache.Add(fieldInfo.FieldHandle.Value, getter = fieldInfo.CreateGetMethod());
                    }
                }
                return getter(null);
#else
                return fieldInfo.GetValue(null);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, fieldInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static void SetStaticFieldValue(Type type, string name, object val)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            FieldInfo fieldInfo = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (fieldInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} field in {1} object.", name, type.FullName));

            SetStaticFieldValue(null, fieldInfo, val);
        }

        public static void SetStaticFieldValue(Type type, FieldInfo fieldInfo, object val)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(fieldInfo, "FieldInfo");

            //ChoValidation.Validate(fieldInfo as MemberInfo, val);

            try
            {
#if _DYNAMIC_
                Action<object, object> setter;
                if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                {
                    lock (_padLock)
                    {
                        if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                            _setterCache.Add(fieldInfo.FieldHandle.Value, setter = fieldInfo.CreateSetMethod());
                    }
                }
                setter(null, val);
#else
                fieldInfo.SetValue(null, val);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, fieldInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        #endregion Get & Set Static Field Value methods

        #region Get & Set Property Value methods

        public static void ConvertNSetPropertyValue(object target, string name, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == (PropertyInfo)null)
                throw new ApplicationException(string.Format("Can't find {0} property in {1} object.", (object)name, (object)target.GetType().FullName));
            ConvertNSetPropertyValue(target, property, val, culture);
        }

        public static void ConvertNSetPropertyValue(object target, PropertyInfo propertyInfo, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull((object)propertyInfo, "PropertyInfo");
            if ((val == null || (val is string && ((string)val).IsEmpty())) && propertyInfo.PropertyType.IsValueType)
            {
                if (propertyInfo.PropertyType.IsNullableType())
                    val = null;
                else
                    val = propertyInfo.PropertyType.Default();
            }

            if (target is Type)
            {
                ConvertNSetStaticPropertyValue(target as Type, propertyInfo, val, culture);
            }
            else
            {
                try
                {
                    val = ChoUtility.ConvertValueToObjectPropertyType(target, propertyInfo, val, culture);
#if _DYNAMIC_
                Action<object, object> setter;
                    var mi = propertyInfo.GetSetMethod();
                    if (mi != null)
                    {
                        var key = mi.MethodHandle.Value;
                        if (!_setterCache.TryGetValue(key, out setter))
                        {
                            lock (_padLock)
                            {
                                if (!_setterCache.TryGetValue(key, out setter))
                                    _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                            }
                        }
                        setter(target, val);
                    }
#else
                    propertyInfo.SetValue(target, val, null);
#endif
                }
                catch (TargetInvocationException ex)
                {
                    throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, propertyInfo.Name), ex.InnerException);
                    //throw ex.InnerException;
                }
            }
        }
        public static object GetPropertyValue(object target, string name)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (target is Type) return GetStaticPropertyValue(target as Type, name);

            PropertyInfo propertyInfo = target.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (propertyInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} property in {1} object.", name, target.GetType().FullName));

            return GetPropertyValue(target, propertyInfo);
        }

        public static object GetPropertyValue(object target, PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull(propertyInfo, "PropertyInfo");

            if (target is Type) return GetStaticPropertyValue(target as Type, propertyInfo);

            try
            {
#if _DYNAMIC_
                Func<object, object> getter;
                var key = propertyInfo.GetGetMethod().MethodHandle.Value;
                if (!_getterCache.TryGetValue(key, out getter))
                {
                    lock (_padLock)
                    {
                        if (!_getterCache.TryGetValue(key, out getter))
                            _getterCache.Add(key, getter = propertyInfo.CreateGetMethod());
                    }
                }
                return getter(target);
#else
                return propertyInfo.GetValue(target, new object[] { });
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, propertyInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static void SetPropertyValue(object target, string name, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            PropertyInfo propertyInfo = target.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (propertyInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} property in {1} object.", name, target.GetType().FullName));

            SetPropertyValue(target, propertyInfo, val);
        }

        public static void SetPropertyValue(object target, PropertyInfo propertyInfo, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull(propertyInfo, "PropertyInfo");

            if ((val == null || (val is string && ((string)val).IsEmpty())) && propertyInfo.PropertyType.IsValueType)
            {
                if (propertyInfo.PropertyType.IsNullableType())
                    val = null;
                else
                    val = propertyInfo.PropertyType.Default();
            }

            if (target is Type)
            {
                SetStaticPropertyValue(target as Type, propertyInfo, val);
                return;
            }

            //ChoValidation.Validate(propertyInfo as MemberInfo, val);

            try
            {
#if _DYNAMIC_
                Action<object, object> setter;
                var mi = propertyInfo.GetSetMethod();
                if (mi != null)
                {
                    var key = mi.MethodHandle.Value;
                    if (!_setterCache.TryGetValue(key, out setter))
                    {
                        lock (_padLock)
                        {
                            if (!_setterCache.TryGetValue(key, out setter))
                                _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                        }
                    }
                    setter(target, val);
                }
#else
                propertyInfo.SetValue(target, val, null);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, propertyInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static object GetPropertyValue(object target, string name, object[] index)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            ChoGuard.ArgumentNotNull(index, "Index");

            try
            {
                PropertyInfo propertyInfo = target.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new ApplicationException(String.Format("Can't find {0} property in {1} object.", name, target.GetType().FullName));

                return propertyInfo.GetValue(target, index);
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static void SetPropertyValue(object target, string name, object[] index, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            ChoGuard.ArgumentNotNull(index, "Index");

            try
            {
                PropertyInfo propertyInfo = target.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (propertyInfo == null)
                    throw new ApplicationException(String.Format("Can't find {0} property in {1} object.", name, target.GetType().FullName));

                propertyInfo.SetValue(target, val, index);
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        #endregion Get & Set Property Value methods

        #region Get & Set Static Property Value methods

        public static object GetStaticPropertyValue(Type type, string name)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            PropertyInfo propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            return GetStaticPropertyValue(type, propertyInfo);
        }

        public static object GetStaticPropertyValue(Type type, PropertyInfo propertyInfo)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propertyInfo, "PropertyInfo");

            try
            {
#if _DYNAMIC_
                Func<object, object> getter;
                var key = propertyInfo.GetGetMethod().MethodHandle.Value;
                if (!_getterCache.TryGetValue(key, out getter))
                {
                    lock (_padLock)
                    {
                        if (!_getterCache.TryGetValue(key, out getter))
                            _getterCache.Add(key, getter = propertyInfo.CreateGetMethod());
                    }
                }
                return getter(null);
#else
                return propertyInfo.GetValue(null, new object[] { });
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, propertyInfo.Name), ex.InnerException);
                throw ex.InnerException;
            }
        }

        public static void SetStaticPropertyValue(Type type, string name, object val)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            PropertyInfo propertyInfo = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (propertyInfo == null)
                throw new ApplicationException(String.Format("Can't find {0} property in {1} object.", name, type.FullName));

            SetStaticPropertyValue(type, propertyInfo, val);
        }

        public static void SetStaticPropertyValue(Type type, PropertyInfo propertyInfo, object val)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propertyInfo, "PropertyInfo");

            //ChoValidation.Validate(propertyInfo as MemberInfo, val);

            try
            {
#if _DYNAMIC_
                Action<object, object> setter;
                var mi = propertyInfo.GetSetMethod();
                if (mi != null)
                {
                    var key = mi.MethodHandle.Value;
                    if (!_setterCache.TryGetValue(key, out setter))
                    {
                        lock (_padLock)
                        {
                            if (!_setterCache.TryGetValue(key, out setter))
                                _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                        }
                    }
                    setter(null, val);
                }
#else
                propertyInfo.SetValue(null, val, null);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", type.FullName, propertyInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        #endregion Get & Set Static Property Value methods

        #region Get & Set Member Value methods

        public static object GetMemberValue(Type type, object target, string memberName)
        {
            //Call the object member, return the value
            if (ChoType.HasGetProperty(type, memberName))
            {
                if (target != null)
                    return ChoType.GetPropertyValue(target, memberName);
                else
                    return ChoType.GetStaticPropertyValue(type, memberName);
            }
            else if (ChoType.HasField(type, memberName))
            {
                if (target != null)
                    return ChoType.GetFieldValue(target, memberName);
                else
                    return ChoType.GetStaticFieldValue(type, memberName);
            }
            else
                throw new ApplicationException(String.Format("Can't find {0} member in {1} type.", memberName, target.GetType()));
        }

        public static object GetMemberValue(object target, string name)
        {
            if (target == null || name == null) return null;
            if (target is Type) return GetStaticMemberValue(target as Type, name);

            MemberInfo[] memberInfos = target.GetType().GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (memberInfos == null || memberInfos.Length == 0) return null;

            return GetMemberValue(target, memberInfos[0]);
        }


        public static bool IsValidObjectMember(MemberInfo memberInfo)
        {
            if ((memberInfo.MemberType != MemberTypes.Field
                && memberInfo.MemberType != MemberTypes.Property)
                || memberInfo.Name == "Item" //Indexer
                )
                return false;

            return true; // memberInfo.GetCustomAttributeEx<ChoIgnoreMemberAttribute>() == null;
        }

        public static object GetMemberValue(object target, MemberInfo memberInfo)
        {
            if (target == null || memberInfo == null) return null;
            if (target is Type) return GetStaticMemberValue(target as Type, memberInfo);

            MemberTypes memberType = memberInfo.MemberType;

            if (memberType == MemberTypes.Property)
                return GetPropertyValue(target, (PropertyInfo)memberInfo);
            else if (memberType == MemberTypes.Field)
                return GetFieldValue(target, (FieldInfo)memberInfo);
            else
                return null;
        }

        public static void SetMemberValue(object target, MemberInfo memberInfo, object value)
        {
            if (target == null || memberInfo == null)
                return;

            if (target is Type)
            {
                SetStaticMemberValue(target as Type, memberInfo, value);
                return;
            }

            MemberTypes memberType = memberInfo.MemberType;

            if (memberType == MemberTypes.Property)
                SetPropertyValue(target, (PropertyInfo)memberInfo, value);
            else if (memberType == MemberTypes.Field)
                SetFieldValue(target, (FieldInfo)memberInfo, value);
        }

        public static void SetMemberValue(object target, string name, object val)
        {
            if (target == null || name == null) return;
            if (target is Type)
            {
                SetStaticMemberValue(target as Type, name, val);
                return;
            }

            MemberInfo[] memberInfos = target.GetType().GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (memberInfos == null || memberInfos.Length == 0) return;

            MemberTypes memberType = memberInfos[0].MemberType;

            if (memberType == MemberTypes.Property)
                SetPropertyValue(target, name, val);
            else if (memberType == MemberTypes.Field)
                SetFieldValue(target, name, val);
        }

        public static void ConvertNSetMemberValue(object target, MemberInfo memberInfo, object value, CultureInfo culture = null)
        {
            if (target == null || memberInfo == (MemberInfo)null)
                return;
            if (target is Type)
            {
                ConvertNSetStaticMemberValue(target as Type, memberInfo, value, culture);
            }
            else
            {
                switch (memberInfo.MemberType)
                {
                    case MemberTypes.Property:
                        ConvertNSetPropertyValue(target, (PropertyInfo)memberInfo, value, culture);
                        break;
                    case MemberTypes.Field:
                        ConvertNSetFieldValue(target, (FieldInfo)memberInfo, value, culture);
                        break;
                }
            }
        }

        public static void ConvertNSetMemberValue(object target, string name, object val, CultureInfo culture = null)
        {
            if (target == null || name == null)
                return;
            if (target is Type)
            {
                ConvertNSetStaticMemberValue(target as Type, name, val, culture);
            }
            else
            {
                MemberInfo[] member = target.GetType().GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (member == null || member.Length == 0)
                    return;
                switch (member[0].MemberType)
                {
                    case MemberTypes.Property:
                        ConvertNSetPropertyValue(target, name, val, culture);
                        break;
                    case MemberTypes.Field:
                        ConvertNSetFieldValue(target, name, val, culture);
                        break;
                }
            }
        }

        #endregion Get & Set Member Value methods

        #region Get & Set Static Member Value methods

        public static void ConvertNSetStaticFieldValue(Type type, string name, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull((object)type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == (FieldInfo)null)
                throw new ApplicationException(string.Format("Can't find {0} field in {1} object.", (object)name, (object)type.FullName));
            ConvertNSetStaticFieldValue((Type)null, field, val, culture);
        }

        public static void ConvertNSetStaticFieldValue(Type type, FieldInfo fieldInfo, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull((object)type, "Type");
            ChoGuard.ArgumentNotNull((object)fieldInfo, "FieldInfo");
            try
            {
                val = ChoUtility.ConvertValueToObjectMemberType((object)type, (MemberInfo)fieldInfo, val, culture);

#if _DYNAMIC_
                Action<object, object> setter;
                if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                {
                    lock (_padLock)
                    {
                        if (!_setterCache.TryGetValue(fieldInfo.FieldHandle.Value, out setter))
                            _setterCache.Add(fieldInfo.FieldHandle.Value, setter = fieldInfo.CreateSetMethod());
                    }
                }
                setter(null, val);
#else
                fieldInfo.SetValue(null, val);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Type: {0}, Member: {1}]:", type.FullName, fieldInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static void ConvertNSetStaticMemberValue(Type type, MemberInfo memberInfo, object val, CultureInfo culture = null)
        {
            if (type == (Type)null || memberInfo == (MemberInfo)null)
                return;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                    ConvertNSetStaticPropertyValue(type, (PropertyInfo)memberInfo, val, culture);
                    break;
                case MemberTypes.Field:
                    ConvertNSetStaticFieldValue(type, (FieldInfo)memberInfo, val, culture);
                    break;
            }
        }

        public static void ConvertNSetStaticMemberValue(Type type, string name, object val, CultureInfo culture = null)
        {
            if (type == (Type)null || name == null)
                return;
            MemberInfo[] member = type.GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (member == null || member.Length == 0)
                return;
            switch (member[0].MemberType)
            {
                case MemberTypes.Property:
                    ConvertNSetStaticPropertyValue(type, name, val, culture);
                    break;
                case MemberTypes.Field:
                    ConvertNSetStaticFieldValue(type, name, val, culture);
                    break;
            }
        }

        public static void ConvertNSetStaticPropertyValue(Type type, string name, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull((object)type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == (PropertyInfo)null)
                throw new ApplicationException(string.Format("Can't find {0} property in {1} object.", (object)name, (object)type.FullName));
            ConvertNSetStaticPropertyValue(type, property, val, culture);
        }

        public static void ConvertNSetStaticPropertyValue(Type type, PropertyInfo propertyInfo, object val, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull((object)type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty((object)propertyInfo, "PropertyInfo");
            try
            {
                val = ChoUtility.ConvertValueToObjectMemberType((object)type, (MemberInfo)propertyInfo, val, culture);

#if _DYNAMIC_
                Action<object, object> setter;
                var mi = propertyInfo.GetSetMethod();
                if (mi != null)
                {
                    var key = mi.MethodHandle.Value;
                    if (!_setterCache.TryGetValue(key, out setter))
                    {
                        lock (_padLock)
                        {
                            if (!_setterCache.TryGetValue(key, out setter))
                                _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                        }
                    }
                    setter(null, val);
                }
#else
                propertyInfo.SetValue(null, val, null);
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", type.FullName, propertyInfo.Name), ex.InnerException);
                //throw ex.InnerException;
            }
        }

        public static object GetStaticMemberValue(Type type, string name)
        {
            if (type == null || name == null) return null;

            MemberInfo[] memberInfos = type.GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (memberInfos == null || memberInfos.Length == 0) return null;

            return GetStaticMemberValue(type, memberInfos[0]);
        }

        public static object GetStaticMemberValue(Type type, MemberInfo memberInfo)
        {
            if (type == null || memberInfo == null) return null;

            MemberTypes memberType = memberInfo.MemberType;

            if (memberType == MemberTypes.Property)
                return GetStaticPropertyValue(type, (PropertyInfo)memberInfo);
            else if (memberType == MemberTypes.Field)
                return GetStaticFieldValue(type, (FieldInfo)memberInfo);
            else
                return null;
        }

        public static void SetStaticMemberValue(Type type, MemberInfo memberInfo, object val)
        {
            if (type == null || memberInfo == null)
                return;

            MemberTypes memberType = memberInfo.MemberType;

            if (memberType == MemberTypes.Property)
                SetStaticPropertyValue(type, (PropertyInfo)memberInfo, val);
            else if (memberType == MemberTypes.Field)
                SetStaticFieldValue(type, (FieldInfo)memberInfo, val);
        }

        public static void SetStaticMemberValue(Type type, string name, object val)
        {
            if (type == null || name == null) return;

            MemberInfo[] memberInfos = type.GetMember(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (memberInfos == null || memberInfos.Length == 0) return;

            MemberTypes memberType = memberInfos[0].MemberType;

            if (memberType == MemberTypes.Property)
                SetStaticPropertyValue(type, name, val);
            else if (memberType == MemberTypes.Field)
                SetStaticFieldValue(type, name, val);
        }

        #endregion Get & Set Static Member Value methods

        #region GetMembers Overloads

        public static MemberInfo[] GetMembers(Type type)
        {
            if (type == null)
                throw new NullReferenceException("Missing Type.");

            TypeInfo typeInfo = GetTypeInfo(type);
            if (typeInfo.MemberInfos == null)
            {
                OrderedDictionary myMemberInfos = new OrderedDictionary();
                foreach (MemberInfo memberInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.NonPublic*/ | BindingFlags.Static))
                {
                    if (ChoType.GetAttribute<ChoIgnoreMemberAttribute>(memberInfo) != null)
                        continue;

                    if (myMemberInfos.Contains(memberInfo.Name))
                    {
                        if (memberInfo.DeclaringType.FullName == type.FullName)
                            myMemberInfos[memberInfo.Name] = memberInfo;
                        else
                            continue;
                    }
                    else
                        myMemberInfos.Add(memberInfo.Name, memberInfo);
                }
                foreach (MemberInfo memberInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.NonPublic */| BindingFlags.Static))
                {
                    if (ChoType.GetAttribute<ChoIgnoreMemberAttribute>(memberInfo) != null)
                        continue;

                    if (myMemberInfos.Contains(memberInfo.Name))
                    {
                        if (memberInfo.DeclaringType.FullName == type.FullName)
                            myMemberInfos[memberInfo.Name] = memberInfo;
                        else
                            continue;
                    }
                    else
                        myMemberInfos.Add(memberInfo.Name, memberInfo);
                }

                typeInfo.MemberInfos = new ArrayList(myMemberInfos.Values).ToArray(typeof(MemberInfo)) as MemberInfo[];
            }
            return typeInfo.MemberInfos;
        }

        #endregion GetMembers Overloads

        #region GetMemberType Overloads

        public static Type GetMemberType(Type targetType, string memberName)
        {
            foreach (MemberInfo memberInfo in ChoType.GetMembers(targetType))
            {
                if (String.Compare(memberInfo.Name, memberName, true) == 0) return GetMemberType(memberInfo);
            }
            return null;
        }

        public static Type GetMemberType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).FieldType;
            else if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).PropertyType;
            else
                throw new InvalidDataException("Invalid member info");
        }

        public static bool TryGetMemberType(MemberInfo memberInfo, out Type type)
        {
            type = null;
            if (memberInfo is FieldInfo)
                type = ((FieldInfo)memberInfo).FieldType;
            else if (memberInfo is PropertyInfo)
                type = ((PropertyInfo)memberInfo).PropertyType;
            else
                return false;

            return true;
        }

        #endregion GetMemberType Overloads

        #region GetMemberAttributes Overloads

        public static T[] GetMemberAttributes<T>(MemberInfo memberInfo) where T : Attribute
        {
            return (T[])GetMemberAttributes(memberInfo, typeof(T));
        }

        public static Attribute[] GetMemberAttributes(MemberInfo memberInfo, Type attributeType)
        {
            return GetMemberAttributes(memberInfo, attributeType, true);
        }

        public static T[] GetMemberAttributes<T>(MemberInfo memberInfo, bool inherit) where T : Attribute
        {
            return (T[])GetMemberAttributes(memberInfo, typeof(T), inherit);
        }

        public static Attribute[] GetMemberAttributes(MemberInfo memberInfo, Type attributeType, bool inherit)
        {
            if (memberInfo == null)
                throw new NullReferenceException("memberInfo");
            if (attributeType == null)
                throw new NullReferenceException("attributeType");

            return GetAttributes(memberInfo, attributeType, inherit) as Attribute[];
        }

        #endregion GetMemberAttributes Overloads

        #region GetMemberAttribute Overloads

        public static T GetMemberAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            return (T)GetMemberAttribute(memberInfo, typeof(T));
        }

        public static Attribute GetMemberAttribute(MemberInfo memberInfo, Type attributeType)
        {
            return GetMemberAttribute(memberInfo, attributeType, false);
        }

        public static T GetMemberAttribute<T>(MemberInfo memberInfo, bool inherit) where T : Attribute
        {
            return (T)GetMemberAttribute(memberInfo, typeof(T), inherit);
        }

        public static Attribute GetMemberAttribute(MemberInfo memberInfo, Type attributeType, bool inherit)
        {
            Attribute[] attributes = GetMemberAttributes(memberInfo, attributeType, inherit);
            if (attributes == null || attributes.Length == 0) return null;
            return attributes[0];
        }

        #endregion GetMemberAttributes Overloads

        #region HasMemberAttribute Overloads

        public static bool HasMemberAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            return GetMemberAttribute(memberInfo, typeof(T)) != null;
        }

        public static bool HasMemberAttribute(MemberInfo memberInfo, Type attributeType)
        {
            return GetMemberAttribute(memberInfo, attributeType, false) != null;
        }

        public static bool HasMemberAttribute<T>(MemberInfo memberInfo, bool inherit) where T : Attribute
        {
            return GetMemberAttribute(memberInfo, typeof(T), inherit) != null;
        }

        public static bool HasMemberAttribute(MemberInfo memberInfo, Type attributeType, bool inherit)
        {
            return GetMemberAttribute(memberInfo, attributeType, inherit) != null;
        }

        #endregion HasMemberAttribute Overloads

        #region GetMemberAttributesByBaseType Overloads

        public static T[] GetMemberAttributesByBaseType<T, baseT>(MemberInfo memberInfo)
            where T : Attribute
            where baseT : Type
        {
            return Array.ConvertAll<Attribute, T>(GetMemberAttributesByBaseType(memberInfo, typeof(T), typeof(baseT)),
                delegate(Attribute attribute) { return attribute as T; });
        }

        public static Attribute[] GetMemberAttributesByBaseType(MemberInfo memberInfo, Type attributeType, Type baseType)
        {
            if (memberInfo == null)
                throw new NullReferenceException("memberInfo");
            if (attributeType == null)
                throw new NullReferenceException("attributeType");
            if (baseType == null)
                throw new NullReferenceException("interfaceType");

            List<Attribute> attributes = new List<Attribute>();
            foreach (Attribute attribute in GetAttributes(memberInfo, attributeType, true))
            {
                if (baseType.IsAssignableFrom(attribute.GetType()))
                    attributes.Add(attribute);
            }

            return attributes.ToArray();
        }

        public static T[] GetMemberAttributesByBaseType<T>(MemberInfo memberInfo)
            where T : Attribute
        {
            return Array.ConvertAll<Attribute, T>(GetMemberAttributesByBaseType(memberInfo, typeof(T)),
                delegate(Attribute attribute) { return attribute as T; });
        }

        public static Attribute[] GetMemberAttributesByBaseType(MemberInfo memberInfo, Type baseType)
        {
            if (memberInfo == null)
                throw new NullReferenceException("memberInfo");
            if (baseType == null)
                throw new NullReferenceException("interfaceType");

            List<Attribute> attributes = new List<Attribute>();
            foreach (Attribute attribute in GetAttributes(memberInfo, baseType, true))
            {
                if (baseType.IsAssignableFrom(attribute.GetType()))
                    attributes.Add(attribute);
            }

            return attributes.ToArray();
        }

        #endregion GetMemberAttributesByBaseType Overloads

        #region GetMemberAttributeByBaseType Overloads

        public static T GetMemberAttributeByBaseType<T, baseT>(MemberInfo memberInfo)
            where T : Attribute
            where baseT : Type
        {
            return (T)GetMemberAttributeByBaseType(memberInfo, typeof(T), typeof(baseT));
        }

        public static Attribute GetMemberAttributeByBaseType(MemberInfo memberInfo, Type attributeType, Type baseType)
        {
            Attribute[] attributes = GetMemberAttributesByBaseType(memberInfo, attributeType, baseType);
            if (attributes == null || attributes.Length == 0) return null;
            return attributes[0];
        }

        public static T GetMemberAttributeByBaseType<T>(MemberInfo memberInfo)
            where T : Attribute
        {
            return (T)GetMemberAttributeByBaseType(memberInfo, typeof(T));
        }

        public static Attribute GetMemberAttributeByBaseType(MemberInfo memberInfo, Type baseType)
        {
            Attribute[] attributes = GetMemberAttributesByBaseType(memberInfo, baseType);
            if (attributes == null || attributes.Length == 0) return null;
            return attributes[0];
        }

        #endregion GetMemberAttributeByBaseType Overloads

        #region GetMembers Overloads

        public static Dictionary<string, MemberInfo> GetMembersDictionary(Type type, Type attributeType)
        {
            Dictionary<string, MemberInfo> memberInfos = new Dictionary<string, MemberInfo>();
            foreach (MemberInfo memberInfo in GetMemberInfos(type, attributeType))
                memberInfos.Add(memberInfo.Name, memberInfo);

            return memberInfos;
        }

        public static MemberInfo[] GetMemberInfos(Type type, Type attributeType)
        {
            if (type == null)
                throw new NullReferenceException("type");
            if (attributeType == null)
                throw new NullReferenceException("attributeType");

            if (_typeMembersDictionaryCache.ContainsKey(type)
                && _typeMembersDictionaryCache[type] != null
                && _typeMembersDictionaryCache[type].ContainsKey(attributeType))
                return _typeMembersDictionaryCache[type][attributeType];

            lock (_typeMembersDictionaryCacheLockObject)
            {
                if (_typeMembersDictionaryCache.ContainsKey(type)
                    && _typeMembersDictionaryCache[type] != null
                    && _typeMembersDictionaryCache[type].ContainsKey(attributeType))
                    return _typeMembersDictionaryCache[type][attributeType];

                if (!_typeMembersDictionaryCache.ContainsKey(type) || _typeMembersDictionaryCache[type] == null)
                {
                    _typeMembersDictionaryCache[type] = new Dictionary<Type, MemberInfo[]>();
                    _typeMembersDictionaryCache[type].Add(attributeType, null);
                }

                OrderedDictionary myMemberInfos = new OrderedDictionary();
                foreach (MemberInfo memberInfo in GetMembers(type))
                {
                    if (!(memberInfo is PropertyInfo)
                        && !(memberInfo is FieldInfo)
                        && !(memberInfo is MethodInfo)
                        && !(memberInfo is ConstructorInfo))
                        continue;

                    object memberAttribute = ChoType.GetMemberAttribute(memberInfo, attributeType);
                    if (memberAttribute == null)
                        continue;
                    myMemberInfos.Add(memberInfo.Name, memberInfo);
                }

                _typeMembersDictionaryCache[type][attributeType] = new ArrayList(myMemberInfos.Values).ToArray(typeof(MemberInfo)) as MemberInfo[];

                return _typeMembersDictionaryCache[type][attributeType];
            }
        }

        #endregion GetMembers Overloads

        #region GetMethod Overloads

        public static MethodInfo GetMethod(Type type, Type attributeType)
        {
            return GetMethod(type, attributeType, false);
        }

        public static MethodInfo GetMethod(Type type, Type attributeType, bool includeStaticMethods)
        {
            MethodInfo[] methodInfos = GetMethods(type, attributeType, includeStaticMethods);
            return methodInfos == null || methodInfos.Length == 0 ? null : methodInfos[0];
        }

        #endregion GetMethod Overloads

        #region GetMethods Overloads

        public static MethodInfo[] GetMethods(Type type, Type attributeType)
        {
            return GetMethods(type, attributeType, false);
        }

        public static MethodInfo[] GetMethods(Type type, Type attributeType, bool includeStaticMethods)
        {
            if (type == null)
                throw new NullReferenceException("type");
            if (attributeType == null)
                throw new NullReferenceException("attributeType");

            OrderedDictionary myMemberInfos = new OrderedDictionary();
            foreach (MemberInfo memberInfo in GetMembers(type))
            {
                if (!(memberInfo is MethodInfo))
                    continue;

                object memberAttribute = ChoType.GetMemberAttribute(memberInfo, attributeType);
                if (memberAttribute == null) continue;
                myMemberInfos.Add(memberInfo.Name, memberInfo);
            }

            return new ArrayList(myMemberInfos.Values).ToArray(typeof(MethodInfo)) as MethodInfo[];
        }

        #endregion GetMethods Overloads

        #region GetMember Overloads

        public static MemberInfo GetMemberInfo(Type type, Type attributeType)
        {
            MemberInfo[] memberInfos = GetMemberInfos(type, attributeType);
            return memberInfos == null || memberInfos.Length == 0 ? null : memberInfos[0];
        }

        public static MemberInfo GetMemberInfo(Type type, string memberName)
        {
            if (type == null)
                throw new NullReferenceException("type");
            if (memberName == null)
                throw new NullReferenceException("memberName");

            foreach (MemberInfo memberInfo in GetMembers(type))
            {
                if (String.Compare(memberInfo.Name, memberName, true) == 0) return memberInfo;
            }
            return null;
        }

        #endregion GetMember Overloads

        #region GetFields Overloads

        public static FieldInfo[] GetFields(Type type)
        {
            if (type == null)
                throw new NullReferenceException("Missing Type.");

            TypeInfo typeInfo = GetTypeInfo(type);
            if (typeInfo.FieldInfos == null)
            {
                OrderedDictionary myFieldInfos = new OrderedDictionary();
                foreach (FieldInfo fieldInfo in type.GetFields())
                {
                    if (myFieldInfos.Contains(fieldInfo.Name))
                    {
                        if (fieldInfo.DeclaringType.FullName == type.FullName)
                            myFieldInfos[fieldInfo.Name] = fieldInfo;
                        else
                            continue;
                    }
                    else
                        myFieldInfos.Add(fieldInfo.Name, fieldInfo);
                }

                typeInfo.FieldInfos = new ArrayList(myFieldInfos.Values).ToArray(typeof(FieldInfo)) as FieldInfo[];
            }
            return typeInfo.FieldInfos;
        }

        #endregion GetFields Overloads

        #region GetProperties Overloads

        public static PropertyInfo[] GetProperties(Type type)
        {
            if (type == null)
                throw new NullReferenceException("Missing Type.");

            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.NonPublic*/ | BindingFlags.Static).Where(mi => mi.GetCustomAttribute<ChoIgnoreMemberAttribute>() == null).ToArray();
            //return type.GetProperties(BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.NonPublic*/ | BindingFlags.Static).Where(mi => ChoType.GetAttribute<ChoIgnoreMemberAttribute>(mi) == null).ToArray();

            //TypeInfo typeInfo = GetTypeInfo(type);
            //if (typeInfo.PropertyInfos == null)
            //{
            //    OrderedDictionary myPropertyInfos = new OrderedDictionary();
            //    foreach (PropertyInfo propertyInfo in type.GetProperties())
            //    {
            //        if (myPropertyInfos.Contains(propertyInfo.Name))
            //        {
            //            if (propertyInfo.DeclaringType.FullName == type.FullName)
            //                myPropertyInfos[propertyInfo.Name] = propertyInfo;
            //            else
            //                continue;
            //        }
            //        else
            //            myPropertyInfos.Add(propertyInfo.Name, propertyInfo);
            //    }

            //    typeInfo.PropertyInfos = new ArrayList(myPropertyInfos.Values).ToArray(typeof(PropertyInfo)) as PropertyInfo[];
            //}
            //return typeInfo.PropertyInfos;
        }

        #endregion GetProperties Overloads

        //#region HasAttribute Overloads

        //public static bool HasAttribute<T>(Type type) where T : Attribute
        //{
        //    return HasAttribute(type, typeof(T));
        //}

        //public static bool HasAttribute(Type type, Type attributeType)
        //{
        //    return GetAttribute(type, attributeType) != null;
        //}

        //public static bool HasAttribute(Type type, string memberName, Type attributeType)
        //{
        //    return ChoType.GetMemberAttribute(ChoType.GetMemberInfo(type, memberName), attributeType) != null;
        //}

        //#endregion HasAttribute Overloads

        //#region GetAttribute Overloads

        //public static T GetAttribute<T>(Type type) where T : Attribute
        //{
        //    if (type == null)
        //        throw new NullReferenceException("type");

        //    return type.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        //    //foreach (Attribute attribute in GetCustomAttributes(type, true))
        //    //{
        //    //    if (typeof(T).IsAssignableFrom(attribute.GetType()))
        //    //        return (T)attribute;
        //    //}

        //    //return null;
        //}

        //public static Attribute GetAttribute(Type type, Type attributeType)
        //{
        //    if (type == null)
        //        throw new NullReferenceException("type");
        //    if (attributeType == null)
        //        throw new NullReferenceException("interfaceType");

        //    return type.GetCustomAttributes(attributeType, false).FirstOrDefault() as Attribute;

        //    //foreach (Attribute attribute in GetCustomAttributes(type, true))
        //    //{
        //    //    if (attributeType.IsAssignableFrom(attribute.GetType()))
        //    //        return attribute;
        //    //}

        //    //return null;
        //}

        //public static T[] GetAttributes<T>(Type type) where T : Attribute
        //{
        //    if (type == null)
        //        throw new NullReferenceException("type");

        //    return type.GetCustomAttributes(typeof(T), false).Cast<T>().ToArray();

        //    //List<T> attributes = new List<T>();
        //    //foreach (Attribute attribute in GetCustomAttributes(type, true))
        //    //{
        //    //    if (typeof(T).IsAssignableFrom(attribute.GetType()))
        //    //        attributes.Add((T)attribute);
        //    //}

        //    //return attributes.ToArray();
        //}

        //public static Attribute[] GetAttributes(Type type, Type attributeType)
        //{
        //    if (type == null)
        //        throw new NullReferenceException("type");
        //    if (attributeType == null)
        //        throw new NullReferenceException("interfaceType");

        //    return type.GetCustomAttributes(attributeType, false).Cast<Attribute>().ToArray();

        //    //List<Attribute> attributes = new List<Attribute>();
        //    //foreach (Attribute attribute in GetCustomAttributes(type, true))
        //    //{
        //    //    if (attributeType.IsAssignableFrom(attribute.GetType()))
        //    //        attributes.Add(attribute);
        //    //}

        //    //return attributes.ToArray();
        //}

        //#endregion GetAttribute Overloads

        #region SetCustomAttributes Overloads

        public static void SetCustomAttribute(Type type, Attribute attribute)
        {
            SetCustomAttributes(type, new Attribute[] { attribute });
        }

        public static void SetCustomAttributes(Type type, Attribute[] attributes)
        {
            lock (_typeAttributesCacheLockObject)
            {
                if (!_typeAttributesCache.ContainsKey(type))
                    _typeAttributesCache[type] = attributes;
                else
                    _typeAttributesCache[type] = ChoArray.Combine<Attribute>(_typeAttributesCache[type], attributes);
            }
        }

        public static void SetCustomAttribute(MemberInfo memberInfo, Attribute attribute)
        {
            SetCustomAttributes(memberInfo, new Attribute[] { attribute });
        }

        public static void SetCustomAttributes(MemberInfo memberInfo, Attribute[] attributes)
        {
            lock (_typeMemberAttributesCacheLockObject)
            {
                if (!_typeMemberAttributesCache.ContainsKey(memberInfo))
                {
                    _typeMemberAttributesCache[memberInfo] = new Dictionary<Type, List<Attribute>>();
                    _typeMemberAllAttributesCache[memberInfo] = new List<Attribute>();
                }

                List<Attribute> allAttributesList = _typeMemberAllAttributesCache[memberInfo];
                Dictionary<Type, List<Attribute>> attributeDictionary = _typeMemberAttributesCache[memberInfo];
                foreach (Attribute attribute in attributes)
                {
                    if (!attributeDictionary.ContainsKey(attribute.GetType()))
                        attributeDictionary[attribute.GetType()] = new List<Attribute>();

                    attributeDictionary[attribute.GetType()].Add(attribute);
                    allAttributesList.Add(attribute);
                }
            }
        }

        #endregion SetCustomAttributes Overloads

        #region GetCustomAttributes Overloads

        public static Attribute[] GetCustomAttributes(Type type, bool inherit)
        {
            lock (_typeAttributesCacheLockObject)
            {
                if (!_typeAttributesCache.ContainsKey(type))
                {
                    if (type.GetCustomAttribute<MetadataTypeAttribute>() == null)
                        SetCustomAttributes(type, ChoArray.ConvertTo<Attribute>(type.GetCustomAttributes(inherit)));
                    else
                        SetCustomAttributes(type, ChoArray.ConvertTo<Attribute>(type.GetCustomAttribute<MetadataTypeAttribute>().MetadataClassType.GetCustomAttributes(inherit)));
                }

                return _typeAttributesCache[type];
            }
        }

        #endregion GetCustomAttributes Overloads

        #region GetCustomAttributes (MemberInfo) Overloads

        public static T GetAttribute<T>(Type type) 
            where T:Attribute
        {
            return ChoTypeDescriptor.GetTypeAttribute<T>(type);
        }

        public static Attribute GetAttribute(Type type, Type attributeType)
        {
            return ChoTypeDescriptor.GetTypeAttribute(type, attributeType);
        }

        public static Attribute GetAttribute(MemberInfo memberInfo, bool inherit)
        {
            Attribute[] attributes = GetAttributes(memberInfo, null, inherit);
            return attributes == null || attributes.Length == 0 ? null : attributes[0];
        }

        public static Attribute[] GetAttributes(MemberInfo memberInfo, bool inherit)
        {
            return GetAttributes(memberInfo, null, inherit);
        }

        public static T GetAttribute<T>(MemberInfo memberInfo, bool inherit = false) where T : Attribute
        {
            return (T)memberInfo.GetCustomAttributeEx(typeof(T));
            //foreach (Attribute attribute in GetAttributes(memberInfo, typeof(T), inherit))
            //{
            //    if (typeof(T).IsAssignableFrom(attribute.GetType()))
            //        return (T)attribute;
            //}
        }

        public static T[] GetAttributes<T>(MemberInfo memberInfo, bool inherit = false) where T : Attribute
        {
            return (T[])memberInfo.GetCustomAttributesEx(typeof(T)).AsTypedEnumerable<T>().ToArray();

            //return (T[])GetAttributes(memberInfo, typeof(T), inherit);
        }

        public static Attribute[] GetAttributes(MemberInfo memberInfo, Type attributeType, bool inherit)
        {
            return memberInfo.GetCustomAttributesEx(attributeType).Where(a => inherit ? attributeType.IsAssignableFrom(a.GetType()) : attributeType == a.GetType()).ToArray();

            //if (Monitor.TryEnter(_typeMemberAttributesCacheLockObject, 1))
            //{
            //    try
            //    {
            //        if (!_typeMemberAttributesCache.ContainsKey(memberInfo))
            //            SetCustomAttributes(memberInfo, ChoArray.ConvertTo<Attribute>(memberInfo.GetCustomAttributes(inherit)));

            //        if (attributeType == null)
            //            return _typeMemberAllAttributesCache[memberInfo].ToArray();
            //        else
            //            return _typeMemberAttributesCache[memberInfo].ContainsKey(attributeType) ? _typeMemberAttributesCache[memberInfo][attributeType].ToArray() : new Attribute[] { };
            //    }
            //    finally
            //    {
            //        Monitor.Exit(_typeMemberAttributesCacheLockObject);
            //    }
            //}
            //else if (attributeType != null)
            //    return ChoArray.ConvertTo<Attribute>(memberInfo.GetCustomAttributes(attributeType, inherit));
            //else
            //    return ChoArray.ConvertTo<Attribute>(memberInfo.GetCustomAttributes(inherit));
        }

        #endregion GetAttributes (MemberInfo) Overloads

        //#region Get/SetAttributeNameParameterValue Overloads (Public)

        //public static void SetAttributeNameParameterValue(Attribute attribute, string paramName, object paramValue)
        //{
        //    ChoType.SetMemberValue(attribute, paramName, paramValue);
        //}

        //public static void SetAttributeNameParameterValue(Type type, string memberName, Type attributeType, string paramName, object paramValue)
        //{
        //    ChoType.SetMemberValue(ChoType.GetMemberAttribute(ChoType.GetMemberInfo(type, memberName), attributeType), paramName, paramValue);
        //}

        //public static void SetAttributeNameParameterValue(Type type, Type attributeType, string paramName, object paramValue)
        //{
        //    ChoType.SetMemberValue(ChoType.GetAttribute(type, attributeType), paramName, paramValue);
        //}

        //public static object GetAttributeNameParameterValue(Attribute attribute, string paramName)
        //{
        //    return ChoType.GetMemberValue(attribute, paramName);
        //}

        //public static object GetAttributeNameParameterValue(Type type, string memberName, Type attributeType, string paramName)
        //{
        //    return ChoType.GetMemberValue(ChoType.GetMemberAttribute(ChoType.GetMemberInfo(type, memberName), attributeType), paramName);
        //}

        //public static object GetAttributeNameParameterValue(Type type, Type attributeType, string paramName)
        //{
        //    return ChoType.GetMemberValue(ChoType.GetAttribute(type, attributeType), paramName);
        //}

        //#endregion Get/SetAttributeNameParameterValue Overloads (Public)

        #region Other helper members (Public)

        public static IEnumerable<MemberInfo> GetGetFieldsNProperties(this Type type)
        {
            return GetGetFieldsNProperties(type, BindingFlags.Public | BindingFlags.Instance);
        }

        private static readonly object _toStringMemberCacheSyncRoot = new object();
        private static readonly Dictionary<IntPtr, MemberInfo[]> _toStringMemberCache = new Dictionary<IntPtr, MemberInfo[]>();

        public static IEnumerable<MemberInfo> GetGetFieldsNProperties(this Type type, BindingFlags flags)
        {
            MemberInfo[] properties;
            if (!_toStringMemberCache.TryGetValue(type.TypeHandle.Value, out properties))
            {
                lock (_toStringMemberCacheSyncRoot)
                {
                    if (!_toStringMemberCache.TryGetValue(type.TypeHandle.Value, out properties))
                    {
                        properties = type.GetProperties(flags | BindingFlags.FlattenHierarchy)
                        .Where(p => p.GetGetMethod() != null && p.GetGetMethod().GetParameters().Length == 0)
                        .Cast<MemberInfo>()
                        .Union(type.GetFields(flags | BindingFlags.FlattenHierarchy).Cast<MemberInfo>()).ToArray();

                        _toStringMemberCache.Add(type.TypeHandle.Value, properties);
                    }
                }
            }

            return properties;
        }

        public static IEnumerable<MemberInfo> GetSetFieldsNProperties(this Type type, BindingFlags flags)
        {
            MemberInfo[] properties;
            if (!_toStringMemberCache.TryGetValue(type.TypeHandle.Value, out properties))
            {
                lock (_toStringMemberCacheSyncRoot)
                {
                    if (!_toStringMemberCache.TryGetValue(type.TypeHandle.Value, out properties))
                    {
                        properties = type.GetProperties(flags | BindingFlags.FlattenHierarchy)
                        .Where(p => p.GetSetMethod() != null)
                        .Cast<MemberInfo>()
                        .Union(type.GetFields(flags | BindingFlags.FlattenHierarchy).Cast<MemberInfo>()).ToArray();

                        _toStringMemberCache.Add(type.TypeHandle.Value, properties);
                    }
                }
            }

            return properties;
        }

        public static bool IsReadOnlyMember(Type type, string memberName)
        {
            MemberInfo mi = GetMemberInfo(type, memberName);
            return mi != null && IsReadOnlyMember(mi);
        }

        public static bool IsReadOnlyMember(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo && ((FieldInfo)memberInfo).IsInitOnly)
                return true;
            if (memberInfo is PropertyInfo && (((PropertyInfo)memberInfo).GetSetMethod(true) == null || ((PropertyInfo)memberInfo).GetSetMethod(true).IsPrivate))
                return true;

            return false;
        }

        public static Type[] ConvertToTypes(object[] objects)
        {
            List<Type> types = new List<Type>();
            if (objects != null)
            {
                foreach (object constructorArg in objects)
                    types.Add(constructorArg.GetType());
            }

            return types.ToArray();
        }

        public static string GetTypeName(object target)
        {
            if (target == null) return "UNKNOWN";
            if (target is Type)
                return _GetTypeName(target as Type);
            else
                return _GetTypeName(target.GetType());
        }

        private static string _GetTypeName(Type type)
        {
            if (type == typeof(int))
                return "int";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(bool))
                return "bool";
            else if (type == typeof(DateTime))
                return "datetime";
            else if (type == typeof(TimeSpan))
                return "timespan";
            else
                return type.FullName;
        }

        #endregion Other helper members (Public)

        public static Type[] ConvertToTypes(ParameterInfo[] parameterInfos)
        {
            List<Type> types = new List<Type>();
            if (parameterInfos != null)
            {
                foreach (ParameterInfo parameterInfo in parameterInfos)
                    types.Add(parameterInfo.ParameterType);
            }

            return types.ToArray();
        }

        #endregion Shared Members (Public)

        #region Shared Members (Private)

        private static TypeInfo GetTypeInfo(Type type)
        {
            lock (_typeInfos.SyncRoot)
            {
                if (!_typeInfos.Contains(type.FullName))
                    _typeInfos.Add(type.FullName, new TypeInfo());
            }

            return (TypeInfo)_typeInfos[type.FullName];
        }

        private static Type[] ConvertToTypesArray(object[] args)
        {
            if (args == null) return new Type[0];

            Type[] types = new Type[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    types[i] = typeof(object);
                }
                else
                {
                    types[i] = args[i].GetType();
                }
            }

            return types;
        }

        #endregion Shared Members (Private)

        public static object CreateInstance(Type type, object[] parameters)
        {
            Type[] types = ChoType.ConvertToTypes(parameters);
            //ConstructorInfo constructorInfo = type.GetConstructor(types);
            ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, types, null);
            if (constructorInfo == null)
                throw new ApplicationException(String.Format("Can't find a constructor of matching inputs [{0}] in '{1}' type.", ChoUtility.Join(types), type.FullName));

            return constructorInfo.Invoke(parameters);
        }

        public static T GetCustomAttribute<T>(Type type, bool inherit)
            where T: Attribute
        {
            return GetCustomAttributes(type, typeof(T), inherit).FirstOrDefault().CastTo<T>();
        }

        public static object GetCustomAttribute(Type type, Type attributeType, bool inherit)
        {
            return GetCustomAttributes(type, attributeType, inherit).FirstOrDefault();
        }

        public static object[] GetCustomAttributes(Type type, Type attributeType, bool inherit)
        {
            if (!inherit)
            {
                return type.GetCustomAttributes(attributeType, false);
            }

            var attributeCollection = new Collection<object>();
            var baseType = type;

            do
            {
                baseType.GetCustomAttributes(attributeType, true).Apply(attributeCollection.Add);
                baseType = baseType.BaseType;
            }
            while (baseType != null);

            foreach (var interfaceType in type.GetInterfaces())
            {
                GetCustomAttributes(interfaceType, attributeType, true).Apply(attributeCollection.Add);
            }

            var attributeArray = new object[attributeCollection.Count];
            attributeCollection.CopyTo(attributeArray, 0);
            return attributeArray;
        }

        public static Type GetNType(this object target)
        {
            if (target == null)
                return typeof(object);

            if (target is Type)
                return target as Type;
            else
                return target.GetType();
        }
        /// <summary>Applies a function to every element of the list.</summary>
        private static void Apply<T>(this IEnumerable<T> enumerable, Action<T> function)
        {
            foreach (var item in enumerable)
            {
                function.Invoke(item);
            }
        }

        #region GetTypes (By Attribute) Overloads

        public static IEnumerable<Type> GetTypesAssignableFrom(Type interfaceType)
        {
            var type = interfaceType;
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));
        }

        private static readonly Lazy<Type[]> _allTypes = new Lazy<Type[]>(() =>
        {
            List<Type> allTypes = new List<Type>();
            foreach (Assembly assembly in ChoAssembly.GetLoadedAssemblies())
            {
                allTypes.AddRange(assembly.GetTypes());
            }
            return allTypes.ToArray();
        }, false);

        public static Type[] GetAllTypes()
        {
            return _allTypes.Value;
        }

        public static Type[] GetTypes(Type attributeType)
        {
            if (attributeType == null)
                return new Type[] { };

            if (_attributeTypesCache.ContainsKey(attributeType))
                return _attributeTypesCache[attributeType];
            else
            {
                lock (_attributeTypesCacheLockObject)
                {
                    if (!_attributeTypesCache.ContainsKey(attributeType))
                    {
                        ArrayList types = new ArrayList();

                        foreach (Assembly assembly in ChoAssembly.GetLoadedAssemblies())
                        {
                            ExtractTypes(attributeType, types, assembly);
                        }

                        _attributeTypesCache.Add(attributeType, types.ToArray(typeof(Type)) as Type[]);
                    }
                    return _attributeTypesCache[attributeType];
                }
            }
        }
        private static void ExtractTypes(Type attributeType, ArrayList types, Assembly assembly)
        {
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type == null) continue;
                    object[] attributes = type.GetCustomAttributes(attributeType, false);
                    if (attributes == null || attributes.Length == 0) continue;
                    //fileProfile.AppendLine(type.FullName);
                    types.Add(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // do nothing 
            }
            catch (Exception ex)
            {
                ChoETLLog.Info(ex.ToString());
            }
        }
        #endregion GetTypes (By Attribute) Overloads

        #region GetDefaultValue Overloads

        public static object GetRawDefaultValue(MemberInfo mi)
        {
            ChoGuard.ArgumentNotNull(mi, "MemberInfo");
            DefaultValueAttribute defaultValueAttribute = ChoType.GetMemberAttribute<DefaultValueAttribute>(mi);
            if (defaultValueAttribute == null)
                return null;

            return defaultValueAttribute.Value;
        }

        public static object GetDefaultValue(MemberInfo mi)
        {
            ChoGuard.ArgumentNotNull(mi, "MemberInfo");
            DefaultValueAttribute defaultValueAttribute = ChoType.GetMemberAttribute<DefaultValueAttribute>(mi);
            if (defaultValueAttribute == null)
                return null;

            object defaultValue = defaultValueAttribute.Value;
            if (defaultValue is string)
                return ((string)defaultValue).ExpandProperties();
            else
                return defaultValue;
        }

        public static bool HasFallbackValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            return (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                    where typeof(ChoFallbackValueAttribute).IsAssignableFrom(a.GetType())
                    select a).Any();
        }

        public static object GetRawFallbackValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            ChoFallbackValueAttribute FallbackValueAttribute = (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                                                           where typeof(ChoFallbackValueAttribute).IsAssignableFrom(a.GetType())
                                                           select a).FirstOrDefault() as ChoFallbackValueAttribute;
            if (FallbackValueAttribute == null)
                return null;

            return FallbackValueAttribute.Value;
        }

        public static object GetFallbackValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            ChoFallbackValueAttribute FallbackValueAttribute = (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                                                           where typeof(ChoFallbackValueAttribute).IsAssignableFrom(a.GetType())
                                                           select a).FirstOrDefault() as ChoFallbackValueAttribute;
            if (FallbackValueAttribute == null)
                return null;

            object FallbackValue = FallbackValueAttribute.Value;
            if (FallbackValue is string)
                return ((string)FallbackValue).ExpandProperties();
            else
                return FallbackValue;
        }

        public static bool HasDefaultValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            return (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                    where typeof(DefaultValueAttribute).IsAssignableFrom(a.GetType())
                    select a).Any();
        }

        public static object GetRawDefaultValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            DefaultValueAttribute defaultValueAttribute = (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                                                           where typeof(DefaultValueAttribute).IsAssignableFrom(a.GetType())
                                                           select a).FirstOrDefault() as DefaultValueAttribute;
            if (defaultValueAttribute != null)
                return defaultValueAttribute.Value;
            ChoDefaultValueAttribute chodefaultValueAttribute = (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                                                           where typeof(ChoDefaultValueAttribute).IsAssignableFrom(a.GetType())
                                                           select a).FirstOrDefault() as ChoDefaultValueAttribute;

            if (chodefaultValueAttribute != null)
                return chodefaultValueAttribute.Value;

            return null;
        }

        public static object GetDefaultValue(PropertyDescriptor mi)
        {
            ChoGuard.ArgumentNotNull(mi, "PropertyDescriptor");
            DefaultValueAttribute defaultValueAttribute = (from a in mi.Attributes.AsTypedEnumerable<Attribute>()
                                                           where typeof(DefaultValueAttribute).IsAssignableFrom(a.GetType())
                                                           select a).FirstOrDefault() as DefaultValueAttribute;
            if (defaultValueAttribute == null)
                return null;

            object defaultValue = defaultValueAttribute.Value;
            if (defaultValue is string)
                return ((string)defaultValue).ExpandProperties();
            else
                return defaultValue;
        }

        #endregion GetDefaultValue Overloads

        #region GetMethodsBySig Overloads

        public static IEnumerable<MethodInfo> GetMethodsBySig(this Type type, Type returnType, params Type[] parameterTypes)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where((m) =>
            {
                if (m.ReturnType != returnType) return false;
                var parameters = m.GetParameters();
                if ((parameterTypes == null || parameterTypes.Length == 0))
                    return parameters.Length == 0;
                if (parameters.Length != parameterTypes.Length)
                    return false;
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        return false;
                }
                return true;
            });
        }

        #endregion

        #region IsOverridden Overloads

        public static bool IsOverrides(this Type type, MethodInfo baseMethod)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(baseMethod, "BaseMethod");
            return baseMethod.GetBaseDefinition().DeclaringType != baseMethod.DeclaringType;
        }

        public static bool IsOverrides(this Type type, string memberName, Type[] parameters = null)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(memberName, "MemberName");

            parameters = parameters ?? Type.EmptyTypes;
            return IsOverrides(type, type.GetMethod(memberName, parameters));
        }

        #endregion IsOverridden Overloads

        #region GetDeclaringMethod Overrides

        private static readonly object _objCachePadLock = new object();
        private static readonly Dictionary<Type, object> _objCache = new Dictionary<Type, object>();
        public static object GetMemberObjectMatchingType(string declaringMember, object rec, params object[] args)
        {
            if (declaringMember == null)
                return null;

            try
            {
                if (declaringMember.Contains("."))
                {
                    int index = declaringMember.IndexOf(".");
                    Type type = ChoType.GetType(declaringMember.Substring(0, index));
                    if (type == null)
                        return null;
                    else
                    {
                        lock (_objCachePadLock)
                        {
                            if (!_objCache.ContainsKey(type))
                            {
                                object o = null;
                                try
                                {
                                    o = ChoActivator.CreateInstance(type, null);
                                }
                                catch { }

                                _objCache.Add(type, o);
                            }

                            return _objCache[type];
                        }
                    }
                }
                else
                {
                    if (ChoType.HasSetProperty(rec.GetType(), declaringMember))
                    {
                        var mo = ChoType.GetPropertyValue(rec, declaringMember);
                        if (mo == null)
                        {
                            ChoType.SetPropertyValue(rec, declaringMember, ChoActivator.CreateInstance(ChoType.GetMemberType(rec.GetType(), declaringMember)));
                            mo = ChoType.GetPropertyValue(rec, declaringMember);
                        }
                        return mo;
                    }
                }
            }
            catch
            {

            }
            return null;
        }

        public static string GetFieldName(string declaringMember)
        {
            if (declaringMember == null)
                return null;

            if (declaringMember.Contains("."))
            {
                int lastIndex = declaringMember.LastIndexOf(".");
                return declaringMember.Substring(lastIndex + 1);
            }
            else
                return declaringMember;
        }

        public static object GetDeclaringRecord(string declaringMember, object rec, int? arrayIndex = null)
        {
            if (declaringMember == null)
                return rec;

            return GetDeclaringRecord(rec, declaringMember, arrayIndex);
        }

        private static object GetDeclaringRecord(object src, string propName, int? arrayIndex = null, bool leaf = true)
        {
            if (src == null) return null; // throw new ArgumentException("Value cannot be null.", "src");
            if (propName == null) throw new ArgumentException("Value cannot be null.", "propName");

            if (propName.Contains("."))//complex type nested
            {
                var temp = propName.Split(new char[] { '.' }, 2);
                return GetDeclaringRecord(GetDeclaringRecord(src, temp[0], arrayIndex, false), temp[1], arrayIndex);
            }
            else
            {
                var prop = src is IList ? src.GetType().GetItemType().GetProperty(propName) : src.GetType().GetProperty(propName);
                if (!leaf && prop != null)
                {
                    object obj = null;
                    if (src is IList)
                    {
                        if (arrayIndex != null)
                        {
                            obj = ((IList)src).OfType<object>().Skip(arrayIndex.Value).FirstOrDefault();
                            obj = prop.GetValue(obj, null);
                        }
                    }
                    else
                    {
                        obj = prop.GetValue(src, null);
                    }

                    if (obj == null)
                    {
                        if (typeof(Array).IsAssignableFrom(prop.PropertyType))
                        {
                            //obj = Array.CreateInstance(prop.PropertyType.GetItemType(), 2);
                        }
                        else
                        {
                            obj = ChoActivator.CreateInstance(prop.PropertyType);
                        }
                        prop.SetValue(src, obj);
                    }
                    return obj;
                }
                else
                    return src;
            }
        }

        #endregion GetDeclaringMethod Overrides


        public static Type ResolveType(this Type recordType)
        {
            if (!recordType.IsDynamicType() && typeof(ICollection).IsAssignableFrom(recordType))
                recordType = recordType.GetEnumerableItemType().GetUnderlyingType();
            else
                recordType = recordType.GetUnderlyingType();

            if (!recordType.IsDynamicType())
            {
                if (recordType.IsSimple())
                    recordType = typeof(ChoScalarObject<>).MakeGenericType(recordType);
            }

            return recordType;
        }

        public static bool HasParameterlessConstructor(this Type type)
        {
            //return type.GetTypeInfo().GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;
            return type.GetTypeInfo().GetConstructor(Type.EmptyTypes) != null;
        }

        /// <summary>
        ///     Determines whether the type is definitely unsupported for schema generation.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the type is unsupported; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUnsupported(this Type type)
        {
            return type == typeof(IntPtr)
                || type == typeof(UIntPtr)
                || type == typeof(object)
                || type.ContainsGenericParameters()
                || (!type.IsArray
                && !type.IsValueType()
                && !type.IsAnonymous()
                && !type.HasParameterlessConstructor()
                && type != typeof(string)
                && type != typeof(Uri)
                && !type.IsAbstract()
                && !type.IsInterface()
                && !(type.IsGenericType() && SupportedInterfaces.Contains(type.GetGenericTypeDefinition())));
        }

        /// <summary>
        /// The natively supported types.
        /// </summary>
        private static readonly HashSet<Type> NativelySupported = new HashSet<Type>
        {
            typeof(char),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(uint),
            typeof(int),
            typeof(bool),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(Uri),
            typeof(byte[]),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid)
        };

        public static bool IsNativelySupported(this Type type)
        {
            var notNullable = Nullable.GetUnderlyingType(type) ?? type;
            return NativelySupported.Contains(notNullable)
                || type.IsArray
                || type.IsKeyValuePair()
                || type.GetAllInterfaces()
                       .FirstOrDefault(t => t.IsGenericType() &&
                                            t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) != null;
        }

        private static readonly HashSet<Type> SupportedInterfaces = new HashSet<Type>
        {
            typeof(IList<>),
            typeof(IDictionary<,>)
        };

        public static bool IsAnonymous(this Type type)
        {
            return type.IsClass()
                && type.GetTypeInfo().GetCustomAttributes(false).Any(a => a is CompilerGeneratedAttribute)
                && !type.IsNested
                && type.Name.StartsWith("<>", StringComparison.Ordinal)
                && type.Name.Contains("__Anonymous");
        }

        public static PropertyInfo GetPropertyByName(
            this Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
        {
            return type.GetProperty(name, flags);
        }

        public static MethodInfo GetMethodByName(this Type type, string shortName, params Type[] arguments)
        {
            var result = type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(m => m.Name == shortName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments));

            if (result != null)
            {
                return result;
            }

            return
                type
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => (m.Name.EndsWith(shortName, StringComparison.Ordinal) ||
                                       m.Name.EndsWith("." + shortName, StringComparison.Ordinal))
                                 && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments));
        }

        /// <summary>
        /// Gets all fields of the type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>Collection of fields.</returns>
        public static IEnumerable<FieldInfo> GetAllFields(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<FieldInfo>();
            }

            const BindingFlags Flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;
            return t
                .GetFields(Flags)
                .Where(f => !f.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .Concat(GetAllFields(t.BaseType()));
        }

        /// <summary>
        /// Gets all properties of the type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>Collection of properties.</returns>
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<PropertyInfo>();
            }

            const BindingFlags Flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;

            return t
                .GetProperties(Flags)
                .Where(p => !p.IsDefined(typeof(CompilerGeneratedAttribute), false)
                            && p.GetIndexParameters().Length == 0)
                .Concat(GetAllProperties(t.BaseType()));
        }

        public static IEnumerable<Type> GetAllInterfaces(this Type t)
        {
            foreach (var i in t.GetInterfaces())
            {
                yield return i;
            }
        }

        public static string GetStrippedFullName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (string.IsNullOrEmpty(type.Namespace))
            {
                return StripAvroNonCompatibleCharacters(type.Name);
            }

            return StripAvroNonCompatibleCharacters(type.Namespace + "." + type.Name);
        }

        public static string StripAvroNonCompatibleCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(value, @"[^A-Za-z0-9_\.]", string.Empty, RegexOptions.None);
        }

        public static bool IsFlagEnum(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return type.GetTypeInfo().GetCustomAttributes(false).ToList().Find(a => a is FlagsAttribute) != null;
        }

        public static bool CanContainNull(this Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return !type.IsValueType() || underlyingType != null;
        }

        public static bool IsKeyValuePair(this Type type)
        {
            return type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }

        public static bool CanBeKnownTypeOf(this Type type, Type baseType)
        {
            return !type.IsAbstract()
                   && (type.GetTypeInfo().IsSubclassOf(baseType)
                   || type == baseType
                   || (baseType.IsInterface() && baseType.IsAssignableFrom(type))
                   || (baseType.IsGenericType() && baseType.IsInterface() && baseType.GenericIsAssignable(baseType)
                           && type.GetGenericArguments()
                                  .Zip(baseType.GetGenericArguments(), (type1, type2) => new Tuple<Type, Type>(type1, type2))
                                  .ToList()
                                  .TrueForAll(tuple => CanBeKnownTypeOf(tuple.Item1, tuple.Item2))));
        }

        private static bool GenericIsAssignable(this Type type, Type instanceType)
        {
            if (!type.IsGenericType() || !instanceType.IsGenericType())
            {
                return false;
            }

            var args = type.GetGenericArguments();
            return args.Any() && type.IsAssignableFrom(instanceType.GetGenericTypeDefinition().MakeGenericType(args));
        }

        public static IEnumerable<Type> GetAllKnownTypes(this Type t)
        {
            if (t == null)
            {
                return Enumerable.Empty<Type>();
            }

            return t.GetTypeInfo().GetCustomAttributes(true)
                .OfType<KnownTypeAttribute>()
                .Select(a => a.Type);
        }

        public static int ReadAllRequiredBytes(this Stream stream, byte[] buffer, int offset, int count)
        {
            int toRead = count;
            int currentOffset = offset;
            int currentRead;
            do
            {
                currentRead = stream.Read(buffer, currentOffset, toRead);
                currentOffset += currentRead;
                toRead -= currentRead;
            }
            while (toRead > 0 && currentRead != 0);
            return currentOffset - offset;
        }

        public static void CheckPropertyGetters(IEnumerable<PropertyInfo> properties)
        {
            var missingGetter = properties.FirstOrDefault(p => p.GetGetMethod(true) == null);
            if (missingGetter != null)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Property '{0}' of class '{1}' does not have a getter.", missingGetter.Name, missingGetter.DeclaringType.FullName));
            }
        }

        public static DataMemberAttribute GetDataMemberAttribute(this PropertyInfo property)
        {
            return property
                .GetCustomAttributes(false)
                .OfType<DataMemberAttribute>()
                .SingleOrDefault();
        }

        public static IList<PropertyInfo> RemoveDuplicates(IEnumerable<PropertyInfo> properties)
        {
            var result = new List<PropertyInfo>();
            foreach (var p in properties)
            {
                if (result.Find(s => s.Name == p.Name) == null)
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        public static Assembly Assembly(this Type type)
        {
            return type.GetTypeInfo().Assembly;
        }

        public static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        public static bool ContainsGenericParameters(this Type type)
        {
            return type.GetTypeInfo().ContainsGenericParameters;
        }

        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
    }
}
