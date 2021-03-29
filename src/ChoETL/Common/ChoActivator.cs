using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoActivator
    {
        private static readonly object _objCachePadLock = new object();
        private static readonly Dictionary<Type, object> _objCache = new Dictionary<Type, object>();
        private static readonly object _padLock = new object();
		public static Func<Type, object[], object> Factory
		{
			get;
			set;
		}

        static ChoActivator()
        {
            ChoUtility.Init();
        }
        public static T CreateInstanceNCache<T>()
        {
            return (T)CreateInstanceNCache(typeof(T));
        } 

        public static object CreateInstanceNCache(Type objType, params object[] args)
        {
            if (_objCache.ContainsKey(objType))
                return _objCache[objType];

            lock (_objCachePadLock)
            {
                if (_objCache.ContainsKey(objType))
                    return _objCache[objType];

                var obj = CreateInstance(objType, args);
                _objCache.Add(objType, obj);

                return obj;
            }
        }

        public static T CreateInstance<T>()
		{
			return (T)CreateInstance(typeof(T));
		}

		public static object CreateInstance(Type objType, params object[] args)
		{
			try
			{
                object obj = null;
                if (Factory != null)
                {
                    lock (_padLock)
                    {
                        obj = Factory(objType, args);
                    }
                }
                if (obj == null)
                {
                    if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        obj = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(objType.GetGenericArguments()));
                    }
                    else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(IList<>))
                    {
                        obj = Activator.CreateInstance(typeof(List<>).MakeGenericType(objType.GetGenericArguments()));
                    }
                    else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        obj = Activator.CreateInstance(typeof(List<>).MakeGenericType(objType.GetGenericArguments()));
                    }
                    else if (objType == typeof(string))
                    {
                        return String.Empty;
                    }
                    else if (objType == typeof(object))
                        return new ChoDynamicObject();
                    else
                        obj = Activator.CreateInstance(objType, args);
                }

				return obj;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		public static T CreateInstanceAndInit<T>()
        {
            return (T)CreateInstanceAndInit(typeof(T));
        }

        public static object CreateInstanceAndInit(Type objType, params object[] args)
        {
            try
            {
				object obj = Factory != null ? Factory(objType, args) : null;
                if (obj == null)
                {
                    obj = Activator.CreateInstance(objType, args);
                    obj.Initialize();
                }
                return obj;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public static T CloneObject<T>(T srcObject)
        {
            return (T)CloneObject((object)srcObject);
        }

        public static object CloneObject(object srcObject)
        {
            ChoGuard.ArgumentNotNull(srcObject, "SrcObject");

            try
            {
                object obj = CreateInstanceAndInit(srcObject.GetType());
                srcObject.EagerCloneTo(obj);
                return obj;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public static object CreateMoqInstance(Type objType, ChoIniFile iniFile = null)
        {
            //if (typeof(ChoRecord).IsAssignableFrom(objType))
            //    return CreateDynamicMoqInstance(objType, iniFile);

            object obj = ChoActivator.CreateInstanceAndInit(objType);

            foreach (KeyValuePair<MemberInfo, Attribute> kv in ChoUtility.DiscoverMembers(objType, typeof(ChoRandomAttribute)))
            {
                if (!(kv.Value is ChoRandomAttribute)) continue;

                ChoType.SetMemberValue(obj, kv.Key, ((ChoRandomAttribute)kv.Value).NextValue());
            }

            return obj;
        }

        //private static object CreateDynamicMoqInstance(Type objType, ChoIniFile iniFile)
        //{
        //    ChoGuard.ArgumentNotNull(iniFile, "IniFile");

        //    ChoRecord obj = ChoActivator.CreateInstance(objType) as ChoRecord;
        //    if (obj == null)
        //        throw new ApplicationException("Type is not a ChoRecord type.");

        //    foreach (KeyValuePair<string, ChoRandomGenerator> kvp in GetRandomGenerators(iniFile))
        //    {
        //        if (kvp.Value != null)
        //            obj.SetPropertyValue(kvp.Key, kvp.Value.NextValue());
        //        else
        //            obj.SetPropertyValue(kvp.Key, null);
        //    }

        //    return obj;
        //}

        private static readonly object _rgLock = new object();
        private static readonly Dictionary<string, Dictionary<string, ChoRandomGenerator>> _randomGeneratorsCache = new Dictionary<string, Dictionary<string, ChoRandomGenerator>>();
        private static Dictionary<string, ChoRandomGenerator> GetRandomGenerators(ChoIniFile iniFile)
        {
            if (_randomGeneratorsCache.ContainsKey(iniFile.Key))
                return _randomGeneratorsCache[iniFile.Key];

            lock (_rgLock)
            {
                if (_randomGeneratorsCache.ContainsKey(iniFile.Key))
                    return _randomGeneratorsCache[iniFile.Key];

                Dictionary<string, ChoRandomGenerator> dict = new Dictionary<string, ChoRandomGenerator>();
                ChoIniFile randomIniFile = iniFile.GetSection("RANDOM");
                ChoRandomGenerator ra = null;
                Dictionary<string, string> iniKeyValuesDict = randomIniFile.KeyValuesDict;
                foreach (string fieldName in iniKeyValuesDict.Keys)
                {
                    ra = GetRandomObject(fieldName, randomIniFile.GetValue<string>(fieldName));
                    dict.Add(fieldName, ra);
                }

                _randomGeneratorsCache.Add(iniFile.Key, dict);
                return _randomGeneratorsCache[iniFile.Key];
            }
        }

        private static ChoRandomGenerator GetRandomObject(string fieldName, string rgParams)
        {
            ChoGuard.ArgumentNotNull(fieldName, "FieldName");

            if (rgParams.IsNullOrWhiteSpace())
                return null;
                //throw new ApplicationException("No random generator defined for {0} field.".FormatString(fieldName));

            string rgType = rgParams.SplitNTrim().FirstOrDefault();
            if (rgType.IsNullOrWhiteSpace())
                throw new ApplicationException("No random generator defined for {0} field.".FormatString(fieldName));

            Type rgt = ChoType.GetType(rgType);
            if (rgt == null)
                throw new ApplicationException("No random generator found for {0} field.".FormatString(fieldName));

            string rgObjectParams = String.Join(",", rgParams.SplitNTrim().Skip(1).ToArray());
            return Activator.CreateInstance(rgt, (from z in rgObjectParams.SplitNTrim()
                                                      select z.ToObject()).ToArray()) as ChoRandomGenerator;
        }
    }
}
