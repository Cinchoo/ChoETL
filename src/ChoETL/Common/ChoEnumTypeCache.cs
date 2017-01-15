namespace System
{
    #region NameSpaces

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.ComponentModel;

    #endregion NameSpaces

    internal static class ChoEnumTypeDescCache
    {
        #region Shared Data Members (Private)

        private readonly static object _padLock = new object();
        private readonly static Dictionary<Type, Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>>> _enumTypeCache =
            new Dictionary<Type, Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>>>();

        #endregion Shared Data Members (Private)

        #region Shared Members (Public)

        public static Enum GetEnumValue(Type enumType, string description)
        {
            if (!enumType.IsEnum)
                throw new ApplicationException("Type should be enum");

            Type type = enumType;
            if (!_enumTypeCache.ContainsKey(type))
                DiscoverEnumType(type);

            if (!_enumTypeCache.ContainsKey(type))
                return (Enum)Activator.CreateInstance(enumType);

            Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>> tuple = _enumTypeCache[type];
            if (tuple == null || tuple.Item1 == null)
                return (Enum)Activator.CreateInstance(enumType);

            Dictionary<string, object> enumDesc2ValueCache = tuple.Item1;
            return (Enum)(enumDesc2ValueCache.ContainsKey(description) ? enumDesc2ValueCache[description] : null);
        }

        public static T GetEnumValue<T>(string description) where T : struct
        {
            T enumValue = default(T);
            if (!(enumValue is Enum))
                throw new ApplicationException("Type should be enum");

            Type type = enumValue.GetType();
            if (!_enumTypeCache.ContainsKey(type))
                DiscoverEnumType(type);

            if (!_enumTypeCache.ContainsKey(type))
                return default(T);

            Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>> tuple = _enumTypeCache[type];
            if (tuple == null || tuple.Item1 == null)
                return default(T);

            Dictionary<string, object> enumDesc2ValueCache = tuple.Item1;
            return (T)(enumDesc2ValueCache.ContainsKey(description) ? enumDesc2ValueCache[description] : null);
        }

        public static string GetEnumDescription(Enum enumValue)
        {
            Type type = enumValue.GetType();
            
            if (!_enumTypeCache.ContainsKey(type))
                DiscoverEnumType(type);

            if (!_enumTypeCache.ContainsKey(type))
                return null;

            Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>> tuple = _enumTypeCache[type];
            if (tuple == null || tuple.Item2 == null)
                return null;

            MemberInfo[] memberInfos = type.GetMember(enumValue.ToString());

            if (memberInfos != null && memberInfos.Length > 0)
            {
                MemberInfo memberInfo = memberInfos[0];
                Dictionary<MemberInfo, string> enumValue2DescCache = tuple.Item2;
                return enumValue2DescCache.ContainsKey(memberInfo) ? enumValue2DescCache[memberInfo] : null;
            }
            else
                return null;
        }

        #endregion Shared Members (Public)

        #region Shared Members (Private)

        private static void DiscoverEnumType(Type type)
        {
            lock (_padLock)
            {
                if (_enumTypeCache.ContainsKey(type))
                    return;

                MemberInfo[] memInfos = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

                string enumDesc;
                Dictionary<string, object> enumDesc2ValueCache = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                Dictionary<MemberInfo, string> enumValue2DescCache = new Dictionary<MemberInfo, string>();

                if (memInfos != null && memInfos.Length > 0)
                {
                    foreach (MemberInfo memInfo in memInfos)
                    {
                        object[] attrs = memInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                        if (attrs != null && attrs.Length > 0)
                        {
                            foreach (Attribute attr in attrs)
                            {
                                enumDesc = ((DescriptionAttribute)attrs[0]).Description;

                                if (!enumDesc2ValueCache.ContainsKey(enumDesc))
                                    enumDesc2ValueCache.Add(enumDesc, Enum.Parse(type, memInfo.Name));

                                if (!enumValue2DescCache.ContainsKey(memInfo))
                                    enumValue2DescCache.Add(memInfo, enumDesc);
                            }
                        }
                    }
                }

                _enumTypeCache.Add(type, new Tuple<Dictionary<string, object>, Dictionary<MemberInfo, string>>(enumDesc2ValueCache, enumValue2DescCache));
            }
        }

        #endregion Shared Members (Private)
    }
}
