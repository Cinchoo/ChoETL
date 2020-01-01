using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDictionaryEx
    {
        //public static IEnumerable<KeyValuePair<string, object>> Flatten(this object target, bool useNestedKeyFormat = true)
        //{
        //    if (target == null)
        //        return null;

        //    if (target is IList)
        //        return target;

        //    Type type = target.GetType();
        //    foreach (var pd in ChoTypeDescriptor.GetProperties(type))
        //    {

        //    }
        //}

        public static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, char? nestedKeySeparator = null)
        {
            if (dict is ChoDynamicObject && ((ChoDynamicObject)dict).DynamicObjectName != ChoDynamicObject.DefaultName)
                return Flatten(dict, ((ChoDynamicObject)dict).DynamicObjectName, nestedKeySeparator);
            else
                return Flatten(dict, null, nestedKeySeparator);
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IList list, string pkey, char? nestedKeySeparator = null)
        {
            nestedKeySeparator = nestedKeySeparator == null ? '_' : nestedKeySeparator;
            int index = 0;
            string key = null;

            foreach (var item in list)
            {
                key = pkey;

                if (item is ChoDynamicObject && ((ChoDynamicObject)item).DynamicObjectName != ChoDynamicObject.DefaultName)
                {
                    key = "{0}{2}{1}".FormatString(key, ((ChoDynamicObject)item).DynamicObjectName, nestedKeySeparator);
                }
                if (item is IDictionary<string, object>)
                {
                    foreach (var kvp1 in Flatten(item as IDictionary<string, object>, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator))
                        yield return kvp1;
                }
                else if (item is IList)
                {
                    foreach (var kvp1 in Flatten(item as IList, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator))
                        yield return kvp1;
                }
                else
                    yield return new KeyValuePair<string, object>("{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), item);
            }

        }
        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, string key = null, char? nestedKeySeparator = null)
        {
            if (dict == null)
                yield break;

            nestedKeySeparator = nestedKeySeparator == null ? '_' : nestedKeySeparator;
            foreach (var kvp in dict)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    var lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    foreach (var tuple in Flatten(kvp.Value as IDictionary<string, object>, lkey, nestedKeySeparator))
                        yield return tuple;
                }
                else if (kvp.Value is IList)
                {
                    var lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    foreach (var tuple in Flatten(kvp.Value as IList, lkey, nestedKeySeparator))
                        yield return tuple;
                }
                else if (kvp.Value == null || kvp.Value.GetType().IsSimple())
                    yield return new KeyValuePair<string, object>(key == null ? kvp.Key.ToString() : "{0}{2}{1}".FormatString(key, kvp.Key.ToString(), nestedKeySeparator), kvp.Value);
                else
                {
                    foreach (var tuple in Flatten(kvp.Value.ToDynamicObject() as IDictionary<string, object>, key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator), nestedKeySeparator))
                        yield return tuple;
                }
            }
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");

            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        public static bool ContainsKey<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase, CultureInfo culture)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            return dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).Any();
        }

        public static void AddOrUpdateValue<TValue>(this IDictionary<string, TValue> dict, string key, TValue value, bool ignoreCase, CultureInfo culture)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (cultureSpecificKeyName.IsNullOrWhiteSpace())
                dict.Add(cultureSpecificKeyName, value);
            else
                dict[cultureSpecificKeyName] = value;
        }

        public static TValue GetValue<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase, CultureInfo culture, TValue defaultValue = default(TValue))
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (!cultureSpecificKeyName.IsNullOrWhiteSpace())
                return dict[cultureSpecificKeyName];
            else
                return defaultValue;
        }

        public static object ToObject(this IDictionary<string, object> dict, Type type)
        {
            object target = Activator.CreateInstance(type);
            string key = null;
            foreach (var p in ChoType.GetProperties(type))
            {
                if (p.GetCustomAttribute<ChoIgnoreMemberAttribute>() != null)
                    continue;

                key = p.Name;
                var attr = p.GetCustomAttribute<ChoPropertyAttribute>();
                if (attr != null && !attr.Name.IsNullOrWhiteSpace())
                    key = attr.Name.NTrim();

                if (!dict.ContainsKey(key))
                    continue;

                p.SetValue(target, dict[key].CastObjectTo(p.PropertyType));
            }

            return target;
        }

        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            return (T)ToObject(source, typeof(T));
            var someObject = new T();
            var someObjectType = someObject.GetType();

            foreach (var item in source)
            {
                someObjectType
                         .GetProperty(item.Key)
                         .SetValue(someObject, item.Value, null);
            }

            return someObject;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            if (source == null) return null;

            string key = null;
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (typeof(ChoDynamicObject).IsAssignableFrom(source.GetType()))
            {
                ChoDynamicObject dobj = source as ChoDynamicObject;
                if (dobj.AlternativeKeys.Count > 0)
                {
                    foreach (var key1 in dobj.Keys)
                    {
                        if (dobj.AlternativeKeys.ContainsKey(key1))
                            dict.Add(dobj.AlternativeKeys[key1], dobj[key1]);
                        else
                            dict.Add(key1, dobj[key1]);
                    }
                    return dict;
                }
                else
                    return source as IDictionary<string, object>;
            }
            else if (source.GetType().IsDynamicType())
                return source as IDictionary<string, object>;
            else
            {
                foreach (var p in source.GetType().GetProperties(bindingAttr))
                {
                    if (p.GetCustomAttribute<ChoIgnoreMemberAttribute>() != null)
                        continue;

                    key = p.Name;
                    var attr = p.GetCustomAttribute<ChoPropertyAttribute>();
                    if (attr != null && !attr.Name.IsNullOrWhiteSpace())
                        key = attr.Name.NTrim();

                    if (dict.ContainsKey(key))
                        continue;

                    dict.Add(key, p.GetValue(source, null));
                }
            }
            return dict;
        }
    }
}
