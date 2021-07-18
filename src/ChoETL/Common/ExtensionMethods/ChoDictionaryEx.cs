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

        private static object Clone(this object src)
        {
            if (src == null)
                return src;
            if (src is IDictionary<string, object>)
            {
                Dictionary<string, object> dest = new Dictionary<string, object>();
                foreach (var kvp in (IDictionary<string, object>)src)
                    dest.Add(kvp.Key, kvp.Value.Clone());
                return dest;
            }
            else if (src is IList)
            {
                List<object> dest = new List<object>();
                foreach (var item in (IList)src)
                    dest.Add(item.Clone());
                return dest;
            }
            else if (src is ICloneable)
                return ((ICloneable)src).Clone();
            else
                return src;
        }

        private static object Merge(this object dest, object src)
        {
            if (src == null) return dest;
            if (dest == null) return src;

            if (dest is IDictionary<string, object> && src is IDictionary<string, object>)
            {
                Merge(dest as IDictionary<string, object>, src as IDictionary<string, object>);
                return dest;
            }
            else if (dest is IList && src is IList)
            {
                IList dlist = (IList)dest;
                IList slist = (IList)src;

                int dcount = dlist.Count;
                int scount = slist.Count;

                int count = dcount < scount ? dcount : scount;
                for (int i = 0; i < count; i++)
                    dlist[i] = Merge(dlist[i], slist[i]);

                if (dcount < scount)
                {
                    if (dlist.IsFixedSize)
                    {
                        List<object> dlist1 = new List<object>();
                        dlist1.AddRange(dlist.OfType<object>());
                        dlist = dlist1;
                    }

                    for (int i = dcount; i < scount; i++)
                    {
                        dlist.Add(slist[i].Clone());
                    }
                }
                return dlist;
            }
            return dest;
        }

        public static void Merge(this IDictionary<string, object> dest, IDictionary<string, object> src)
        {
            foreach (var kvp in src)
            {
                if (!dest.ContainsKey(kvp.Key))
                    dest.Add(kvp.Key, kvp.Value.Clone());
                else
                {
                    if (dest[kvp.Key] == null)
                        dest[kvp.Key] = kvp.Value.Clone();
                    else
                    {
                        var destValue = dest[kvp.Key];
                        var srcValue = kvp.Value;

                        if (destValue is IDictionary<string, object>)
                        {
                            if (srcValue is IDictionary<string, object>)
                                ((IDictionary<string, object>)destValue).Merge((IDictionary<string, object>)srcValue);
                        }
                        else if (destValue is IList)
                        {
                            if (srcValue is IList)
                            {
                                dest[kvp.Key] = Merge(destValue, srcValue);
                            }
                        }
                        else
                        {
                            if (srcValue is IDictionary<string, object>)
                            {
                                dest[kvp.Key] = kvp.Value.Clone();
                            }
                            else if (srcValue is IList)
                            {
                                dest[kvp.Key] = kvp.Value.Clone();
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<dynamic> FlattenBy(this IEnumerable dicts, params string[] fields)
        {
            if (dicts == null || fields == null)
                yield return dicts;
            else
            {
                foreach (var rec in dicts)
                {
                    if (rec is IDictionary<string, object>)
                    {
                        foreach (var child in FlattenBy((IDictionary<string, object>)rec, fields))
                            yield return child;
                    }
                    else
                        yield return rec;
                }
            }
        }

        public static IEnumerable<dynamic> FlattenBy(this IDictionary<string, object> dict, params string[] fields)
        {
            if (dict == null || fields == null)
                yield return dict;
            else
            {
                dynamic dest = new ChoDynamicObject();
                dest.Merge(dict);

                foreach (var rec in FlattenByInternal(dict, dest, fields))
                    yield return rec;
            }
        }

        private static IEnumerable<dynamic> FlattenByInternal(IDictionary<string, object> dict, dynamic dest, string[] fields)
        {
            if (fields.Length == 0)
                yield break;

            string field = fields.First();
            if (!dict.ContainsKey(field))
                yield break;
            //else if (!(dict[field] is IDictionary<string, object>))
            //    yield break;
            else
            {
                var ele = dict[field];
                if (ele is IList)
                {
                    foreach (IDictionary<string, object> child in (IEnumerable)dict[field])
                    {
                        var dest1 = dest.Clone();
                        dest1.Merge(child);
                        dest1.Remove(field);
                        if (fields.Skip(1).Count() == 0)
                            yield return dest1;
                        else
                        {
                            foreach (var ret in FlattenByInternal(child, dest1, fields.Skip(1).ToArray()))
                                yield return ret;
                        }
                    }
                }
                else
                {
                    IDictionary<string, object> child = ele as IDictionary<string, object>;
                    if (child != null)
                    {
                        var dest1 = dest.Clone();
                        dest1.Merge(child);
                        dest1.Remove(field);
                        if (fields.Skip(1).Count() == 0)
                            yield return dest1;
                        else
                        {
                            foreach (var ret in FlattenByInternal(child, dest1, fields.Skip(1).ToArray()))
                                yield return ret;
                        }
                    }
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false)
        {
            if (dict is ChoDynamicObject && ((ChoDynamicObject)dict).DynamicObjectName != ChoDynamicObject.DefaultName)
                return Flatten(dict, ((ChoDynamicObject)dict).DynamicObjectName, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix);
            else
                return Flatten(dict, (string)null, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix);
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IList list, string pkey, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false)
        {
            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            int index = 0;
            string key = null;

            foreach (var item in list)
            {
                key = pkey;

                if (item is ChoDynamicObject && ((ChoDynamicObject)item).DynamicObjectName != ChoDynamicObject.DefaultName)
                {
                    key = "{0}{2}{1}".FormatString(key, ((ChoDynamicObject)item).DynamicObjectName, nestedKeySeparator);
                }
                else if (item is IDictionary<string, object>)
                {
                    foreach (var kvp1 in Flatten(item as IDictionary<string, object>, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator))
                        yield return kvp1;
                }
                else if (item is IDictionary)
                {
                    foreach (var kvp1 in Flatten(item as IDictionary, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator))
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
        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary dict, string key = null, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false)
        {
            if (dict == null)
                yield break;

            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            foreach (var dkey in dict.Keys)
            {
                string dKey = dkey.ToNString();
                if (dict[key] is IDictionary<string, object>)
                {
                    string lkey = null;
                    lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                    if (!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary<string, object>, lkey, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                    else
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary<string, object>, null, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                }
                else if (dict[key] is IDictionary)
                {
                    string lkey = null;
                    lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                    if (!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary, lkey, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                    else
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary, null, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                }
                else if (dict[key] is IList)
                {
                    var lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                    foreach (var tuple in Flatten(dict[key] as IList, lkey, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                        yield return tuple;
                }
                else if (dict[key] == null || dict[key].GetType().IsSimple())
                    yield return new KeyValuePair<string, object>(key == null ? dKey.ToString() : "{0}{2}{1}".FormatString(key, dKey.ToString(), nestedKeySeparator), dict[key]);
                else
                {
                    foreach (var tuple in Flatten(dict[key].ToDynamicObject() as IDictionary<string, object>, key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator), nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                        yield return tuple;
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, string key = null, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false)
        {
            if (dict == null)
                yield break;

            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            foreach (var kvp in dict)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    string lkey = null;
                    lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    if (!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary<string, object>, lkey, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                    else
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary<string, object>, null, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                }
                else if (kvp.Value is IDictionary)
                {
                    string lkey = null;
                    lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    if (!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary, lkey, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                    else
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary, null, nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                }
                else if (kvp.Value is IList)
                {
                    var lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                    foreach (var tuple in Flatten(kvp.Value as IList, lkey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
                        yield return tuple;
                }
                else if (kvp.Value == null || kvp.Value.GetType().IsSimple())
                {
                    //if (key == null)
                    //{
                    //    yield return new KeyValuePair<string, object>("Key", kvp.Key);
                    //    yield return new KeyValuePair<string, object>("Value", kvp.Value);
                    //}
                    //else
                        yield return new KeyValuePair<string, object>(key == null ? kvp.Key.ToString() : "{0}{2}{1}".FormatString(key, kvp.Key.ToString(), nestedKeySeparator), kvp.Value);
                }
                else
                {
                    if (key == null)
                    {
                        yield return new KeyValuePair<string, object>("Key", kvp.Key);
                        foreach (var tuple in Flatten(kvp.Value.ToDynamicObject() as IDictionary<string, object>, null, nestedKeySeparator, ignoreDictionaryFieldPrefix))
                            yield return tuple;
                    }
                    else
                        foreach (var tuple in Flatten(kvp.Value.ToDynamicObject() as IDictionary<string, object>, key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator), nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix))
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

        public static bool ContainsKey<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase = false, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            if (culture == null)
                culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            return dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).Any();
        }

        public static void AddOrUpdateValue<TValue>(this IDictionary<string, TValue> dict, string key, TValue value, bool ignoreCase = false, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            if (culture == null)
                culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (cultureSpecificKeyName.IsNullOrWhiteSpace())
                dict.Add(cultureSpecificKeyName, value);
            else
                dict[cultureSpecificKeyName] = value;
        }

        public static TValue GetValue<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase = false, CultureInfo culture = null, TValue defaultValue = default(TValue))
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            if (culture == null)
                culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (!cultureSpecificKeyName.IsNullOrWhiteSpace())
                return dict[cultureSpecificKeyName];
            else
                return defaultValue;
        }

        public static object ToObject(this IDictionary<string, object> dict, Type type)
        {
            object target = ChoActivator.CreateInstance(type);
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
