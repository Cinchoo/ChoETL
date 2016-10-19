namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;

    #endregion NameSpaces

    public static class ChoTypeDescriptor
    {
        #region Constants

        private static readonly TypeConverter[] EmptyTypeConverters = new TypeConverter[] { };
        private static readonly object[] EmptyParams = new object[] { };

        #endregion Constants

        #region Shared Data Members (Private)

        private static readonly object _typeMemberTypeConverterCacheLockObject = new object();
        private static readonly Dictionary<MemberInfo, object[]> _typeMemberTypeConverterCache = new Dictionary<MemberInfo, object[]>();
        private static readonly Dictionary<Type, object[]> _typeTypeConverterCache = new Dictionary<Type, object[]>();

        private static readonly Dictionary<MemberInfo, object[]> _typeMemberTypeConverterParamsCache = new Dictionary<MemberInfo, object[]>();
        private static readonly Dictionary<Type, object[]> _typeTypeConverterParamsCache = new Dictionary<Type, object[]>();

        #endregion Shared Data Members (Private)

        #region Shared Members (Public)

        #region GetTypeConverters Overloads (Public)

        public static IEnumerable<PropertyDescriptor> GetProperties<T>(Type type)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<T>().Any());
        }

        public static PropertyDescriptor GetProperty<T>(Type type, string propName)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<T>().Any()).FirstOrDefault();
        }

        public static T GetPropetyAttribute<T>(PropertyDescriptor pd)
                   where T : Attribute
        {
            if (pd == null)
                return null;
            else
                return pd.Attributes.OfType<T>().First();
        }

        public static T GetPropetyAttribute<T>(Type type, string propName)
                   where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            PropertyDescriptor pd = TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd1 => pd1.Attributes.OfType<T>().Any()).FirstOrDefault();
            if (pd == null)
                return null;
            else
                return GetPropetyAttribute<T>(pd);
        }

        public static void RegisterConverters(MemberInfo memberInfo, TypeConverter[] typeConverters)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (typeConverters == null)
                    typeConverters = EmptyTypeConverters;

                if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    _typeMemberTypeConverterCache[memberInfo] = typeConverters;
                else
                    _typeMemberTypeConverterCache.Add(memberInfo, typeConverters);
            }
        }

        public static void RegisterConverters(Type type, TypeConverter[] typeConverters)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (typeConverters == null)
                    typeConverters = EmptyTypeConverters;

                if (_typeTypeConverterCache.ContainsKey(type))
                    _typeTypeConverterCache[type] = typeConverters;
                else
                    _typeTypeConverterCache.Add(type, typeConverters);
            }
        }

        public static void ClearConverters(MemberInfo memberInfo)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    _typeMemberTypeConverterCache.Remove(memberInfo);
            }
        }

        public static void ClearConverters(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            lock (_typeMemberTypeConverterCacheLockObject)
            {
                if (_typeTypeConverterCache.ContainsKey(type))
                    _typeTypeConverterCache.Remove(type);
            }
        }

        #endregion GetTypeConverters Overloads (Public)

        #region GetTypeConverters Overloads (internal)

        public static object[] GetTypeConverterParams(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return EmptyParams;

            Type memberType;
            if (ChoType.TryGetMemberType(memberInfo, out memberType) && (memberType == null /*|| memberType.IsSimple() */))
                return EmptyParams;

            if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
            {
                if (_typeMemberTypeConverterCache[memberInfo] == EmptyTypeConverters)
                {
                    if (_typeTypeConverterParamsCache.ContainsKey(memberType))
                        return _typeTypeConverterParamsCache[memberType];
                }

                //if (ChoObjectMemberMetaDataCache.Default.GetConverterParams(memberInfo) == null)
                //    return _typeMemberTypeConverterParamsCache[memberInfo];
                //else
                //    return ChoObjectMemberMetaDataCache.Default.GetConverterParams(memberInfo);
            }

            return EmptyParams;
        }

        public static object GetTypeConverter(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            object[] typeConverters = GetTypeConverters(memberInfo);
            if (typeConverters == null || typeConverters.Length == 0)
                return null;

            return typeConverters[0];
        }


        public static object[] GetTypeConverters(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return null;

            Type memberType;
            if (!ChoType.TryGetMemberType(memberInfo, out memberType) || (memberType == null /*|| memberType.IsSimple() */))
                return null;

            if (_typeMemberTypeConverterCache.ContainsKey(memberInfo))
            {
                if (_typeMemberTypeConverterCache[memberInfo] == EmptyTypeConverters)
                {
                    if (_typeTypeConverterCache.ContainsKey(memberType))
                        return _typeTypeConverterCache[memberType];
                }

                return _typeMemberTypeConverterCache[memberInfo];
            }
            else
            {
                lock (_typeMemberTypeConverterCacheLockObject)
                {
                    if (!_typeMemberTypeConverterCache.ContainsKey(memberInfo))
                    {
                        Type typeConverterAttribute = typeof(ChoTypeConverterAttribute);

                        _typeMemberTypeConverterCache[memberInfo] = EmptyTypeConverters;
                        _typeMemberTypeConverterParamsCache[memberInfo] = EmptyParams;

                        SortedList<int, object> queue = new SortedList<int, object>();
                        SortedList<int, object[]> paramsQueue = new SortedList<int, object[]>();
                        foreach (Attribute attribute in ChoType.GetMemberAttributesByBaseType(memberInfo, typeof(ChoTypeConverterAttribute)))
                        {
                            ChoTypeConverterAttribute converterAttribute = (ChoTypeConverterAttribute)attribute;
                            if (converterAttribute != null)
                            {
                                queue.Add(converterAttribute.Priority, converterAttribute.CreateInstance());
                                paramsQueue.Add(converterAttribute.Priority, converterAttribute.Parameters);
                            }

                            if (queue.Count > 0)
                            {
                                _typeMemberTypeConverterCache[memberInfo] = queue.Values.ToArray();
                                _typeMemberTypeConverterParamsCache[memberInfo] = paramsQueue.Values.ToArray();

                                return _typeMemberTypeConverterCache[memberInfo];
                            }
                        }

                        if (queue.Count == 0 && !memberType.IsSimple())
                        {
                            if (!_typeTypeConverterCache.ContainsKey(memberType))
                            {
                                ChoTypeConverterAttribute converterAttribute = memberType.GetCustomAttribute<ChoTypeConverterAttribute>();
                                if (converterAttribute != null /*&& converterAttribute.ConverterType == memberType*/)
                                {
                                    _typeTypeConverterCache.Add(memberType, new object[] { converterAttribute.CreateInstance() });
                                    _typeTypeConverterParamsCache.Add(memberType, new object[] { converterAttribute.Parameters });

                                    return _typeTypeConverterCache[memberType];
                                }
                                //}

                                TypeConverter converter = TypeDescriptor.GetConverter(memberType);
                                if (converter != null)
                                    _typeTypeConverterCache.Add(memberType, new object[] { converter });
                                else
                                    _typeTypeConverterCache.Add(memberType, EmptyTypeConverters);

                                _typeTypeConverterParamsCache.Add(memberType, EmptyParams);
                            }

                            return _typeTypeConverterCache[memberType];
                        }
                    }

                    return _typeMemberTypeConverterCache.ContainsKey(memberInfo) ? _typeMemberTypeConverterCache[memberInfo] : EmptyTypeConverters;
                }
            }
        }

        #endregion GetTypeConverter Overloads (internal)

        #endregion Shared Members (Public)
    }
}
