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
    public enum ChoArrayNamePrefixMode1
    {
        None,
        ContainerNameOnly,
        ObjectNameOnly,
        Both
    }

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
        public static IDictionary<string, object> TranslateDictionary(this IDictionary values)
        {
            return values.Keys.Cast<string>().ToDictionary(key => key, key => values[key] as object);
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
        public static IEnumerable<string> GetNestedKeys(this object obj, StringComparer comparer = null)
        {
            if (obj == null)
                return null;

            if (comparer == null)
                comparer = StringComparer.InvariantCultureIgnoreCase;

            IList<string> keys = new List<string>();
            if (obj != null)
                GetNestedKeys(obj, keys, comparer);

            return keys;
        }

        private static void GetNestedKeys(object obj, IList<string> keys, StringComparer comparer)
        {
            if (obj is IDictionary<string, object>)
            {
                foreach (var kvp in ((IDictionary<string, object>)obj))
                {
                    if (kvp.Value is IDictionary<string, object> || kvp.Value is IList)
                    {
                        if (!keys.Contains(kvp.Key, comparer))
                            keys.Add(kvp.Key);
                        GetNestedKeys(kvp.Value, keys, comparer);
                    }
                }
            }
            else if (obj is IList)
            {
                foreach (var item in (IList)obj)
                {
                    GetNestedKeys(item, keys, comparer);
                }
            }
            else
            {

            }
        }
        public static IEnumerable<dynamic> Flatten(this IEnumerable dicts)
        {
            return Flatten(dicts, false);
        }
        public static IEnumerable<dynamic> Flatten(this IEnumerable dicts, bool turnOffSingularization)
        {
            var cache = dicts != null ? dicts.OfType<object>().ToArray() : null;
            var fields = GetNestedKeys(cache).ToArray();

            return FlattenBy(cache, fields, turnOffSingularization);
        }

        public static IEnumerable<dynamic> FlattenBy(this IEnumerable dicts, params string[] fields)
        {
            return FlattenBy(dicts, fields, false);
        }
        public static IEnumerable<dynamic> FlattenBy(this IEnumerable dicts, string[] fields, bool turnOffSingularization)
        {
            var cache = dicts != null ? dicts.OfType<object>().ToArray() : null;
            if (cache == null)
                yield break;

            if (fields.IsNullOrEmpty())
                fields = GetNestedKeys(cache).ToArray();

            if (fields.IsNullOrEmpty())
            {
                foreach (var rec in cache)
                    yield return rec;
            }
            else
            {
                foreach (var rec in cache)
                {
                    if (rec is IDictionary<string, object>)
                    {
                        foreach (var child in FlattenBy((IDictionary<string, object>)rec, fields, turnOffSingularization))
                            yield return child;
                    }
                    else
                        yield return rec;
                }
            }
        }
        public static IEnumerable<dynamic> Flatten(this IDictionary<string, object> dict)
        {
            return Flatten(dict, false);
        }
        public static IEnumerable<dynamic> Flatten(this IDictionary<string, object> dict, bool turnOffSingularization, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null
            )
        {
            var fields = GetNestedKeys(dict).ToArray();
            return FlattenBy(dict, fields, turnOffSingularization, ignoreDictionaryFieldPrefix, valueNamePrefix);
        }
        public static IEnumerable<dynamic> FlattenBy(this IDictionary<string, object> dict, params string[] fields)
        {
            return FlattenBy(dict, fields, false);
        }
        public static IEnumerable<dynamic> FlattenBy(this IDictionary<string, object> dict, string[] fields,
            bool turnOffSingularization = false, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null
            )
        {
            if (fields.IsNullOrEmpty())
                fields = GetNestedKeys(dict).ToArray();

            if (dict == null || fields.IsNullOrEmpty())
                yield return dict;
            else
            {
                dynamic dest = new ChoDynamicObject();
                dest.Merge(dict);

                foreach (var rec in FlattenByInternal(dict, dest, fields, null, turnOffSingularization, ignoreDictionaryFieldPrefix, valueNamePrefix))
                    yield return rec;
            }
        }

        private static IEnumerable<dynamic> FlattenByInternal(IDictionary<string, object> dict, dynamic dest, string[] fields, string key = null,
            bool turnOffSingularization = false, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null
            )
        {
            if (fields.Length == 0)
            {
                if (!key.IsNullOrWhiteSpace())
                {
                    var dest1 = dest.Clone();
                    dest1.Remove(key);
                    foreach (var kvp in dict)
                    {
                        dest1.Add(kvp.Key, kvp.Value);
                    }
                    yield return dest1;
                }
                yield break;
            }
            else
            {
                foreach (var kvp in dict.Where(kvp => !fields.Any(f => f == kvp.Key)))
                {
                    if (kvp.Value != null)
                        dest.Add(kvp.Key, kvp.Value);
                }
            }

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
                    var dictField = dict[field];
                    if (dictField is IDictionary<string, object>)
                    {

                    }
                    else if (dictField is IDictionary)
                    {
                        dictField = ((IDictionary)dictField).TranslateDictionary();
                    }

                    if (dictField is IList)
                    {
                        foreach (var child in (IList)dictField)
                        {
                            var newKey = turnOffSingularization ? field : field.ToSingular();
                            var dest1 = dest.Clone();
                            dest1.Remove(field);
                            //dest1.Add($"{newKey}", child);
                            if (child != null)
                            {
                                if (child is IDictionary<string, object>)
                                {
                                    foreach (var ret in FlattenByInternal(child as IDictionary<string, object>, dest1, fields.Skip(1).ToArray(), newKey, turnOffSingularization,
                                        ignoreDictionaryFieldPrefix, valueNamePrefix))
                                        yield return ret;
                                }
                                else if (child is IDictionary)
                                {
                                    foreach (var ret in FlattenByInternal(((IDictionary)child).TranslateDictionary(), dest1, fields.Skip(1).ToArray(), newKey, turnOffSingularization,
                                        ignoreDictionaryFieldPrefix, valueNamePrefix))
                                        yield return ret;
                                }
                                else if (child.GetType().IsAnonymousType())
                                {
                                    var d = child.ToDictionary();
                                    dest1.Add($"{newKey}", d);
                                    foreach (var ret in FlattenByInternal(d, dest1, fields.Skip(1).ToArray(), newKey, turnOffSingularization,
                                        ignoreDictionaryFieldPrefix, valueNamePrefix))
                                        yield return ret;
                                }
                                else
                                    yield return dest1;
                            }
                            else
                                yield return dest1;
                        }
                    }
                    else if (dictField is IDictionary<string, object>)
                    {
                        foreach (IDictionary<string, object> child in (IEnumerable)dictField)
                        {
                            var dest1 = dest.Clone();
                            dest1.Merge(child);
                            dest1.Remove(field);
                            if (fields.Skip(1).Count() == 0)
                                yield return dest1;
                            else
                            {
                                foreach (var ret in FlattenByInternal(child, dest1, fields.Skip(1).ToArray(), null, turnOffSingularization,
                                    ignoreDictionaryFieldPrefix, valueNamePrefix))
                                    yield return ret;
                            }
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
                            foreach (var ret in FlattenByInternal(child, dest1, fields.Skip(1).ToArray(), null, turnOffSingularization,
                                ignoreDictionaryFieldPrefix, valueNamePrefix))
                                yield return ret;
                        }
                    }
                }
            }
        }

        public static IDictionary<string, object> FlattenToDictionary(this object target, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null,
            bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null)
        {
            var dict = Flatten(target, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix,
                valueNamePrefix).ToDictionary();
            foreach (var kvp in dict.ToArray())
            {
                if (kvp.Value is string && ((string)kvp.Value) == ChoETLSettings.EMPTY_VALUE)
                    dict[kvp.Key] = String.Empty;
                else if (kvp.Value is string && ((string)kvp.Value) == ChoETLSettings.NULL_VALUE)
                    dict[kvp.Key] = null;
            }
            return dict;
        }

        public static IEnumerable<KeyValuePair<string, object>> Flatten(this object target, char? nestedKeySeparator = null, char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false,
            string valueNamePrefix = null)
        {
            if (target == null)
                return Enumerable.Empty<KeyValuePair<string, object>>();

            if (target is IDictionary)
            {
                return Flatten(((IDictionary)target).Keys.Cast<object>().ToDictionary(key => key.ToNString(), key => ((IDictionary)target)[key]),
                    nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix);
            }
            else if (target.GetType().IsSimple())
                return Enumerable.Repeat(new KeyValuePair<string, object>(ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix), target), 1);
            else if (target is IList)
            {
                if (target.GetType().GetEnumerableItemType().IsSimple())
                {
                    return ((IList)target).OfType<object>().Select((o, i) => new KeyValuePair<string, object>($"{ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix)}{i + ChoETLSettings.ValueNameStartIndex}", o));
                }
                else
                {
                    return ((IList)target).OfType<object>().Select((o, i) => new KeyValuePair<string, object>($"{ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix)}{i + ChoETLSettings.ValueNameStartIndex}",
                        o.ToDictionary(valueNamePrefix: valueNamePrefix).Flatten(nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix,
                        valueNamePrefix).ToArray()));
                }
            }
            else
            {
                return target.ToDictionary(valueNamePrefix: valueNamePrefix).Flatten(nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix,
                    valueNamePrefix).ToArray();
            }
        }

        public static IDictionary<string, object> FlattenToDictionary(this IDictionary<string, object> dict, char? nestedKeySeparator = null, char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false,
            string valueNamePrefix = null, bool ignoreRootDictionaryFieldPrefix = false)
        {
            return Flatten(dict, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix,
                valueNamePrefix, ignoreRootDictionaryFieldPrefix).ToDictionary(valueNamePrefix: valueNamePrefix);
        }

        public static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, char? nestedKeySeparator = null,
            char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false,
            string valueNamePrefix = null, bool ignoreRootDictionaryFieldPrefix = false)
        {
            //if (dict is ChoDynamicObject && ((ChoDynamicObject)dict).DynamicObjectName != ChoDynamicObject.DefaultName)
            //    return Flatten(dict, false, ignoreDictionaryFieldPrefix ? null : ((ChoDynamicObject)dict).DynamicObjectName, nestedKeySeparator, arrayIndexSeparator, 
            //        arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix);
            //else
            //    return Flatten(dict, (string)null, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, 
            //        ignoreDictionaryFieldPrefix, valueNamePrefix);

            if (dict is ChoDynamicObject && ((ChoDynamicObject)dict).DynamicObjectName != ChoDynamicObject.DefaultName)
                return Flatten(dict, ignoreRootDictionaryFieldPrefix ? null : ((ChoDynamicObject)dict).DynamicObjectName, nestedKeySeparator, arrayIndexSeparator,
                    arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix);
            else
                return Flatten(dict, (string)null, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator,
                    ignoreDictionaryFieldPrefix, valueNamePrefix);
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IList list, string pkey, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null,
            bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null)
        {
            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            int index = 0;
            string key = null;

            foreach (var item in list)
            {
                key = pkey;

                //if (item is ChoDynamicObject && ((ChoDynamicObject)item).DynamicObjectName != ChoDynamicObject.DefaultName)
                //{
                //    key = "{0}{2}{1}".FormatString(key, ((ChoDynamicObject)item).DynamicObjectName, nestedKeySeparator);
                //}
                if (item is ChoDynamicObject dobj)
                {
                    if (true) //!ignoreDictionaryFieldPrefix /*&& dobj.DynamicObjectName != ChoDynamicObject.DefaultName*/)
                    {
                        key = $"{key}{nestedKeySeparator}{index++}"; // {nestedKeySeparator}{dobj.DynamicObjectName}";
                    }

                    if (!ignoreDictionaryFieldPrefix)
                    {
                        if (dobj.DynamicObjectName != ChoDynamicObject.DefaultName)
                        {
                            if (valueNamePrefix != null && dobj.DynamicObjectName.StartsWith(valueNamePrefix))
                            {
                                var indexTxt = dobj.DynamicObjectName.Substring(valueNamePrefix.Length);
                                if (long.TryParse(indexTxt, out long index1))
                                {
                                }
                                else
                                    key = $"{key}{nestedKeySeparator}{dobj.DynamicObjectName}";
                            }
                            else
                                key = $"{key}{nestedKeySeparator}{dobj.DynamicObjectName}";
                        }
                    }

                    foreach (var kvp1 in Flatten(item as IDictionary<string, object>, key, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                        yield return kvp1;
                }
                else if (item is IDictionary<string, object>)
                {
                    foreach (var kvp1 in Flatten(item as IDictionary<string, object>, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                        yield return kvp1;
                }
                else if (item is IDictionary)
                {
                    foreach (var kvp1 in Flatten(item as IDictionary, "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator), nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                        yield return kvp1;
                }
                else if (item is IList)
                {
                    string akey = "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator);
                    foreach (var kvp1 in Flatten(item as IList, akey, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator,
                        ignoreDictionaryFieldPrefix, valueNamePrefix))
                        yield return kvp1;
                }
                else
                {
                    string akey = null; // "{0}{2}{1}".FormatString(key, index++, nestedKeySeparator);
                    if (arrayIndexSeparator == null)
                    {
                        switch (ChoETLSettings.ArrayBracketNotation)
                        {
                            case ChoArrayBracketNotation.Square:
                                arrayIndexSeparator = '[';
                                arrayEndIndexSeparator = ']';
                                break;
                            case ChoArrayBracketNotation.Parenthesis:
                                arrayIndexSeparator = '(';
                                arrayEndIndexSeparator = ')';
                                break;
                            default:
                                arrayIndexSeparator = nestedKeySeparator; // '_';
                                break;
                        }
                    }
                    if (arrayEndIndexSeparator == null)
                        akey = "{0}{2}{1}".FormatString(key, index++, arrayIndexSeparator);
                    else
                        akey = "{0}{2}{1}{3}".FormatString(key, index++, arrayIndexSeparator, arrayEndIndexSeparator);
                    yield return new KeyValuePair<string, object>(akey, item);
                }
            }

        }
        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary dict, string key = null, char? nestedKeySeparator = null, char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null)
        {
            if (dict == null)
                yield break;

            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            foreach (var dkey in dict.Keys)
            {
                string dKey = dkey.ToNString();
                if (dict[dKey] is IDictionary<string, object>)
                {
                    string lkey = null;
                    if (dict[key] is ChoDynamicObject dobj)
                    {
                        if (ignoreDictionaryFieldPrefix)
                        {
                            lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                        }
                        else
                        {
                            lkey = key == null ? dKey : "{0}{2}{1}{2}{3}".FormatString(key, dKey, nestedKeySeparator, dobj.DynamicObjectName);
                        }
                    }
                    else
                        lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);

                    //lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                    if (true) //!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary<string, object>, lkey, nestedKeySeparator, arrayIndexSeparator,
                            arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                            yield return tuple;
                    }
                    //else
                    //{
                    //    foreach (var tuple in Flatten(dict[key] as IDictionary<string, object>, null, nestedKeySeparator, arrayIndexSeparator, 
                    //        arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                    //        yield return tuple;
                    //}
                }
                else if (dict[key] is IDictionary)
                {
                    string lkey = null;
                    if (dict[key] is ChoDynamicObject dobj)
                    {
                        if (ignoreDictionaryFieldPrefix)
                        {
                            lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                        }
                        else
                        {
                            lkey = key == null ? dKey : "{0}{2}{1}{2}{3}".FormatString(key, dKey, nestedKeySeparator, dobj.DynamicObjectName);
                        }
                    }
                    else
                        lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);

                    //                    lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator);
                    if (true) //!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(dict[key] as IDictionary, lkey, nestedKeySeparator, arrayIndexSeparator,
                            arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                            yield return tuple;
                    }
                    else
                    {
                        //foreach (var tuple in Flatten(dict[key] as IDictionary, null, nestedKeySeparator, arrayIndexSeparator, 
                        //    arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix, true))
                        //    yield return tuple;
                    }
                }
                else if (dict[key] is IList)
                {
                    var lkey = key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                    switch (ChoETLSettings.ArrayBracketNotation)
                    {
                        case ChoArrayBracketNotation.Square:
                            lkey = key == null ? dKey : "{0}{2}[{1}]".FormatString(key, dKey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                            break;
                        case ChoArrayBracketNotation.Parenthesis:
                            lkey = key == null ? dKey : "{0}{2}({1})".FormatString(key, dKey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                            break;
                    }

                    foreach (var tuple in Flatten(dict[key] as IList, lkey, nestedKeySeparator, arrayIndexSeparator, null,
                        ignoreDictionaryFieldPrefix, valueNamePrefix /*, true */))
                        yield return tuple;
                }
                else if (dict[key] == null || dict[key].GetType().IsSimple())
                    yield return new KeyValuePair<string, object>(key == null ? dKey.ToString() : "{0}{2}{1}".FormatString(key, dKey.ToString(), nestedKeySeparator), dict[key]);
                else
                {
                    foreach (var tuple in Flatten(dict[key].ToDynamicObject() as IDictionary<string, object>, key == null ? dKey : "{0}{2}{1}".FormatString(key, dKey, nestedKeySeparator), nestedKeySeparator,
                        arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                        yield return tuple;
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, object>> Flatten(this IDictionary<string, object> dict, string key = null,
            char? nestedKeySeparator = null, char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null
            )
        {
            if (dict == null)
                yield break;

            nestedKeySeparator = nestedKeySeparator == null ? ChoETLSettings.NestedKeySeparator : nestedKeySeparator;
            foreach (var kvp in dict)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    string lkey = null;

                    if (kvp.Value is ChoDynamicObject dobj)
                    {
                        if (ignoreDictionaryFieldPrefix)
                        {
                            //if (dobj.DynamicObjectName == kvp.Key)
                            //    lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                            //else
                            lkey = key; // == null ? kvp.Key : key;
                        }
                        else
                        {
                            lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                            //lkey = key == null ? kvp.Key : "{0}{2}{1}{2}{3}".FormatString(key, kvp.Key, nestedKeySeparator, dobj.DynamicObjectName);
                        }
                    }
                    else
                    {
                        if (ignoreDictionaryFieldPrefix)
                            lkey = null;
                        else
                            lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    }

                    if (((IDictionary<string, object>)kvp.Value).Count == 0)
                        yield return new KeyValuePair<string, object>(lkey, null);

                    if (true) //!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary<string, object>, lkey, nestedKeySeparator,
                            arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                            yield return tuple;
                    }
                    else
                    {
                        //foreach (var tuple in Flatten(kvp.Value as IDictionary<string, object>, null, nestedKeySeparator, 
                        //    arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                        //    yield return tuple;
                    }
                }
                else if (kvp.Value is IDictionary)
                {
                    string lkey = null;
                    if (kvp.Value is ChoDynamicObject dobj)
                    {
                        if (ignoreDictionaryFieldPrefix)
                        {
                            lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                        }
                        else
                        {
                            lkey = key == null ? kvp.Key : "{0}{2}{1}{2}{3}".FormatString(key, kvp.Key, nestedKeySeparator, dobj.DynamicObjectName);
                        }
                    }
                    else
                        lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    if (true) //!ignoreDictionaryFieldPrefix)
                    {
                        foreach (var tuple in Flatten(kvp.Value as IDictionary, lkey, nestedKeySeparator, arrayIndexSeparator,
                            arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                            yield return tuple;
                    }
                    else
                    {
                        //foreach (var tuple in Flatten(kvp.Value as IDictionary, null, nestedKeySeparator, arrayIndexSeparator, 
                        //    arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix, true))
                        //    yield return tuple;
                    }
                }
                else if (kvp.Value is IList)
                {
                    string lkey = null;
                    switch (ChoETLSettings.ArrayBracketNotation)
                    {
                        case ChoArrayBracketNotation.Square:
                            lkey = key == null ? kvp.Key : "{0}{2}[{1}]".FormatString(key, kvp.Key, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                            break;
                        case ChoArrayBracketNotation.Parenthesis:
                            lkey = key == null ? kvp.Key : "{0}{2}({1})".FormatString(key, kvp.Key, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                            break;
                        default:
                            lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value);
                            break;
                    }
                    if (ignoreDictionaryFieldPrefix)
                    {
                        //lkey = key;
                    }
                    else
                    {
                        lkey = key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator);
                    }

                    foreach (var tuple in Flatten(kvp.Value as IList, lkey, arrayIndexSeparator == null ? nestedKeySeparator : arrayIndexSeparator.Value, arrayIndexSeparator,
                        arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
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
                    if (ignoreDictionaryFieldPrefix)
                        yield return new KeyValuePair<string, object>(kvp.Key.ToNString(), kvp.Value);
                    else
                        yield return new KeyValuePair<string, object>(key == null ? kvp.Key.ToString() : "{0}{2}{1}".FormatString(key, kvp.Key.ToString(), nestedKeySeparator), kvp.Value);
                }
                else
                {
                    if (key == null)
                    {
                        yield return new KeyValuePair<string, object>("Key", kvp.Key);
                        if (kvp.Value != null)
                        {
                            if (!kvp.Value.GetType().IsSimple())
                            {
                                foreach (var tuple in Flatten(kvp.Value.ToDynamicObject() as IDictionary<string, object>, null, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator,
                                    ignoreDictionaryFieldPrefix, valueNamePrefix))
                                    yield return tuple;
                            }
                        }
                    }
                    else
                    {
                        if (kvp.Value != null)
                        {
                            if (!kvp.Value.GetType().IsSimple())
                            {
                                foreach (var tuple in Flatten(kvp.Value.ToDynamicObject() as IDictionary<string, object>, key == null ? kvp.Key : "{0}{2}{1}".FormatString(key, kvp.Key, nestedKeySeparator), nestedKeySeparator,
                                    arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix, valueNamePrefix))
                                    yield return tuple;
                            }
                        }
                    }
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
