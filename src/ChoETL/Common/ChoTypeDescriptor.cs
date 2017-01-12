namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows.Data;

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

        public static IEnumerable<PropertyDescriptor> GetAllProperties(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>();
        }

        public static IEnumerable<PropertyDescriptor> GetProperties<T>(Type type)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Attributes.OfType<T>().Any());
        }

        public static IEnumerable<PropertyDescriptor> GetProperties(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>();
        }

        public static PropertyDescriptor GetProperty<T>(Type type, string propName)
            where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd =>
                pd.Name == propName && pd.Attributes.OfType<T>().Any()).FirstOrDefault();
        }

        public static PropertyDescriptor GetProperty(Type type, string propName)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            return TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd =>
                pd.Name == propName).FirstOrDefault();
        }

        public static T GetPropetyAttribute<T>(PropertyDescriptor pd)
                   where T : Attribute
        {
            if (pd == null)
                return null;
            else
                return pd.Attributes.OfType<T>().First();
        }

        public static IEnumerable<T> GetPropetyAttributes<T>(PropertyDescriptor pd)
                   where T : Attribute
        {
            if (pd == null)
                return new T[] { };
            else
                return pd.Attributes.OfType<T>();
        }

        public static T GetPropetyAttribute<T>(Type type, string propName)
                   where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            PropertyDescriptor pd = TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd1 => 
                pd1.Name == propName && pd1.Attributes.OfType<T>().Any()).FirstOrDefault();
            if (pd == null)
                return null;
            else
                return GetPropetyAttribute<T>(pd);
        }

        public static IEnumerable<T> GetPropetyAttributes<T>(Type type, string propName)
                   where T : Attribute
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNullOrEmpty(propName, "PropName");

            PropertyDescriptor pd = TypeDescriptor.GetProperties(type).AsTypedEnumerable<PropertyDescriptor>().Where(pd1 =>
                pd1.Name == propName && pd1.Attributes.OfType<T>().Any()).FirstOrDefault();
            if (pd == null)
                return Enumerable.Empty<T>();
            else
                return GetPropetyAttributes<T>(pd);
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

                        int index = 0;
                        SortedList<int, object> queue = new SortedList<int, object>();
                        SortedList<int, object[]> paramsQueue = new SortedList<int, object[]>();
                        foreach (Attribute attribute in GetPropetyAttributes<ChoTypeConverterAttribute>(memberInfo.ReflectedType, memberInfo.Name))  //ChoType.GetMemberAttributesByBaseType(memberInfo, typeof(ChoTypeConverterAttribute)))
                        {
                            ChoTypeConverterAttribute converterAttribute = (ChoTypeConverterAttribute)attribute;
                            if (converterAttribute != null)
                            {
                                if (converterAttribute.PriorityInternal == null)
                                {
                                    queue.Add(index, converterAttribute.CreateInstance());
                                    paramsQueue.Add(index, converterAttribute.Parameters);
                                    index++;
                                }
                                else
                                {
                                    queue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.CreateInstance());
                                    paramsQueue.Add(converterAttribute.PriorityInternal.Value, converterAttribute.Parameters);
                                }
                            }
                        }

                        if (queue.Count == 0)
                        {
                            Type type = ChoType.GetMemberType(memberInfo);
                            if (ChoTypeConverter.Global.Contains(type))
                            {
                                _typeMemberTypeConverterCache[memberInfo] = (from a1 in ChoTypeConverter.Global.GetAll()
                                                                             where a1.Key == type
                                                                             select a1.Value).ToArray();
                            }
                        }

                        if (queue.Count > 0)
                        {
                            _typeMemberTypeConverterCache[memberInfo] = queue.Values.ToArray();
                            _typeMemberTypeConverterParamsCache[memberInfo] = paramsQueue.Values.ToArray();

                            return _typeMemberTypeConverterCache[memberInfo];
                        }

                        if (queue.Count == 0 && !memberType.IsSimple())
                        {
                            if (!_typeTypeConverterCache.ContainsKey(memberType))
                            {
                                Type[] types = ChoType.GetTypes(typeof(ChoTypeConverterAttribute)).Where(t => t.GetCustomAttribute<ChoTypeConverterAttribute>().ConverterType == memberType).ToArray();

                                if (types != null)
                                {
                                    int index1 = 0;
                                    SortedList<int, object> queue1 = new SortedList<int, object>();
                                    SortedList<int, object[]> paramsQueue1 = new SortedList<int, object[]>();

                                    foreach (Type t in types)
                                    {
                                        queue.Add(index1, Activator.CreateInstance(t));
                                        index1++;
                                    }
                                    _typeTypeConverterCache.Add(memberType, queue.Values.ToArray());
                                    return _typeTypeConverterCache[memberType];
                                }

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

        public static object[] GetTypeConvertersForType(Type memberType)
        {
            if (memberType == null)
                return null;

            if (_typeTypeConverterCache.ContainsKey(memberType))
                return _typeTypeConverterCache[memberType];
            else
            {
                lock (_typeMemberTypeConverterCacheLockObject)
                {
                    if (!_typeTypeConverterCache.ContainsKey(memberType))
                    {
                        Type type = memberType;
                        if (ChoTypeConverter.Global.Contains(type))
                        {
                            _typeTypeConverterCache[type] = (from a1 in ChoTypeConverter.Global.GetAll()
                                                                         where a1.Key == type
                                                                         select a1.Value).ToArray();
                            return _typeTypeConverterCache[type];
                        }

                        Type[] types = ChoType.GetTypes(typeof(ChoTypeConverterAttribute)).Where(t => t.GetCustomAttribute<ChoTypeConverterAttribute>().ConverterType == memberType).ToArray();

                        if (types != null)
                        {
                            int index1 = 0;
                            SortedList<int, object> queue1 = new SortedList<int, object>();
                            SortedList<int, object[]> paramsQueue1 = new SortedList<int, object[]>();

                            foreach (Type t in types)
                            {
                                queue1.Add(index1, Activator.CreateInstance(t));
                                index1++;
                            }
                            _typeTypeConverterCache.Add(memberType, queue1.Values.ToArray());
                            return _typeTypeConverterCache[memberType];
                        }

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
        }

        #endregion GetTypeConverter Overloads (internal)

        #endregion Shared Members (Public)
    }
}
