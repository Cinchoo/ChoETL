using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace ChoETL
{
    public static class ChoUtility
    {
        private const char StartSeparator = '%';
        private const char EndSeparator = '%';
        private const char FormatSeparator = '^';
        private static readonly XmlWriterSettings _xws = new XmlWriterSettings() { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Auto, Indent = true };

        static ChoUtility()
        {
            if (!ChoETLFrxBootstrap.IsSandboxEnvironment)
                _Initialize();
        }

        private static void _Initialize()
        {
            ChoMetadataTypesRegister.Init();
            TypeDescriptor.AddProvider(new ChoExpandoObjectTypeDescriptionProvider(), typeof(ExpandoObject));
        }

        public static void Init()
        {

        }

        private static readonly object _xmlSerializersLock = new object();
        private static readonly Dictionary<string, XmlSerializer> _xmlSerializers = new Dictionary<string, XmlSerializer>();
        public static bool HasXmlSerializer(string elementName, Type type)
        {
            ChoGuard.ArgumentNotNull(elementName, nameof(elementName));
            ChoGuard.ArgumentNotNull(type, nameof(type));

            lock (_xmlSerializersLock)
            {
                string key = GetXmlSerializerKey(elementName, type);
                return _xmlSerializers.ContainsKey(key);
            }
        }
        private static string GetXmlSerializerKey(string elementName, Type type)
        {
            return $"{elementName}+{type.FullName}";
        }
        public static XmlSerializer GetXmlSerializer(string elementName, Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(elementName, nameof(elementName));
            ChoGuard.ArgumentNotNull(type, nameof(type));

            string key = GetXmlSerializerKey(elementName, type);
            if (_xmlSerializers.ContainsKey(key))
                return _xmlSerializers[key];

            lock (_xmlSerializersLock)
            {
                if (!_xmlSerializers.ContainsKey(key))
                {
                    XmlSerializer serializer = overrides != null ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
                    if (unknownNode != null)
                        serializer.UnknownNode += unknownNode;
                    _xmlSerializers.Add(key, serializer);
                }

                return _xmlSerializers[key];
            }
        }

        private static readonly XmlAttributeOverrides DefaultOverrides = new XmlAttributeOverrides();
        private static readonly Dictionary<Type, Dictionary<XmlAttributeOverrides, XmlSerializer>> _xmlSerializersWithOverrides = new Dictionary<Type, Dictionary<XmlAttributeOverrides, XmlSerializer>>();
        public static bool HasXmlSerializerWithOverrides(Type type, XmlAttributeOverrides overrides)
        {
            lock (_xmlSerializersLock)
            {
                if (overrides == null)
                    overrides = DefaultOverrides;
                if (_xmlSerializersWithOverrides.ContainsKey(type))
                {
                    return _xmlSerializersWithOverrides[type].ContainsKey(overrides);
                }
            }

            return false;
        }
        public static XmlSerializer GetXmlSerializerWithOverrides(Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(type, nameof(type));
            if (overrides == null)
                overrides = DefaultOverrides;

            if (_xmlSerializersWithOverrides.ContainsKey(type))
            {
                if (_xmlSerializersWithOverrides[type].ContainsKey(overrides))
                    return _xmlSerializersWithOverrides[type][overrides];
            }

            lock (_xmlSerializersLock)
            {
                if (!_xmlSerializersWithOverrides.ContainsKey(type))
                {
                    _xmlSerializersWithOverrides.Add(type, new Dictionary<XmlAttributeOverrides, XmlSerializer>());
                    XmlSerializer serializer = overrides != DefaultOverrides ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
                    if (unknownNode != null)
                        serializer.UnknownNode += unknownNode;
                    _xmlSerializersWithOverrides[type].Add(overrides, serializer);
                }
                else
                {
                    if (!_xmlSerializersWithOverrides[type].ContainsKey(overrides))
                    {
                        XmlSerializer serializer = overrides != DefaultOverrides ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
                        if (unknownNode != null)
                            serializer.UnknownNode += unknownNode;
                        _xmlSerializersWithOverrides[type].Add(overrides, serializer);
                    }
                }
                return _xmlSerializersWithOverrides[type][overrides];
            }
        }

        public static IEnumerable<T> ZipEx<T1, T2, T>(this IEnumerable<T1> first,
                                    IEnumerable<T2> second, Func<T1, T2, T> operation)
        {
            using (var iter1 = first.GetEnumerator())
            using (var iter2 = second.GetEnumerator())
            {
                while (iter1.MoveNext())
                {
                    if (iter2.MoveNext())
                    {
                        yield return operation(iter1.Current, iter2.Current);
                    }
                    else
                    {
                        yield return operation(iter1.Current, default(T2));
                    }
                }
                while (iter2.MoveNext())
                {
                    yield return operation(default(T1), iter2.Current);
                }
            }
        }

        public static IDictionary<V, K> Transpose<K, V>(
            this IDictionary<K, IEnumerable<V>> source,
            Func<IEnumerable<K>, K> selector = null)
        {
            if (selector != null)
                return (from kvp in source
                        from V value in kvp.Value
                        group kvp.Key by value)
                            .ToDictionary(grp => grp.Key, grp => selector(grp));
            else
                return source
                    .SelectMany(e => e.Value.Select(s => new { Key = s, Value = e.Key }))
                    .ToDictionary(x => x.Key, x => x.Value);
        }

        public static IDictionary<V, K> Transpose<K, V>(this IDictionary<K, V> source)
        {
            return source
                .ToDictionary(x => x.Value, x => x.Key);
        }

        public static IEnumerable<dynamic> ToDynamic(this IDictionary source)
        {
            yield return ChoDynamicObject.FromDictionary(source);
        }

        //public static IEnumerable<object[]> Transpose<K, V>(this IDictionary<K, V> dict)
        //{
        //    yield return dict.Keys.Cast<object>().ToArray();
        //    yield return dict.Values.Cast<object>().ToArray();
        //}
        //public static IEnumerable<object[]> Transpose<K, V>(this IEnumerable<IDictionary<K, V>> dicts)
        //{
        //    bool first = true;
        //    foreach (var dict in dicts)
        //    {
        //        if (first)
        //        {
        //            first = false;
        //            yield return dict.Keys.Cast<object>().ToArray();
        //        }
        //        yield return dict.Values.Cast<object>().ToArray();
        //    }
        //}


        //public static ChoDynamicObject Transpose(this ChoDynamicObject dict)
        //{
        //    var dict1 = Transpose((IDictionary<string, object>)dict);
        //    return new ChoDynamicObject(dict1.GroupBy(g => g.Key.ToNString(), StringComparer.OrdinalIgnoreCase).ToDictionary(kvp => kvp.Key.ToNString(), kvp => (object)kvp.Last(), StringComparer.OrdinalIgnoreCase));
        //}

        public static IEnumerable<IDictionary<string, object>> Pivot(this IEnumerable<object> dicts)
        {
            foreach (var d in Pivot(dicts.OfType<IDictionary<string, object>>()))
            {
                yield return d;
            }
        }

        private static IEnumerable<IDictionary<string, object>> Pivot(this IEnumerable<IDictionary<string, object>> dicts)
        {
            return dicts.Select(r1 =>
                ToDictionary(r1.First().Value as IList, r1.Skip(1).First().Value as IList));
        }

        public static IDictionary<string, object> ToDictionary(IList keys, IList values)
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            if (keys != null && values != null)
            {
                foreach (var kvp in keys.OfType<string>().Select((k, i) => new KeyValuePair<int, string>(i, k)))
                {
                    if (dict.ContainsKey(kvp.Value))
                        continue;
                    if (kvp.Key < values.Count)
                        dict.Add(kvp.Value, values[kvp.Key]);
                    else
                        dict.Add(kvp.Value, null);
                }
            }
            return dict;
        }

        public static IEnumerable<ChoDynamicObject> Transpose(this IEnumerable<object> dicts, bool treatFirstItemAsHeader = true)
        {
            return Transpose(dicts.OfType<IDictionary<string, object>>(), treatFirstItemAsHeader).Select(d =>
            {
                var dict = new ChoDynamicObject(d);
                if (d != null && d.Values.First().ToNString() == "#NULL#")
                    dict.IsHeaderOnlyObject = true;

                return dict;
            });
        }

        public static IEnumerable<IDictionary<string, object>> Transpose(this IEnumerable<IDictionary<string, object>> dicts, bool treatFirstItemAsHeader = true)
        {
            var dictsArray = dicts.ToArray();

            var first = dictsArray.FirstOrDefault();
            if (first == null)
                yield break;

            int counter = 0;
            List<string> keys = new List<string>();
            string firstColumn = first.ElementAt(0).Key;
            foreach (var x in dictsArray)
            {
                if (treatFirstItemAsHeader)
                    keys.Add(x.First().Value.ToNString());
                else
                    keys.Add("Column{0}".FormatString(++counter));
            }

            int length = first.Count;
            int startIndex = treatFirstItemAsHeader ? 1 : 0;
            if (startIndex >= length)
            {
                Dictionary<string, object> obj = new Dictionary<string, object>();
                foreach (var x in keys)
                {
                    obj.AddOrUpdate(x, "#NULL#");
                }
                yield return obj;
            }
            else
            {
                for (int index = startIndex; index < length; index++)
                {
                    Dictionary<string, object> obj = new Dictionary<string, object>();
                    int keyIndex = 0;
                    foreach (var x in dictsArray)
                    {
                        obj.AddOrUpdate(keys[keyIndex++], x.ElementAt(index).Value);
                    }
                    yield return obj;
                }
            }
        }

        public static List<T[]> Transpose<T>(this IDictionary<T, T[]> dict)
        {
            List<T[]> ret = new List<T[]>();

            ret.Add(dict.Keys.ToArray());
            List<T> dest = new List<T>();
            for (int i = 0; i < dict.Keys.Count; i++)
            {
                dest.Clear();
                foreach (var value in dict.Values)
                {
                    dest.Add(i < value.Length ? value[i] : default(T));
                }
                ret.Add(dest.ToArray());

            }
            return ret;
        }

        public static IEnumerable<ChoDynamicObject> ExpandToObjects(this IEnumerable<object> list, Func<string, string> fieldMap = null, Func<string, object, object> converter = null)
        {
            return ExpandToObjects<ChoDynamicObject>(list, fieldMap, converter);
        }
        public static IEnumerable<T> ExpandToObjects<T>(this IEnumerable<object> list, Func<string, string> fieldMap = null)
            where T : class, new()
        {
            return ExpandToObjects<T>(list, fieldMap, null);
        }

        private static IEnumerable<T> ExpandToObjects<T>(IEnumerable<object> list, Func<string, string> fieldMap = null, Func<string, object, object> converter = null)
        {
            bool firstItem = true;
            string[] keys = null;
            Dictionary<string, PropertyDescriptor> pds = new Dictionary<string, PropertyDescriptor>();
            PropertyDescriptor pd = null;
            int count = 1;
            bool isSourceDynamic = true;
            IDictionary<string, object> dict = null;
            foreach (var item in list) //.OfType<IDictionary<string, object>>())
            {
                dict = item as IDictionary<string, object>;
                if (firstItem)
                {
                    if (item is IDictionary<string, object>)
                        isSourceDynamic = true;
                    else
                        isSourceDynamic = false;

                    if (fieldMap == null)
                    {
                        fieldMap = new Func<string, string>(fn =>
                        {
                            if (item is Dictionary<string, object>)
                                return fn;
                            else
                            {
                                IDictionary<string, string> dictx = ChoTypeDescriptor.GetProperties(typeof(T)).ToDictionary(pi => pi.Name, pi =>
                                {
                                    var fm = pi.Attributes.OfType<ChoFieldMapAttribute>().FirstOrDefault();
                                    if (fm == null || fm.Name.IsNullOrWhiteSpace())
                                        return pi.Name;
                                    else
                                        return fm.Name;
                                });
                                return dictx.ContainsKey(fn) ? dictx[fn] : fn;
                            }
                        });
                    }

                    if (!typeof(IDictionary<string, object>).IsAssignableFrom(typeof(T)))
                        pds = ChoTypeDescriptor.GetProperties(typeof(T)).ToDictionary(kvp => fieldMap(kvp.Name), StringComparer.CurrentCultureIgnoreCase);

                    if (isSourceDynamic)
                    {
                        keys = dict.Keys.ToArray();
                        firstItem = false;

                        count = dict.Values.Where(i => i is IList).Select(i => ((IList)i).Count).Max();
                        if (count <= 0) count = 1;
                    }
                    else
                    {
                        keys = ChoTypeDescriptor.GetProperties(item.GetType()).Select(pi => pi.Name).ToArray();
                        firstItem = false;

                        count = keys.Select(pn => ChoType.GetPropertyValue(item, pn)).Where(i => i is IList).Select(i => ((IList)i).Count).Max();
                        if (count <= 0) count = 1;
                    }
                }

                object value = null;
                int index = 0;

                while (index < count)
                {
                    T rec = ChoActivator.CreateInstance<T>();
                    foreach (var key in keys)
                    {
                        if (isSourceDynamic)
                        {
                            if (!dict.ContainsKey(key, true, Thread.CurrentThread.CurrentCulture))
                                continue;
                            value = dict[key];
                        }
                        else
                        {
                            value = ChoType.GetPropertyValue(item, key);
                        }

                        if (value == null)
                            continue;

                        if (rec is IDictionary<string, object>)
                        {
                            var destdict = rec as IDictionary<string, object>;
                            if (value is IList)
                            {
                                var list1 = value as IList;
                                if (index < list1.Count)
                                {
                                    if (!destdict.ContainsKey(key))
                                        destdict.Add(key, list1[index]);
                                    else
                                        destdict[key] = list1[index];
                                }
                            }
                            else
                            {
                                if (!destdict.ContainsKey(key))
                                    destdict.Add(key, value);
                                else
                                    destdict[key] = value;
                            }

                            destdict[key] = converter != null ? converter(key, destdict[key]) : destdict[key];
                        }
                        else
                        {
                            if (!pds.ContainsKey(key))
                                continue;
                            pd = pds[key];

                            if (value is IList)
                            {
                                var list1 = value as IList;
                                if (index < list1.Count)
                                {
                                    ChoType.ConvertNSetPropertyValue(rec, pd.Name, list1[index]);
                                }
                            }
                            else
                            {
                                ChoType.SetPropertyValue(rec, pd.Name, value);
                            }
                        }
                    }
                    yield return rec;
                    index++;
                }
            }
        }

        public static object FirstOrDefaultEx(this object value, object defaultValue = null)
        {
            return FirstOrDefaultEx<object>(value, defaultValue);
        }

        public static T FirstOrDefaultEx<T>(this object value, T defaultValue = default(T))
        {
            if (value == null) return defaultValue;
            if (!(value is string) && value is IEnumerable)
            {
                foreach (object x in (IEnumerable)value)
                {
                    value = x;
                    break;
                }
                return value.CastTo<T>(defaultValue);
            }
            else
                return value.CastTo<T>(defaultValue);
        }

        public static void Write(this FileStream sr, string value, Encoding encoding = null)
        {
            ChoGuard.ArgumentNotNull(sr, "FileStream");

            if (value.IsNullOrEmpty())
                return;

            if (encoding == null)
                encoding = Encoding.ASCII;

            byte[] byteData = null;
            byteData = encoding.GetBytes(value);
            sr.Write(byteData, 0, byteData.Length);
        }

        public static dynamic ToDynamic(this object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            return expando as ExpandoObject;
        }

        public static dynamic ToDynamicObject(this object src,
            bool shallowDynamic = false,
            Func<IDictionary<string, object>> dynamicFactory = null
            )
        {
            return ToDynamicObject(src, shallowDynamic, dynamicFactory, new HashSet<object>());
        }

        private static dynamic ToDynamicObject(this object src,
            bool shallowDynamic = false,
            Func<IDictionary<string, object>> dynamicFactory = null,
            HashSet<object> objectGraph = null
            )
        {
            if (src == null) return new ChoDynamicObject();
            if (objectGraph.Contains(src)) return null;
            objectGraph.Add(src);

            //if (src.GetType().IsSimple())
            //    return src;
            if (src is ExpandoObject || src is IDictionary<string, object>)
                return src;
            if (src is DynamicObject)
                return ChoExpandoObjectEx.ToExpandoObject(src as DynamicObject);
            if (src.GetType().IsSimple())
            {
                IDictionary<string, object> expando1 = new ExpandoObject();
                expando1.Add("Value", src);
                return expando1;
            }

            object propValue = null;
            IDictionary<string, object> expando = dynamicFactory == null ? new ChoDynamicObject() : dynamicFactory();
            if (expando == null) expando = new ChoDynamicObject();

            //string valuePrefix = "Value";
            //string separator = "_";
            //if (src is IList)
            //{
            //    Dictionary<string, object> dict = new Dictionary<string, object>();
            //    int index = 0;
            //    foreach (var rec in (IList)src)
            //    {
            //        var key = $"{valuePrefix}{separator}{index++}";
            //        if (rec == null)
            //            dict.Add(key, null);
            //        else if (rec.GetType().IsSimple())
            //            dict.Add(key, rec);
            //        else
            //            dict.Add(key, ToDynamicObject(rec, shallowDynamic, dynamicFactory, objectGraph));
            //    }
            //    return dict;
            //}
            if (src is IList)
            {
                List<object> list = new List<object>();
                foreach (var rec in (IList)src)
                {
                    if (rec == null)
                        list.Add(rec);
                    else if (rec.GetType().IsSimple())
                        list.Add(rec);
                    else
                        list.Add(ToDynamicObject(rec, shallowDynamic, dynamicFactory, objectGraph));
                }
                return list.ToArray();
            }
            else if (src is IDictionary)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (var key in ((IDictionary)src).Keys)
                {
                    string keyObj = null;
                    keyObj = key.ToString();

                    object valueObj = ((IDictionary)src)[key];
                    if (valueObj == null)
                        dict.Add(keyObj, null);
                    if (key.GetType().IsSimple())
                        dict.Add(keyObj, valueObj);
                    else
                        dict.Add(keyObj, ToDynamicObject(valueObj, shallowDynamic, dynamicFactory, objectGraph));
                }
                return dict;
            }
            else
            {
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(src.GetType()))
                {
                    if (pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any()) continue;

                    try
                    {
                        propValue = pd.GetValue(src);

                        if (shallowDynamic)
                            expando.Add(pd.Name, propValue);
                        else
                        {
                            if (propValue == null)
                                expando.Add(pd.Name, propValue);
                            else if (propValue.GetType().IsSimple())
                                expando.Add(pd.Name, propValue);
                            else
                                expando.Add(pd.Name, ToDynamicObject(propValue, shallowDynamic, dynamicFactory, objectGraph));
                        }
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "ToDynamicObject: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(pd), ex.Message));
                    }
                }
            }
            //foreach (MemberInfo memberInfo in ChoType.GetMembers(src.GetType()).Where(m => ChoType.GetAttribute<ChoIgnoreMemberAttribute>(m) == null))
            //{
            //    try
            //    {
            //        expando.Add(memberInfo.Name, ChoType.GetMemberValue(src, memberInfo));
            //    }
            //    catch (Exception ex)
            //    {
            //        ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "ToDynamicObject: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
            //    }
            //}

            return expando; // as ExpandoObject;
        }

        public static void EagerCloneTo(this object src, object dest)
        {
            if (src == null || dest == null)
                return;

            if (src is ExpandoObject)
            {
                var srcDict = (IDictionary<string, object>)src;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(dest.GetType()))
                {
                    try
                    {
                        if (!srcDict.ContainsKey(pd.Name)) continue;
                        pd.SetValue(dest, ChoConvert.ConvertTo(srcDict[pd.Name], pd.PropertyType));
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "Clone: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(pd), ex.Message));
                    }
                }
            }
            else
            {
                Dictionary<string, PropertyDescriptor> destMembers = ChoTypeDescriptor.GetProperties(dest.GetType()).ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(src.GetType()))
                {
                    try
                    {
                        if (!destMembers.ContainsKey(pd.Name)) continue;
                        destMembers[pd.Name].SetValue(dest, ChoConvert.ConvertTo(pd.GetValue(src), destMembers[pd.Name].PropertyType));
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "Clone: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[pd.Name]), ex.Message));
                    }
                }
            }

            //if (src is ExpandoObject)
            //{
            //    var srcDict = (IDictionary<string, object>)src;
            //    foreach (MemberInfo memberInfo in ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoIgnoreMemberAttribute>(m) == null))
            //    {
            //        try
            //        {
            //            if (!srcDict.ContainsKey(memberInfo.Name)) continue;
            //            ChoType.SetMemberValue(dest, memberInfo, srcDict[memberInfo.Name]);
            //        }
            //        catch (Exception ex)
            //        {
            //            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
            //        }
            //    }
            //}
            //else
            //{
            //    Dictionary<string, MemberInfo> destMembers = ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoIgnoreMemberAttribute>(m) == null).ToArray().ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
            //    foreach (MemberInfo memberInfo in ChoType.GetMembers(src.GetType()).Where(m => ChoType.GetAttribute<ChoIgnoreMemberAttribute>(m) == null))
            //    {
            //        try
            //        {
            //            if (!destMembers.ContainsKey(memberInfo.Name)) continue;
            //            ChoType.SetMemberValue(dest, destMembers[memberInfo.Name], ChoType.GetMemberValue(src, memberInfo));
            //        }
            //        catch (Exception ex)
            //        {
            //            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[memberInfo.Name]), ex.Message));
            //        }
            //    }
            //}
        }

        public static void CloneTo<T>(this object src, object dest, Func<string, Type, object> defaultValueCallback = null,
            Func<string, Type, object> fallbackValueCallback = null)
            where T : Attribute
        {
            if (src == null || dest == null)
                return;

            if (src is ExpandoObject || src is IDictionary<string, object>)
            {
                if (dest is ExpandoObject || dest is IDictionary<string, object>)
                {
                    foreach (var kvp in ((IDictionary<string, object>)src))
                    {
                        IDictionary<string, object> dest1 = dest as IDictionary<string, object>;
                        dest1.Add(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    if (dest is IDictionary)
                    {
                        if (!dest.GetType().IsGenericType)
                        {
                            var dest1 = dest as IDictionary;
                            foreach (var key in ((IDictionary<string, object>)src).Keys)
                            {
                                dest1.Add(key, ((IDictionary<string, object>)src)[key]);
                            }
                        }
                        else
                        {
                            var keyType = dest.GetType().GetGenericArguments()[0];
                            var valueType = dest.GetType().GetGenericArguments()[1];

                            var keyConverter = ChoTypeConverter.Global.GetConverter(keyType);
                            var valueConverter = ChoTypeConverter.Global.GetConverter(valueType);

                            var dest1 = dest as IDictionary;
                            foreach (var key in ((IDictionary<string, object>)src).Keys)
                            {
                                dest1.Add(ChoConvert.ConvertTo(key, keyType, null, keyConverter != null ? new object[] { keyConverter } : null, null, null),
                                    ChoConvert.ConvertTo(((IDictionary<string, object>)src)[key], valueType, null, valueConverter != null ? new object[] { valueConverter } : null, null, null));
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, PropertyDescriptor> destMembers = ChoTypeDescriptor.GetProperties<T>(dest.GetType()).ToDictionary(pd => pd.Name, StringComparer.CurrentCultureIgnoreCase);
                        //Set default values to all members
                        if (defaultValueCallback != null)
                        {
                            foreach (string mn in destMembers.Keys)
                            {
                                ChoType.ConvertNSetMemberValue(dest, mn, defaultValueCallback(mn, destMembers[mn].PropertyType));
                            }
                        }

                        foreach (string mn in ((IDictionary<string, object>)src).Keys)
                        {
                            try
                            {
                                if (!destMembers.ContainsKey(mn)) continue;
                                ChoType.ConvertNSetMemberValue(dest, mn, ChoType.GetMemberValue(src, mn));
                            }
                            catch (Exception ex)
                            {
                                if (fallbackValueCallback != null)
                                {
                                    try
                                    {
                                        ChoType.ConvertNSetMemberValue(dest, mn, fallbackValueCallback(mn, destMembers[mn].PropertyType));
                                    }
                                    catch
                                    {
                                        throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                                    }
                                }
                                else
                                    throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                            }

                        }
                    }
                }
            }
            else if (false) //src.GetType().IsDynamicRecord())
            {
                //IChoDynamicRecord srcRec = src as IChoDynamicRecord;
                //if (dest.GetType().IsDynamicRecord())
                //{
                //    //IChoDynamicRecord destRec = dest as IChoDynamicRecord;
                //    //foreach (string fn in srcRec.GetDynamicMemberNames())
                //    //    destRec.SetPropertyValue(fn, srcRec.GetPropertyValue(fn));
                //}
                //else
                //{
                //    Dictionary<string, MemberInfo> destMembers = ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null).ToArray().ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
                //    foreach (MemberInfo memberInfo in destMembers.Values)
                //    {
                //        try
                //        {
                //            ChoType.ConvertNSetMemberValue(dest, memberInfo, srcRec.GetPropertyValue(memberInfo.Name));
                //        }
                //        catch (Exception ex)
                //        {
                //            if (fallbackValueCallback != null)
                //            {
                //                try
                //                {
                //                    ChoType.ConvertNSetMemberValue(dest, memberInfo, fallbackValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                //                }
                //                catch
                //                {
                //                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                //                }
                //            }
                //            else
                //                ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                //        }
                //    }
                //}
            }
            else
            {
                Dictionary<string, PropertyDescriptor> destMembers = ChoTypeDescriptor.GetProperties<T>(dest.GetType()).ToDictionary(pd => pd.Name, StringComparer.CurrentCultureIgnoreCase);
                //Set default values to all members
                if (defaultValueCallback != null)
                {
                    foreach (string mn in destMembers.Keys)
                    {
                        ChoType.ConvertNSetMemberValue(dest, mn, defaultValueCallback(mn, destMembers[mn].PropertyType));
                    }
                }

                Dictionary<string, PropertyDescriptor> srcMembers = ChoTypeDescriptor.GetProperties<T>(src.GetType()).ToDictionary(pd => pd.Name, StringComparer.CurrentCultureIgnoreCase);
                foreach (string mn in srcMembers.Keys)
                {
                    try
                    {
                        if (!destMembers.ContainsKey(mn)) continue;
                        ChoType.ConvertNSetMemberValue(dest, mn, ChoType.GetMemberValue(src, mn));
                    }
                    catch (Exception ex)
                    {
                        if (fallbackValueCallback != null)
                        {
                            try
                            {
                                ChoType.ConvertNSetMemberValue(dest, mn, fallbackValueCallback(mn, destMembers[mn].PropertyType));
                            }
                            catch
                            {
                                throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                            }
                        }
                        else
                            throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                    }
                }
                //foreach (MemberInfo memberInfo in destMembers.Values)
                //{
                //    try
                //    {
                //        if (srcMembers.ContainsKey(memberInfo.Name)) continue;
                //        ChoType.ConvertNSetMemberValue(dest, memberInfo, ChoType.GetMemberValue(dest, memberInfo));
                //    }
                //    catch (Exception ex)
                //    {
                //        if (fallbackValueCallback != null)
                //        {
                //            try
                //            {
                //                ChoType.ConvertNSetMemberValue(dest, memberInfo, fallbackValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                //            }
                //            catch
                //            {
                //                ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(srcMembers[memberInfo.Name]), ex.Message));
                //            }
                //        }
                //        else
                //            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(srcMembers[memberInfo.Name]), ex.Message));
                //    }
                //}
            }
        }

        public static void CloneTo(this object src, object dest, Func<string, Type, object> defaultValueCallback = null,
            Func<string, Type, object> fallbackValueCallback = null)
        {
            if (src == null || dest == null)
                return;

            if (src is ExpandoObject || src is IDictionary<string, object>)
            {
                if (dest is ExpandoObject || dest is IDictionary<string, object>)
                    dest = src;
                else
                {
                    Dictionary<string, PropertyDescriptor> destMembers = ChoTypeDescriptor.GetProperties(dest.GetType()).ToDictionary(pd => pd.Name, StringComparer.CurrentCultureIgnoreCase);
                    //Set default values to all members
                    if (defaultValueCallback != null)
                    {
                        foreach (string mn in destMembers.Keys)
                        {
                            ChoType.ConvertNSetMemberValue(dest, mn, defaultValueCallback(mn, destMembers[mn].PropertyType));
                        }
                    }

                    IDictionary<string, object> srcMembers = src as IDictionary<string, object>;
                    foreach (string mn in srcMembers.Keys)
                    {
                        try
                        {
                            if (!destMembers.ContainsKey(mn)) continue;
                            ChoType.ConvertNSetMemberValue(dest, mn, srcMembers[mn]);
                        }
                        catch (Exception ex)
                        {
                            if (fallbackValueCallback != null)
                            {
                                try
                                {
                                    ChoType.ConvertNSetMemberValue(dest, mn, fallbackValueCallback(mn, destMembers[mn].PropertyType));
                                }
                                catch
                                {
                                    throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                                }
                            }
                            else
                                throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[mn]), ex.Message));
                        }

                    }
                }
            }
            else if (false) //src.GetType().IsDynamicRecord())
            {
                //IChoDynamicRecord srcRec = src as IChoDynamicRecord;
                //if (dest.GetType().IsDynamicRecord())
                //{
                //    //IChoDynamicRecord destRec = dest as IChoDynamicRecord;
                //    //foreach (string fn in srcRec.GetDynamicMemberNames())
                //    //    destRec.SetPropertyValue(fn, srcRec.GetPropertyValue(fn));
                //}
                //else
                //{
                //    Dictionary<string, MemberInfo> destMembers = ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null).ToArray().ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
                //    foreach (MemberInfo memberInfo in destMembers.Values)
                //    {
                //        try
                //        {
                //            ChoType.ConvertNSetMemberValue(dest, memberInfo, srcRec.GetPropertyValue(memberInfo.Name));
                //        }
                //        catch (Exception ex)
                //        {
                //            if (fallbackValueCallback != null)
                //            {
                //                try
                //                {
                //                    ChoType.ConvertNSetMemberValue(dest, memberInfo, fallbackValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                //                }
                //                catch
                //                {
                //                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                //                }
                //            }
                //            else
                //                ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                //        }
                //    }
                //}
            }
            else
            {
                Dictionary<string, MemberInfo> destMembers = ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null).ToArray().ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
                //Set default values to all members
                if (defaultValueCallback != null)
                {
                    foreach (MemberInfo memberInfo in destMembers.Values)
                    {
                        ChoType.ConvertNSetMemberValue(dest, memberInfo, defaultValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                    }
                }

                Dictionary<string, MemberInfo> srcMembers = ChoType.GetMembers(src.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null).ToArray().ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
                foreach (MemberInfo memberInfo in srcMembers.Values)
                {
                    try
                    {
                        if (!destMembers.ContainsKey(memberInfo.Name)) continue;
                        ChoType.ConvertNSetMemberValue(dest, destMembers[memberInfo.Name], ChoType.GetMemberValue(src, memberInfo));
                    }
                    catch (Exception ex)
                    {
                        if (fallbackValueCallback != null)
                        {
                            try
                            {
                                ChoType.ConvertNSetMemberValue(dest, destMembers[memberInfo.Name], fallbackValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                            }
                            catch
                            {
                                throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[memberInfo.Name]), ex.Message));
                            }
                        }
                        else
                            throw new ApplicationException("Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(destMembers[memberInfo.Name]), ex.Message));
                    }
                }
                //foreach (MemberInfo memberInfo in destMembers.Values)
                //{
                //    try
                //    {
                //        if (srcMembers.ContainsKey(memberInfo.Name)) continue;
                //        ChoType.ConvertNSetMemberValue(dest, memberInfo, ChoType.GetMemberValue(dest, memberInfo));
                //    }
                //    catch (Exception ex)
                //    {
                //        if (fallbackValueCallback != null)
                //        {
                //            try
                //            {
                //                ChoType.ConvertNSetMemberValue(dest, memberInfo, fallbackValueCallback(memberInfo.Name, ChoType.GetMemberType(memberInfo)));
                //            }
                //            catch
                //            {
                //                ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(srcMembers[memberInfo.Name]), ex.Message));
                //            }
                //        }
                //        else
                //            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(srcMembers[memberInfo.Name]), ex.Message));
                //    }
                //}
            }
        }

        public static MemberInfo[] GetMembers<T>(this Type type)
            where T : Attribute
        {
            if (type == null) return null;
            return ChoType.GetMembers(type).Where(m => ChoType.GetAttribute<T>(m) != null).ToArray();
        }

        public static T ParseEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static object GetValueFor(this object array, string key, int? index = null)
        {
            return GetValueFor<object>(array, key, index);
        }

        public static T GetValueFor<T>(this object array, object key, int? index = null, T defaultValue = default(T))
        {
            Type type = typeof(T).GetUnderlyingType();
            if (array is IDictionary)
            {
                object value = null;
                IDictionary dict = array as IDictionary;

                if (index != null)
                {
                    var lkey = dict.Keys.GetValueAt(index.Value);
                    if (lkey != null)
                        key = lkey;
                }

                if (key != null && dict.Contains(key))
                {
                    value = dict[key];
                }

                if (value != null)
                {
                    try
                    {
                        return ParseValue(type, value, defaultValue);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
            else if (index != null)
            {
                return GetValueAt(array, index.Value, defaultValue);
            }

            return defaultValue;
        }

        private static T ParseValue<T>(Type type, object value, T defaultValue)
        {
            if (type.IsEnum)
            {
                if (Enum.IsDefined(type, value.ToString()))
                    return (T)Enum.Parse(type, value.ToString());
                else
                    return defaultValue;
            }
            return (T)Convert.ChangeType(value, type);
        }

        public static object GetValueAt(this object array, int index)
        {
            return GetValueAt<object>(array, index);
        }
        public static T GetValueAt<T>(this object array, int index, T defaultValue = default(T))
        {
            Type type = typeof(T).GetUnderlyingType();
            if (array is IDictionary)
            {
                return GetValueFor(array, null, index, defaultValue);
            }
            else if (array is IList)
            {
                if (index < ((IList)array).Count)
                {
                    try
                    {
                        return ParseValue(type, ((IList)array)[index], defaultValue);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                else
                    return defaultValue;
            }
            else if (array is ICollection)
            {
                var count = ((ICollection)array).Count;
                if (index < count)
                {
                    try
                    {
                        int count1 = 0;
                        object val = null;
                        foreach (var rec in (ICollection)array)
                        {
                            val = rec;
                            if (count1 == index)
                                break;
                            count1++;
                        }

                        return ParseValue(type, val, defaultValue);
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
                else
                    return defaultValue;
            }
            else
                return array != null && array is T ? (T)array : defaultValue;
        }

        public static Type GetTypeFromXmlSectionNode(this XmlNode sectionNode)
        {
            if (sectionNode == null)
                throw new ArgumentNullException("sectionNode");

            return GetTypeFromXmlSectionName(sectionNode.Name);
        }

        public static Type GetTypeFromXmlSectionName(this string sectionName)
        {
            if (sectionName == null || sectionName.Length == 0)
                throw new ArgumentNullException("sectionName");

            Type[] types = ChoType.GetTypes(typeof(XmlRootAttribute));
            //Trace.TraceInformation("SectionName: {0}, XmlRootAttribute Types: {1}".FormatString(sectionName, types != null ? types.Length : 0));

            if (types == null || types.Length == 0) return null;

            foreach (Type type in types)
            {
                if (type == null) continue;

                XmlRootAttribute xmlRootAttribute = ChoType.GetAttribute(type, typeof(XmlRootAttribute)) as XmlRootAttribute;
                if (xmlRootAttribute == null) continue;

                if (xmlRootAttribute.ElementName == sectionName)
                    return type;
            }

            return null;
        }

        public static string GetName(this XmlNode xmlNode)
        {
            XPathNavigator navigator = xmlNode.CreateNavigator();

            return (string)navigator.Evaluate("string(@name)");
        }

        public static string GetNodeName(this XmlNode xmlNode)
        {
            XPathNavigator navigator = xmlNode.CreateNavigator();

            string nodeName = (string)navigator.Evaluate("string(@name)");
            return nodeName.IsNullOrWhiteSpace() ? xmlNode.Name : nodeName;
        }

        public static T ToObject<T>(this XmlNode node, XmlNodeEventHandler unknownNode = null)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (unknownNode != null)
                serializer.UnknownNode += unknownNode;
            return (T)serializer.Deserialize(new XmlNodeReader(node));
        }

        public static object ToObject(this XmlNode node, Type type)
        {
            return ToObject(node, type, null);
        }


        public static object ToObject(this XmlNode node, Type type, XmlAttributeOverrides overrides, XmlNodeEventHandler unknownNode = null)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            if (type == null)
                throw new ArgumentException("Type");

            //XmlSerializer serializer = overrides != null ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
            XmlSerializer serializer = ChoUtility.GetXmlSerializerWithOverrides(type, overrides);
            if (unknownNode != null)
                serializer.UnknownNode += unknownNode;
            return serializer.Deserialize(new XmlNodeReader(node));
        }

        public static StreamReader ToStreamReader(this string source)
        {
            MemoryStream ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            sw.Write(source);
            sw.Flush();
            ms.Position = 0;

            return new StreamReader(ms);
        }

        public static string ReadToEnd(this Stream ms)
        {
            ChoGuard.ArgumentNotNull(ms, "Stream");

            ms.Flush();
            ms.Position = 0;
            return new StreamReader(ms).ReadToEnd();
        }

        public static IEnumerable<T> ConvertToEnumerable<T>(this T source)
        {
            if (source == null)
                yield break;

            yield return source;
        }

        public static IEnumerable<object> ConvertToEnumerable(params object[] source)
        {
            if (source == null)
                yield break;

            foreach (var src in source)
                yield return src;
        }

        public static IEnumerable<T> AsTypedEnumerable<T>(this IEnumerable source, T firstItem = default(T))
        {
            // Note: firstItem parameter is unused and is just for resolving type of T
            foreach (var item in source)
            {
                yield return (T)item;
            }
        }

        public static IEnumerable<T> Unfold<T>(this IEnumerable<IEnumerable<T>> e)
        {
            ChoGuard.ArgumentNotNull(e, "Enumeable");
            foreach (var r in e)
            {
                foreach (var i in r)
                {
                    yield return i;
                }
            }
        }

        public static string ToNString<T>(this T target, T defaultValue = default(T))
        {
            return target == null ? defaultValue == null ? String.Empty : defaultValue.ToString() : target.ToString();
        }

        public static string SerializeToText<T>(this T obj)
        {
            var serializer = new DataContractSerializer(obj.GetType());
            using (var writer = new StringWriter())
            using (var stm = new XmlTextWriter(writer))
            {
                serializer.WriteObject(stm, obj);
                return writer.ToString();
            }
        }
        public static T DeserializeFromText<T>(this string serialized)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using (var reader = new StringReader(serialized))
            using (var stm = new XmlTextReader(reader))
            {
                return (T)serializer.ReadObject(stm);
            }
        }

        public static IEnumerable<T> CastEnumerable<T>(this IEnumerable @this)
        {
            if (@this == null)
                yield break;

            foreach (object item in @this)
                yield return (T)ChoConvert.ChangeType(item, typeof(T));
        }

        public static IEnumerable CastEnumerable(this IEnumerable @this, Type toType)
        {
            if (@this == null)
                yield break;

            foreach (object item in @this)
            {
                if (toType == null)
                    yield return item;
                else
                    yield return ChoConvert.ChangeType(item, toType);
            }
        }

        public static string ReadResourceFile(this Assembly assembly, string resourceName)
        {
            ChoGuard.ArgumentNotNullOrEmpty(resourceName, "ResourceName");

            string filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), resourceName);
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                    return reader.ReadToEnd();
            }
            else
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (StreamReader reader = new StreamReader(stream))
                        return reader.ReadToEnd();
                }
            }
        }

        public static byte[] Serialize(this object target)
        {
            ChoGuard.ArgumentNotNull(target, "Target");

            using (MemoryStream f = new MemoryStream())
            {
                if (target != null)
                    new BinaryFormatter().Serialize(f, target);

                return f.ToArray();
            }
        }

        public static T Deserialize<T>(this byte[] buffer)
        {
            return (T)Deserialize(buffer);
        }

        public static object Deserialize(this byte[] buffer)
        {
            ChoGuard.ArgumentNotNull(buffer, "buffer");

            using (MemoryStream f = new MemoryStream(buffer))
            {
                return new BinaryFormatter().Deserialize(f);
            }
        }
        #region XmlSerialize Overloads

        public static void XmlSerialize(Stream sr, object target, XmlWriterSettings xws = null, byte[] separator = null)
        {
            ChoGuard.ArgumentNotNull(sr, "Stream");
            ChoGuard.ArgumentNotNull(target, "Target");

            if (target.GetType().IsArray)
            {
                if (((object[])target).Length > 0)
                {
                    bool first = true;
                    foreach (object item in (object[])target)
                    {
                        if (separator != null)
                        {
                            if (!first)
                                sr.Write(separator, 0, separator.Length);
                            else
                                first = false;
                        }

                        XmlSerialize(sr, item, xws, separator);
                    }
                }

                return;
            }

            using (XmlWriter xtw = XmlTextWriter.Create(sr, xws ?? _xws))
            {
                XmlSerializer serializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer(target.GetType());
                serializer.Serialize(xtw, target);

                xtw.Flush();
            }
        }

        public static string XmlSerialize(object target, XmlWriterSettings xws = null, string separator = null,
            ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore,
            string nsPrefix = null, bool emitDataType = false, bool? useXmlArray = null,
            bool useJsonNamespaceForObjectType = false, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, string key = null,
            bool? turnOffPluralization = null, Func<string, object, bool?> xmlArrayQualifierOverride = null,
            bool useOriginalNodeName = false, string valueNamePrefix = null
            )
        {
            if (target is ExpandoObject)
            {
                var dObj = new ChoDynamicObject(target as IDictionary<string, object>);
                target = dObj;
                dObj.DynamicObjectName = key;
            }
            else if (target is IDictionary<string, object>)
            {
                if (target is ChoDynamicObject dObj)
                {
                    if (!key.IsNullOrWhiteSpace()
                        && dObj.DynamicObjectName == ChoDynamicObject.DefaultName)
                    {
                        dObj.DynamicObjectName = key;
                    }
                }
                else
                {
                    var dObj1 = new ChoDynamicObject(target as IDictionary<string, object>);
                    target = dObj1;
                    dObj1.DynamicObjectName = key;
                }
            }
            else if (target is IDictionary)
            {
                var dObj = new ChoDynamicObject(((IDictionary)target).ToDictionary(valueNamePrefix: valueNamePrefix));
                target = dObj;
                dObj.DynamicObjectName = key;
            }
            else if (target is ChoDynamicObject dObj)
            {
                if (!key.IsNullOrWhiteSpace()
                    && dObj.DynamicObjectName == ChoDynamicObject.DefaultName)
                {
                    dObj.DynamicObjectName = key;
                }
            }

            XmlSerializerNamespaces ns = null;
            if (nsMgr != null)
                ns = nsMgr.XmlSerializerNamespaces;

            if (xws == null)
            {
                if (separator.IsNullOrEmpty())
                {
                    xws = new XmlWriterSettings();
                    xws.OmitXmlDeclaration = true;
                    xws.Indent = false;
                    xws.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                }
            }

            //ChoGuard.ArgumentNotNull(target, "Target");
            if (target == null)
                return null;

            StringBuilder xmlString = new StringBuilder();

            if (target.GetType().IsArray || typeof(IList).IsAssignableFrom(target.GetType()))
            {
                if (((IList)target).Count > 0)
                {
                    string xml = null;
                    IList list = target as IList;
                    if (list.OfType<ChoDynamicObject>().Count() == list.Count)
                    {
                        xml = ((IList)target).OfType<object>().Select(o => XmlSerialize(o, xws, separator, nullValueHandling, nsPrefix, emitDataType, useXmlArray,
                            useJsonNamespaceForObjectType, nsMgr, ignoreFieldValueMode, key?.ToSingular(), turnOffPluralization, xmlArrayQualifierOverride,
                            useOriginalNodeName)).Aggregate((current, next) => "{0}{1}{2}".FormatString(current, separator, next));
                    }
                    else if (!(list is ArrayList) && list.OfType<Object>().Select(o => o.GetType()).Distinct().Count() == 1)
                    {
                        xml = ((IList)target).OfType<object>().Select(o => XmlSerialize(o, xws, separator, nullValueHandling, nsPrefix, emitDataType, useXmlArray,
                            useJsonNamespaceForObjectType, nsMgr, ignoreFieldValueMode, key?.ToSingular(), turnOffPluralization, xmlArrayQualifierOverride,
                            useOriginalNodeName)).Aggregate((current, next) => "{0}{1}{2}".FormatString(current, separator, next));
                    }
                    else
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            // Various for loops etc as necessary that will ultimately do this:
                            XmlSerialize(memoryStream, target, xws);
                            memoryStream.Position = 0;
                            var bytes = memoryStream.ToArray();
                            xml = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        }
                    }
                    //return $"<dynamic>{xml}</dynamic>";
                    return xml;
                }
                else
                    return String.Empty;
            }

            bool replaceNode = false;
            using (XmlWriter xtw = XmlTextWriter.Create(xmlString, xws ?? _xws))
            {
                if (target is ChoDynamicObject)
                {
                    xtw.WriteRaw(((ChoDynamicObject)target).GetXml(null, nullValueHandling, nsPrefix, emitDataType, EOLDelimiter: separator, useXmlArray: useXmlArray,
                        useJsonNamespaceForObjectType: useJsonNamespaceForObjectType, nsMgr: nsMgr, ignoreFieldValueMode: ignoreFieldValueMode,
                        turnOffPluralization: turnOffPluralization, xmlArrayQualifierOverride: xmlArrayQualifierOverride,
                        useOriginalNodeName: useOriginalNodeName));
                }
                else
                {
                    replaceNode = true;
                    if (ns == null)
                        ns = new XmlSerializerNamespaces();

                    target = ChoXmlConvert.ToString(target);
                    XmlSerializer serializer = ChoNullNSXmlSerializerFactory.GetNSXmlSerializer(target.GetType());
                    serializer.Serialize(xtw, target, ns);
                }

                xtw.Flush();

                var xml = key.IsNullOrWhiteSpace() || !replaceNode ? xmlString.ToString() : xmlString.ToString().ReplaceXmlNodeIfAppl(key);

                if (emitDataType)
                {
                    string xsiNSPrefix = "xsi";
                    var xsiNS = nsMgr.GetNamespaceForPrefix(xsiNSPrefix);
                    if (xsiNS != null)
                        xml = xml.AddXsiTypeIfApplicable(target.GetType(), xsiNS);
                }

                return xml;
            }
        }

        public static void XmlSerialize(string path, object target, XmlWriterSettings xws = null, string separator = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(path, "Path");
            ChoGuard.ArgumentNotNull(target, "Target");

            Directory.CreateDirectory(Path.GetDirectoryName(ChoPath.GetFullPath(path)));

            File.WriteAllText(path, XmlSerialize(target, xws, separator));
        }

        #endregion XmlSerialize Overloads

        #region XmlDeserialize Overloads

        public static T XmlDeserialize<T>(Stream sr, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null, string jsonSchemaNS = null,
            ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null, bool retainXmlAttributesAsNative = true, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore,
            string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null, ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            return (T)XmlDeserialize(sr, typeof(T), xrs, overrides, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling,
                defaultNSPrefix, nsMgr, ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
        }

        public static object XmlDeserialize(Stream sr, Type type, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null, string jsonSchemaNS = null,
            ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null, bool retainXmlAttributesAsNative = true,
            ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore, string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(sr, "Stream");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");

            using (XmlReader xtw = XmlTextReader.Create(sr, xrs ?? new XmlReaderSettings()))
            {
                if (type == typeof(ChoDynamicObject))
                {
                    XElement ele = XElement.Load(xtw);
                    return ele.ToDynamic(xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling, defaultNSPrefix, nsMgr,
                        ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
                }
                else
                {
                    XmlSerializer serializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer(type, overrides);
                    var o = serializer.Deserialize(xtw);
                    o = ChoXmlConvert.ToObject(type, o);
                    return o;
                }
            }
        }

        public static T XmlDeserialize<T>(string xmlString, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null, string jsonSchemaNS = null, ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null,
            bool retainXmlAttributesAsNative = true, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore, string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(xmlString, "xmlString");

            return (T)XmlDeserialize(xmlString, typeof(T), xrs, overrides, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling,
                defaultNSPrefix, nsMgr, ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
        }

        public static object XmlDeserialize(string xmlString, Type type, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null, string jsonSchemaNS = null, ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null,
            bool retainXmlAttributesAsNative = true, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore, string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(xmlString, "XmlString");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");

            using (StringReader sr = new StringReader(xmlString))
            {
                XmlParserContext parserContext = new XmlParserContext(null, nsMgr.NSMgr, null, XmlSpace.None);
                using (XmlReader xtw = XmlTextReader.Create(sr, xrs ?? new XmlReaderSettings(), parserContext))
                {
                    if (type == typeof(ChoDynamicObject))
                    {
                        XElement ele = XElement.Load(xtw);
                        object obj = ele.ToDynamic(xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling, defaultNSPrefix, nsMgr,
                            ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
                        if (obj is Array)
                        {
                            ChoDynamicObject dobj = new ChoDynamicObject();
                            dobj.Add(ele.Name.LocalName, obj);
                            return dobj;
                        }
                        return obj;
                    }
                    else
                    {
                        XmlSerializer serializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer(type, overrides);
                        var o = serializer.Deserialize(xtw);
                        o = ChoXmlConvert.ToObject(type, o);
                        return o;
                    }
                }
            }
        }

        public static T XmlDeserializeFromFile<T>(string path, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null,
            string jsonSchemaNS = null,
            ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null,
            bool retainXmlAttributesAsNative = true, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore,
            string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            return (T)XmlDeserializeFromFile(path, typeof(T), xrs, overrides, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling,
                            defaultNSPrefix, nsMgr, ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
        }

        public static object XmlDeserializeFromFile(string path, Type type, XmlReaderSettings xrs = null, XmlAttributeOverrides overrides = null, string xmlSchemaNS = null,
            string jsonSchemaNS = null,
            ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null,
            bool retainXmlAttributesAsNative = true, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Ignore,
            string defaultNSPrefix = null, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null, bool turnOffPluralization = false, bool ignoreNSPrefix = false,
            bool includeAllSchemaNS = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(path, "Path");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");

            using (StreamReader sr = new StreamReader(path))
            {
                using (XmlReader xtw = XmlTextReader.Create(sr, xrs ?? new XmlReaderSettings()))
                {
                    if (type == typeof(ChoDynamicObject))
                    {
                        XElement ele = XElement.Load(xtw);
                        return ele.ToDynamic(xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative, nullValueHandling,
                            defaultNSPrefix, nsMgr, ignoreFieldValueMode, turnOffPluralization, ignoreNSPrefix, includeAllSchemaNS);
                    }
                    else
                    {
                        XmlSerializer serializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer(type, overrides);
                        var o = serializer.Deserialize(xtw);
                        o = ChoXmlConvert.ToObject(type, o);
                        return o;
                    }
                }
            }
        }

        #endregion XmlDeserialize Overloads

        public static string CompareExchange(this string @this, string value, string newValue = null, StringComparison comparer = StringComparison.CurrentCultureIgnoreCase)
        {
            if (String.Compare(@this, value, comparer) == 0)
                @this = newValue;

            return @this;
        }

        public static object CastToDbValue(this object @this, object defaultValue = null)
        {
            if (@this == null)
                return defaultValue == null ? DBNull.Value : defaultValue;
            else
                return @this;
        }

        public static object CastObjectTo(this object @this, Type type, object defaultValue = null,
            ChoTypeConverterFormatSpec typeConverterFormatSpec = null)
        {
            if (type == null)
                return @this;

            if (typeConverterFormatSpec == null)
                typeConverterFormatSpec = ChoTypeConverterFormatSpec.Instance;

            if (@this == null || @this == DBNull.Value)
                return defaultValue == null ? type.Default() : defaultValue;
            if (type == @this.GetType())
                return @this;
            else if (type != typeof(object) && type.IsAssignableFrom(@this.GetType()))
                return @this;
            else if (@this is string && ((string)@this).IsNullOrWhiteSpace())
            {
                if (type == typeof(string))
                    return @this;
                else
                    return defaultValue == null ? type.Default() : defaultValue;
            }
            else
            {
                Type targetType = type;
                if (targetType == typeof(object))
                    return @this;

                try
                {
                    if (targetType.IsEnum)
                    {
                        if (@this is string)
                            return Enum.Parse(targetType, @this as string);
                        else
                            return Enum.ToObject(targetType, @this);
                    }
                    else if (targetType == typeof(Type))
                    {
                        if (@this is string)
                            return Type.GetType(@this as string);  // Convert.ChangeType(Type.GetType(@this as string), targetType);
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else if (targetType == typeof(bool))
                    {
                        bool bResult;
                        if (ChoBoolean.TryParse(@this.ToNString(), out bResult))
                            return bResult;
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else if (targetType == typeof(Guid))
                    {
                        Guid guidResult;
                        if (Guid.TryParse(@this.ToNString(), out guidResult))
                            return guidResult;
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        DateTime dtResult;
                        if (!typeConverterFormatSpec.DateTimeFormat.IsNullOrWhiteSpace()
                            && ChoDateTime.TryParseExact(@this.ToNString(), typeConverterFormatSpec.DateTimeFormat, CultureInfo.CurrentCulture, out dtResult))
                            return dtResult;
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else if (targetType == typeof(ChoCurrency))
                    {
                        ChoCurrency cyResult;
                        if (ChoCurrency.TryParse(@this.ToNString(), out cyResult))
                            return cyResult;
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else if (targetType == typeof(Decimal))
                    {
                        Decimal decResult = 0;
                        if (typeConverterFormatSpec.TreatCurrencyAsDecimal && Decimal.TryParse(@this.ToNString(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decResult))
                            return decResult;
                        else
                            return ChoConvert.ConvertFrom(@this, targetType);
                    }
                    else
                        return ChoConvert.ConvertFrom(@this, targetType);
                }
                catch
                {
                    if (defaultValue != null)
                        return defaultValue;

                    throw;
                }
            }
        }

        public static T GetAs<T>(this object @this, T defaultValue = default(T))
        {
            return CastTo<T>(@this, defaultValue);
        }

        public static T CastTo<T>(this object @this, T defaultValue = default(T))
        {
            if (@this == null || @this == DBNull.Value)
                return defaultValue == null ? default(T) : defaultValue;
            else if (@this is string && ((string)@this).IsNullOrWhiteSpace())
                return defaultValue == null ? default(T) : defaultValue;
            else
            {
                Type targetType = typeof(T);
                if (targetType == typeof(object))
                    return (T)@this;

                try
                {
                    if (targetType.IsEnum)
                    {
                        if (@this is string)
                            return (T)Enum.Parse(targetType, @this as string);
                        else
                            return (T)Enum.ToObject(targetType, @this);
                    }
                    else if (targetType == typeof(Type))
                    {
                        if (@this is string)
                            return (T)ChoConvert.ChangeType(Type.GetType(@this as string), typeof(T));
                        else
                            return (T)ChoConvert.ChangeType(@this, typeof(T));
                    }
                    else if (targetType == typeof(bool))
                    {
                        bool bResult;
                        if (ChoBoolean.TryParse(@this.ToNString(), out bResult))
                            return (T)ChoConvert.ChangeType(bResult, typeof(T));

                        return (T)ChoConvert.ChangeType(@this, typeof(T));
                    }
                    else
                        return (T)ChoConvert.ChangeType(@this, typeof(T));
                }
                catch
                {
                    if (defaultValue != null)
                        return defaultValue;

                    throw;
                }
            }
        }

        public static bool IsCollectionType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.IsArray)
                return true;

            foreach (Type @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>) && type.GetGenericArguments().Length > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Type GetItemType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (!typeof(IList).IsAssignableFrom(type)
                && !(type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(IList<>))
                )
                return type;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericEnumerable())
            {
                if (type.GetGenericArguments().Length > 0)
                    return type.GetGenericArguments()[0];
            }
            else
            {
                foreach (Type @interface in type.GetInterfaces())
                {
                    if (@interface.IsGenericType)
                    {
                        if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>) && type.GetGenericArguments().Length > 0)
                        {
                            return type.GetGenericArguments()[0];
                        }
                    }
                }
            }
            return type;
            //throw new ArgumentNullException("Invalid '{0}' collection type passed.".FormatString(type.Name));
        }

        public static bool IsGenericList(this Type type, Type itemType)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.IsArray)
                return itemType.IsAssignableFrom(type.GetElementType());

            foreach (Type @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {

                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        return itemType.IsAssignableFrom(type.GetGenericArguments()[0]);
                    }
                }
            }
            return false;
        }

        public static string FullName(this MemberInfo mi)
        {
            if (mi == null) return null;

            return "{0}.{1}".FormatString(mi.ReflectedType.Name, mi.Name);
        }

        public static void Loop(this IEnumerable e, Action preActionCallback = null, Action<object> postActionCallback = null)
        {
            if (e == null) return;

            var recEnum = e.GetEnumerator();
            while (true)
            {
                if (preActionCallback != null)
                    preActionCallback();

                if (!recEnum.MoveNext())
                    break;

                if (postActionCallback != null)
                    postActionCallback(recEnum.Current);
            }

            //foreach (var x in e)
            //{
            //    if (postActionCallback != null)
            //        postActionCallback(x);
            //}
        }

        public static string Right(this string source, int length)
        {
            if (source == null)
                return source;

            if (length < 0)
                throw new ArgumentException("Invalid length passed.");

            if (length == 0)
                return String.Empty;
            if (source.Length <= length)
                return source;
            return source.Substring(source.Length - length);
        }

        public static string RightOf(this string source, string searchText)
        {
            if (source == null)
                return source;

            if (searchText.IsNullOrEmpty())
                throw new ArgumentException("Invalid searchText passed.");

            int index = source.IndexOf(searchText);
            if (index < 0)
                return String.Empty;
            index = index + searchText.Length;
            return source.Substring(index);
        }

        public static string MaskDigitsLeft(this string source, int noOfDigitToShowAtEnd = 4)
        {
            if (source == null) return source;

            return MaskDigits(source, 0, noOfDigitToShowAtEnd);
        }

        public static string MaskDigitsRight(this string source, int noOfDigitToShowAtBegin = 4)
        {
            if (source == null) return source;

            return MaskDigits(source, noOfDigitToShowAtBegin, 0);
        }

        public static string MaskDigits(this string source, int skipLeft = 6, int skipRight = 4)
        {
            if (source == null) return source;

            StringBuilder sb = new StringBuilder(source);

            int left = -1;

            for (int i = 0, c = 0; i < sb.Length; ++i)
            {
                if (Char.IsDigit(sb[i]))
                {
                    c += 1;

                    if (c > skipLeft)
                    {
                        left = i;

                        break;
                    }
                }
            }

            if (left >= 0)
            {
                for (int i = sb.Length - 1, c = 0; i >= left; --i)
                    if (Char.IsDigit(sb[i]))
                    {
                        c += 1;

                        if (c > skipRight)
                            sb[i] = 'X';
                    }
            }

            return sb.ToString();
        }

        public static string MaskLeft(this string source, int noOfDigitToShowAtEnd = 4)
        {
            if (source == null) return source;

            return Mask(source, 0, noOfDigitToShowAtEnd);
        }

        public static string MaskRight(this string source, int noOfDigitToShowAtBegin = 4)
        {
            if (source == null) return source;

            return Mask(source, noOfDigitToShowAtBegin, 0);
        }

        public static string Mask(this string source, int skipLeft = 6, int skipRight = 4)
        {
            if (source == null) return source;

            StringBuilder sb = new StringBuilder(source);

            int left = -1;

            for (int i = 0, c = 0; i < sb.Length; ++i)
            {
                c += 1;

                if (c > skipLeft)
                {
                    left = i;

                    break;
                }
            }

            if (left >= 0)
            {
                for (int i = sb.Length - 1, c = 0; i >= left; --i)
                {
                    c += 1;

                    if (c > skipRight)
                        sb[i] = 'X';
                }
            }

            return sb.ToString();
        }

        public static void Bcp(this DataTable dt, string connectionString, string tableName = null,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(connectionString, copyOptions);
            Bcp(dt, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        public static void Bcp(this DataTable dt, SqlConnection conn, string tableName = null,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default,
            SqlTransaction transaction = null)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(conn, copyOptions, transaction);
            Bcp(dt, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        private static void Bcp(this DataTable dt, SqlBulkCopy bcp, string tableName = null,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null)
        {
            dt.AcceptChanges();

            bcp.DestinationTableName = !tableName.IsNullOrWhiteSpace() ? tableName : dt.TableName;

            if (batchSize > 0)
                bcp.BatchSize = batchSize;
            bcp.EnableStreaming = true;
            if (timeoutInSeconds > 0)
                bcp.BulkCopyTimeout = timeoutInSeconds;
            else
                bcp.BulkCopyTimeout = 0;
            if (notifyAfter > 0)
            {
                bcp.NotifyAfter = notifyAfter;
                bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                {
                    if (rowsCopied != null)
                        rowsCopied(sender, e);
                    else
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                };
            }

            if (columnMappings != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in columnMappings)
                    bcp.ColumnMappings.Add(keyValuePair.Key, keyValuePair.Value);
            }

            try
            {
                bcp.WriteToServer(dt);
            }
            catch (SqlException ex)
            {
                throw ChoSqlBulkCopyException.New(bcp, ex);
            }
        }

        public static void Bcp(this IDataReader dr, string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(connectionString, copyOptions);
            Bcp(dr, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        public static void Bcp(this IDataReader dr, SqlConnection conn, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default,
            SqlTransaction transaction = null)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(conn, copyOptions, transaction);
            Bcp(dr, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        private static void Bcp(this IDataReader dr, SqlBulkCopy bcp, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

            bcp.DestinationTableName = tableName;

            if (batchSize > 0)
                bcp.BatchSize = batchSize;
            bcp.EnableStreaming = true;
            if (timeoutInSeconds > 0)
                bcp.BulkCopyTimeout = timeoutInSeconds;
            else
                bcp.BulkCopyTimeout = 0;
            if (notifyAfter > 0)
            {
                bcp.NotifyAfter = notifyAfter;
                bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                {
                    if (rowsCopied != null)
                        rowsCopied(sender, e);
                    else
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                };
            }

            if (columnMappings == null || columnMappings.Count == 0)
            {
                foreach (KeyValuePair<string, string> keyValuePair in columnMappings)
                    bcp.ColumnMappings.Add(keyValuePair.Key, keyValuePair.Value);
            }

            try
            {
                bcp.WriteToServer(dr);
            }
            catch (SqlException ex)
            {
                throw ChoSqlBulkCopyException.New(bcp, ex);
            }
        }

        public static void Bcp(this IEnumerable collection, string connectionString, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(connectionString, copyOptions);
            Bcp(collection, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        public static void Bcp(this IEnumerable collection, SqlConnection conn, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null,
            SqlBulkCopyOptions copyOptions = SqlBulkCopyOptions.Default,
            SqlTransaction transaction = null)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(conn, copyOptions, transaction);
            Bcp(collection, bcp, tableName, batchSize, notifyAfter, timeoutInSeconds, rowsCopied, columnMappings);
        }

        private static void Bcp(this IEnumerable collection, SqlBulkCopy bcp, string tableName,
            int batchSize = 0, int notifyAfter = 0, int timeoutInSeconds = 0,
            Action<object, SqlRowsCopiedEventArgs> rowsCopied = null,
            IDictionary<string, string> columnMappings = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(collection, nameof(collection));
            ChoGuard.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

            Bcp(collection.AsDataReader((d) =>
            {
                if (columnMappings == null || columnMappings.Count == 0)
                {
                    columnMappings = new Dictionary<string, string>();
                    foreach (var key in d.Keys)
                    {
                        columnMappings.Add(key, key);
                    }
                }
            }), bcp, tableName, batchSize, notifyAfter, timeoutInSeconds,
                rowsCopied, columnMappings);
        }

        internal static string Format(string format, object value)
        {
            if (value == null) return null;
            return format.IsNullOrWhiteSpace() ? value.ToString() : String.Format("{{0:{0}}}".FormatString(format), value);
        }

        public static string ExpandProperties(this string inString, ChoPropertyReplacerManager propertyReplacer = null, object state = null)
        {
            return ExpandProperties(inString, StartSeparator, EndSeparator, FormatSeparator, propertyReplacer, state);
        }

        public static string ExpandProperties(string inString, char startSeparator, char endSeparator, char formatSeparator,
            ChoPropertyReplacerManager propertyReplacer = null /*IChoPropertyReplacer[] propertyReplacers */, object state = null)
        {
            if (propertyReplacer == null)
                propertyReplacer = ChoPropertyReplacerManager.Default;

            if (inString.IsNullOrEmpty())
                return inString;

            string msg = inString;
            if (inString.IndexOf(startSeparator) != -1)
            {
                int index = -1;
                bool hasChar = false;
                StringBuilder message = new StringBuilder();
                StringBuilder token = new StringBuilder();
                while (++index < inString.Length)
                {
                    if (!hasChar && inString[index] == startSeparator
                        && index + 1 < inString.Length && inString[index + 1] == startSeparator)
                    {
                        index++;
                        message.Append(inString[index]);
                        continue;
                        //hasChar = true;
                    }
                    else if (inString[index] == startSeparator)
                    {
                        if (hasChar)
                        {
                            bool hadEndChar = false;
                            do
                            {
                                if (inString[index] == endSeparator && inString[index - 1] == endSeparator)
                                {
                                    if (!hadEndChar)
                                        hadEndChar = true;
                                    else
                                        message.Append(inString[index]);

                                    continue;
                                }
                                message.Append(inString[index]);
                            }
                            while (++index < inString.Length && inString[index] != startSeparator);

                            index--;
                            hasChar = false;
                        }
                        else
                        {
                            token.Remove(0, token.Length);
                            index++;
                            do
                            {
                                if (!hasChar && inString[index] == endSeparator
                                    && index + 1 < inString.Length && inString[index + 1] == endSeparator)
                                {
                                    hasChar = true;
                                }
                                else if (inString[index] == endSeparator)
                                {
                                    if (hasChar)
                                    {
                                        message.Append(startSeparator);
                                        message.Append(token);
                                        message.Append(inString[index]);
                                        bool hadEndChar = false;
                                        do
                                        {
                                            if (inString[index] == endSeparator && inString[index - 1] == endSeparator)
                                            {
                                                if (!hadEndChar)
                                                    hadEndChar = true;
                                                else
                                                    message.Append(inString[index]);

                                                continue;
                                            }
                                            message.Append(inString[index]);
                                        }
                                        while (++index < inString.Length && inString[index] == endSeparator);
                                    }
                                    else
                                    {
                                        if (token.Length > 0)
                                        {
                                            string[] propertyNameNFormat = token.ToString().SplitNTrim(formatSeparator);
                                            if (!(propertyNameNFormat.Length >= 1 &&
                                                ReplaceToken(propertyReplacer, message, propertyNameNFormat[0],
                                                    propertyNameNFormat.Length == 2 ? propertyNameNFormat[1] : null, state)))
                                                message.AppendFormat("{0}{1}{2}", startSeparator, token, endSeparator);
                                        }
                                    }

                                    break;
                                }
                                else
                                    token.Append(inString[index]);
                            }
                            while (++index < inString.Length);
                        }
                    }
                    else
                        message.Append(inString[index]);
                }
                msg = message.ToString();
            }

            foreach (IChoPropertyReplacer propertyReplacer1 in propertyReplacer.Items.ToArray())
            {
                if (!(propertyReplacer1 is IChoCustomPropertyReplacer)) continue;

                IChoCustomPropertyReplacer customPropertyReplacer = propertyReplacer1 as IChoCustomPropertyReplacer;
                string formattedMsg = msg;
                bool retVal = customPropertyReplacer.Format(ref formattedMsg);

                if (!String.IsNullOrEmpty(formattedMsg))
                    msg = formattedMsg;

                if (!retVal)
                    break;
            }

            return msg;
        }

        private static bool ReplaceToken(ChoPropertyReplacerManager propertyReplacer, StringBuilder message,
            string propertyName, string format, object state = null)
        {
            string propertyValue;
            bool retValue = propertyReplacer.RaisePropertyReolve(propertyName, format, out propertyValue, state);
            if (retValue)
            {
                if (propertyValue != null)
                    message.Append(propertyValue);
                return true;
            }

            if (!String.IsNullOrEmpty(propertyName))
            {
                foreach (IChoPropertyReplacer propertyReplacer1 in propertyReplacer.Items.ToArray())
                {
                    if (!(propertyReplacer1 is IChoKeyValuePropertyReplacer)) continue;

                    IChoKeyValuePropertyReplacer dictionaryPropertyReplacer = propertyReplacer1 as IChoKeyValuePropertyReplacer;
                    if (dictionaryPropertyReplacer == null || !dictionaryPropertyReplacer.ContainsProperty(propertyName)) continue;
                    message.Append(dictionaryPropertyReplacer.ReplaceProperty(propertyName, format));
                    return true;
                }
            }

            return false;
        }
        public static void Seek(this StreamReader sr, long position, SeekOrigin origin)
        {
            if (sr.BaseStream.CanSeek)
            {
                sr.BaseStream.Seek(position, origin);
                sr.DiscardBufferedData();
            }
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private readonly static object _padLock = new object();
        private readonly static Dictionary<Type, Dictionary<Type, Tuple<MemberInfo, ChoOrderedAttribute>[]>> _recordTypeMembersCache = new Dictionary<Type, Dictionary<Type, Tuple<MemberInfo, ChoOrderedAttribute>[]>>();
        public static Tuple<MemberInfo, ChoOrderedAttribute>[] DiscoverRecordMembers(Type type, Type attrType)
        {
            if (_recordTypeMembersCache.ContainsKey(type)
                && _recordTypeMembersCache[type].ContainsKey(attrType)) return _recordTypeMembersCache[type][attrType];

            lock (_padLock)
            {
                if (!_recordTypeMembersCache.ContainsKey(type))
                    _recordTypeMembersCache.Add(type, new Dictionary<Type, Tuple<MemberInfo, ChoOrderedAttribute>[]>());

                if (!_recordTypeMembersCache[type].ContainsKey(attrType))
                {
                    int counter = 1;
                    SortedList<int, Tuple<MemberInfo, ChoOrderedAttribute>> q = new SortedList<int, Tuple<MemberInfo, ChoOrderedAttribute>>();
                    ChoOrderedAttribute attr = null;
                    foreach (MemberInfo mi in ChoType.GetMembers(type).ToArray())
                    {
                        if (mi.MemberType == MemberTypes.Property
                            || mi.MemberType == MemberTypes.Field)
                        {
                            attr = (mi.GetCustomAttributesEx(attrType)).AsEnumerable().FirstOrDefault() as ChoOrderedAttribute;
                            if (attr == null) continue;

                            if (attr.Order == Int32.MinValue)
                            {
                                q.Add(counter++, new Tuple<MemberInfo, ChoOrderedAttribute>(mi, attr));
                            }
                            else
                                q.Add(attr.Order, new Tuple<MemberInfo, ChoOrderedAttribute>(mi, attr));
                        }
                    }

                    _recordTypeMembersCache[type].Add(attrType, q.Values.ToArray());
                }
                return _recordTypeMembersCache[type][attrType];
            }
        }

        private readonly static object _padLock1 = new object();
        private readonly static Dictionary<Type, KeyValuePair<MemberInfo, Attribute>[]> _membersCache = new Dictionary<Type, KeyValuePair<MemberInfo, Attribute>[]>();
        public static KeyValuePair<MemberInfo, Attribute>[] DiscoverMembers(Type type, Type attrType)
        {
            if (_membersCache.ContainsKey(type)) return _membersCache[type];

            lock (_padLock1)
            {
                if (!_membersCache.ContainsKey(type))
                {
                    List<KeyValuePair<MemberInfo, Attribute>> q = new List<KeyValuePair<MemberInfo, Attribute>>();
                    Attribute attr = null;
                    foreach (MemberInfo mi in ChoType.GetMembers(type).ToArray())
                    {
                        if (mi.MemberType == MemberTypes.Property
                            || mi.MemberType == MemberTypes.Field)
                        {
                            attr = (mi.GetCustomAttributesEx(attrType)).AsEnumerable().FirstOrDefault() as Attribute;
                            if (attr == null) continue;

                            q.Add(new KeyValuePair<MemberInfo, Attribute>(mi, attr));
                        }
                    }

                    _membersCache.Add(type, q.ToArray());
                }
                return _membersCache[type];
            }
        }

        public static readonly DataContractJsonSerializerSettings _jsonSettings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true, EmitTypeInformation = EmitTypeInformation.Never };
        public static string DumpAsJson(this object target, Encoding encoding = null)
        {
            return DumpAsJson(target, encoding, null);
        }

        public static string DumpAsJson(this object target, Encoding encoding, DataContractJsonSerializerSettings jsonSettings)
        {
            if (target == null)
                return String.Empty;

            encoding = encoding == null ? Encoding.UTF8 : encoding;

            if (target is IEnumerable)
                target = ((IEnumerable)target).OfType<object>().ToArray();

            using (MemoryStream ms = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                   ms, encoding, true, true, "  "))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(target.GetType(), jsonSettings == null ? _jsonSettings : jsonSettings);

                    //if (target.GetType().IsDynamicType())
                    //    serializer = new DataContractJsonSerializer(typeof(IDictionary<string, object>), jsonSettings);

                    serializer.WriteObject(writer, target);
                    writer.Flush();
                    byte[] json = ms.ToArray();
                    return encoding.GetString(json, 0, json.Length);
                }
            }
        }

        public static void PrintAsJson(this object target, TextWriter writer = null)
        {
            if (writer != null)
                writer.WriteLine(DumpAsJson(target));
            else
            {
                var output = DumpAsJson(target);
                if (output != null)
                    Console.WriteLine(output);
            }
        }

        public static void Print(this object target, TextWriter writer = null)
        {
            if (writer != null)
                writer.WriteLine(ToStringEx(target));
            else
            {
                var output = ToStringEx(target);
                if (output != null)
                    Console.WriteLine(output);
            }
        }

        public static string Dump(this object target)
        {
            return ToStringEx(target);
        }

        private static string GetMemberType(object target, string name, object value)
        {
            if (value != null)
                return value.GetType().Name;

            if (target is ChoDynamicObject)
            {
                var dobj = target as ChoDynamicObject;
                var mt = dobj.GetMemberType(name);
                if (mt != null)
                    return mt.Name;
            }

            return "UNKNOWN";
        }

        public static string ToStringEx(this object target)
        {
            if (target == null) return String.Empty;
            if (ChoETLFrxBootstrap.CustomObjectToString != null)
            {
                var result = ChoETLFrxBootstrap.CustomObjectToString(target);
                if (result != null)
                {
                    if (result is string)
                        return result;
                    else if (!Object.ReferenceEquals(target, result))
                        target = result;
                }
            }

            if (target is DataTable)
            {
                StringBuilder csv = new StringBuilder();
                ChoCSVWriter.Write(csv, target as DataTable);
                return csv.ToString();
            }
            else if (target is IDataReader)
            {
                StringBuilder csv = new StringBuilder();
                ChoCSVWriter.Write(csv, target as IDataReader);
                return csv.ToString();
            }
            else if (target.GetType().IsSimple())
                return target.ToString();
            else if (target is IEnumerable)
            {
                if (typeof(IQueryable).IsAssignableFrom(target.GetType()) || !target.GetType().IsOverrides("ToString"))
                {
                    StringBuilder arrMsg = new StringBuilder();

                    int count = 0;
                    foreach (object item in (IEnumerable)target)
                    {
                        if (target is Array && count > ChoETLFrxBootstrap.MaxArrayItemsToPrint)
                        {
                            count++;
                            continue;
                        }
                        if (item != null)
                        {
                            Type valueType = item.GetType();
                            if (valueType.IsGenericType)
                            {
                                Type baseType = valueType.GetGenericTypeDefinition();
                                if (baseType == typeof(KeyValuePair<,>))
                                {
                                    object kvpKey = valueType.GetProperty("Key").GetValue(item, null);
                                    object kvpValue = valueType.GetProperty("Value").GetValue(item, null);
                                    arrMsg.AppendFormat("Key: {0} [Type: {2}]{1}", ToStringEx(kvpKey), Environment.NewLine, kvpKey == null ? "UNKNOWN" : kvpKey.GetType().Name);
                                    arrMsg.AppendFormat("Value: {0} [Type: {2}]{1}", ToStringEx(kvpValue), Environment.NewLine, GetMemberType(target, kvpKey.ToNString(), kvpValue)); // ( "UNKNOWN" ) : kvpValue.GetType().Name);
                                    count++;
                                    continue;
                                }
                                else
                                {
                                    arrMsg.AppendFormat("{0}{1}", ToStringEx(item), Environment.NewLine);
                                    count++;
                                    continue;
                                }
                            }
                            else if (typeof(XObject).IsAssignableFrom(valueType))
                                arrMsg.AppendFormat("{0}{1}", ((XObject)item).ToString(), Environment.NewLine);
                            else if (typeof(XmlNode).IsAssignableFrom(valueType))
                                arrMsg.AppendFormat("{0}{1}", ((XmlNode)item).OuterXml, Environment.NewLine);
                            else
                                arrMsg.AppendFormat("{0}{1}", ToStringEx(item), Environment.NewLine);
                        }
                        else
                            arrMsg.AppendFormat("{0}{1}", "NULL", Environment.NewLine);

                        count++;
                    }

                    if (count > 0 && arrMsg.Length > 0)
                        return "[Count: {0}]{1}{2}".FormatString(count, Environment.NewLine, arrMsg.ToString());
                    else
                        return null;
                }
                else
                    return target.ToString();
            }
            else
            {
                bool hasToStringMethod = false;
                try
                {
                    if (target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public).FirstOrDefault(m => m.Name == "ToString") == null)
                        hasToStringMethod = false;
                    else
                        hasToStringMethod = true;
                }
                catch (AmbiguousMatchException)
                {
                    hasToStringMethod = true;
                }

                //Check if ToString is overridden
                if (target.GetType().IsAnonymousType() || !hasToStringMethod)
                {
                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                    ChoStringMsgBuilder msg = new ChoStringMsgBuilder(String.Format("{0} State", target.GetType().IsAnonymousType() ? "Anonymous Type" : target.GetType().FullName));

                    //MemberInfo[] memberInfos = target.GetType().GetMembers(bindingFlags /*BindingFlags.Public | BindingFlags.Instance /*| BindingFlags.DeclaredOnly*/ /*| BindingFlags.GetField | BindingFlags.GetProperty*/);
                    IEnumerable<MemberInfo> memberInfos = ChoType.GetGetFieldsNProperties(target.GetType(), bindingFlags);
                    if (memberInfos == null || memberInfos.Count() == 0)
                        msg.AppendFormatLine(ChoStringMsgBuilder.Empty);
                    else
                    {
                        foreach (MemberInfo memberInfo in memberInfos)
                        {
                            if (!ChoType.IsValidObjectMember(memberInfo))
                                continue;

                            Type type = ChoType.GetMemberType(memberInfo);
                            object value = ChoType.GetMemberValue(target, memberInfo);
                            string memberText = null;

                            if (typeof(XmlNode).IsAssignableFrom(type))
                                memberText = value != null ? ((XmlNode)value).OuterXml : "[NULL]";
                            else if (typeof(XNode).IsAssignableFrom(type))
                                memberText = value != null ? ((XNode)value).GetInnerXml() : "[NULL]";
                            else if (!type.IsSimple() && type != typeof(Type))
                            {
                                memberText = value != null ? ChoUtility.ToStringEx(value) : "[NULL]";
                                if (memberText.ContainsMultiLines())
                                    memberText = Environment.NewLine + memberText.Indent();
                            }
                            else
                                memberText = value.ToNString("[NULL]");

                            msg.AppendFormatLine("{0}: {1}", memberInfo.Name, memberText);
                        }
                    }
                    msg.AppendNewLine();

                    return msg.ToString();
                }
                else
                    return target.ToString();
            }
        }

        public static bool IsCollection(this Type type)
        {
            if (type == null) return false;

            if (typeof(Array).IsAssignableFrom(type))
                return true;
            else if (typeof(IList).IsAssignableFrom(type))
                return true;
            else if (type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(IList<>))
                return true;
            //else if (typeof(IDictionary).IsAssignableFrom(type))
            //    return true;
            else
                return false;
        }

        public static Type GetElementType(this Type type)
        {
            if (typeof(Array).IsAssignableFrom(type))
                type = type.GetElementType();
            else if (typeof(IList).IsAssignableFrom(type))
                type = type.GetGenericArguments()[0];
            //else
            //    return null;

            return type;
        }

        public static Array Cast(this Array array, Type elementType)
        {
            // assume there is at least one element in list
            IList arr = Array.CreateInstance(elementType, array.Length);
            int index = 0;
            foreach (var item in array)
            {
                arr[index] = item == null ? ChoType.GetDefaultValue(elementType) : ChoConvert.ChangeType(item, elementType);
                index++;
            }
            return (Array)arr;
        }

        public static IList CreateGenericList(this Type type)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(new[] { type }));
        }

        public static IList Cast(this IList list, Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(new Type[] { elementType });
            IList list1 = (IList)Activator.CreateInstance(listType);

            foreach (object t in list)
                list1.Add(t);

            return list1;
        }

        public static IList Cast(this IList list, Func<object, object> itemConverter)
        {
            if (itemConverter == null)
                return list;

            var oldList = new List<object>();
            foreach (object t in list)
                oldList.Add(t);

            list.Clear();

            if (list is Array)
            {
                List<object> l = new List<object>();
                foreach (object t in oldList)
                    l.Add(itemConverter(t));

                list = l.ToArray();
            }
            else
            {
                foreach (object t in oldList)
                    list.Add(itemConverter(t));
            }

            return list;
        }

        public static IDictionary Cast(this IDictionary dict, Func<KeyValuePair<object, object>, KeyValuePair<object, object>> itemConverter)
        {
            if (itemConverter == null)
                return dict;

            var oldDict = new Hashtable();
            foreach (var t in dict.Keys)
                oldDict.Add(t, dict[t]);

            dict.Clear();

            foreach (var t in oldDict.Keys)
            {
                var kvp = itemConverter(new KeyValuePair<object, object>(t, oldDict[t]));
                dict.Add(kvp.Key, kvp.Value);
            }

            return dict;
        }
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
        public static object ConvertValueToObjectMemberType(object target, MemberInfo memberInfo, object value, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull((object)memberInfo, "MemberInfo");
            if (target is Type)
                return ChoConvert.ConvertFrom(value, memberInfo, (object)null, culture);
            return ChoConvert.ConvertFrom(value, memberInfo, target, culture);
        }
        public static object ConvertValueToObjectPropertyType(object target, PropertyInfo propertyInfo, object value, CultureInfo culture = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull((object)propertyInfo, "PropertyInfo");
            if (target is Type)
                return ChoConvert.ConvertFrom(value, propertyInfo, (object)null, culture);
            return ChoConvert.ConvertFrom(value, propertyInfo, target, culture);
        }

        public static void TryCatch(Action action)
        {
            if (action == null) return;

            try
            {
                action();
            }
            catch { }
        }

        public static T TryCatch<T>(Func<T> func, T defaultValue = default(T))
        {
            if (func == null) return defaultValue;

            try
            {
                return func();
            }
            catch { }

            return defaultValue;
        }

        #region Join Overloads (Public)

        public static string Join(object[] inValues)
        {
            return Join(inValues, null, ',');
        }

        public static string Join(object[] inValues, char Separator)
        {
            return Join(inValues, null, Separator);
        }

        public static string Join(object[] inValues, string defaultNullValue)
        {
            return Join(inValues, defaultNullValue, ',');
        }

        public static string Join(object[] inValues, string defaultNullValue, char Separator)
        {
            if (inValues == null || inValues.Length == 0) return String.Empty;

            StringBuilder outString = new StringBuilder();
            foreach (object inValue in inValues)
            {
                object convertedValue = inValue;
                if (inValue == null || inValue == DBNull.Value)
                {
                    if (defaultNullValue == null)
                        continue;
                    else
                        convertedValue = defaultNullValue;
                }
                if (outString.Length == 0)
                    outString.Append(convertedValue.ToString());
                else
                    outString.AppendFormat("{0}{1}", Separator, convertedValue.ToString());
            }

            return outString.ToString();
        }

        #endregion Join Overloads

        private static MemberInfo GetMemberEx(this Type type, string memberName)
        {
            PropertyInfo prop = type.GetProperty(memberName);
            if (prop != null)
                return prop;
            return type.GetField(memberName);
        }

        public static bool HasAttribute(this MemberInfo property, Type attributeType)
        {
            if (property == null)
                return false;

            if (property.GetCustomAttributes().Any(a => a.GetType() == attributeType))
                return true;

            var interfaces = property.DeclaringType.GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
                if (HasAttribute(interfaces[i].GetMemberEx(property.Name), attributeType))
                    return true;

            return false;
        }

        public static Attribute[] GetCustomAttributesEx(this MemberInfo property, Type attributeType)
        {
            if (property == null)
                return new Attribute[] { };

            if (attributeType != null && property.GetCustomAttributes().Any(a => attributeType.IsAssignableFrom(a.GetType())))
                return (from x in property.GetCustomAttributes()
                        where attributeType.IsAssignableFrom(x.GetType())
                        select x).ToArray();

            if (property.DeclaringType != null)
            {
                var interfaces = property.DeclaringType.GetInterfaces();

                if (interfaces.Length > 0)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        Attribute[] attr = GetCustomAttributesEx(interfaces[i].GetProperty(property.Name), attributeType);
                        if (attr != null && attr.Length > 0)
                            return attr;
                    }
                }
                else
                    return property.GetCustomAttributes().ToArray().Where(a => attributeType.IsAssignableFrom(a.GetType())).ToArray();
            }

            return new Attribute[] { };
        }

        public static Attribute GetCustomAttributeEx(this MemberInfo property, Type attributeType)
        {
            if (property == null)
                return null;

            if (property.GetCustomAttributes().Any(a => attributeType.IsAssignableFrom(a.GetType())))
                return (from x in property.GetCustomAttributes()
                        where attributeType.IsAssignableFrom(x.GetType())
                        select x).FirstOrDefault();

            if (property.DeclaringType != null)
            {
                var interfaces = property.DeclaringType.GetInterfaces();

                for (int i = 0; i < interfaces.Length; i++)
                {
                    Attribute[] attr = GetCustomAttributesEx(interfaces[i].GetProperty(property.Name), attributeType);
                    if (attr != null && attr.Length > 0)
                        return attr[0];
                }
            }

            return null;
        }

        public static T GetCustomAttributeEx<T>(this MemberInfo property)
            where T : Attribute
        {
            return (T)GetCustomAttributeEx(property, typeof(T));

            //Type attributeType = typeof(T);
            //if (property == null)
            //    return default(T);

            //if (property.GetCustomAttributes().Any(a => a.GetType() == attributeType))
            //    return (from x in property.GetCustomAttributes()
            //            where x.GetType() == attributeType
            //            select x).FirstOrDefault() as T;

            //var interfaces = property.DeclaringType.GetInterfaces();

            //for (int i = 0; i < interfaces.Length; i++)
            //{
            //    Attribute[] attr = GetCustomAttributesEx(interfaces[i].GetProperty(property.Name), attributeType);
            //    if (attr != null && attr.Length > 0)
            //        return (T)attr[0];
            //}

            //return default(T);
        }

        public static IEnumerable<T> ExternalSort<T>(this IEnumerable<T> unsorted, IComparer<T> comparer, int capacity = 10000, int mergeCount = 10)
        {
            ChoTextFileExternalSorter<T> sorter = new ChoTextFileExternalSorter<T>(comparer, capacity, mergeCount);
            return sorter.Sort(unsorted);
        }

        public static IEnumerable<T> ExternalSort<T>(this IEnumerable<T> unsorted, Func<T, T, int> comparer, int capacity = 10000, int mergeCount = 10)
        {
            ChoTextFileExternalSorter<T> sorter = new ChoTextFileExternalSorter<T>(new ChoLamdaComparer<T>(comparer), capacity, mergeCount);
            return sorter.Sort(unsorted);
        }

        public static IEnumerable<dynamic> ExternalSort(this IEnumerable<dynamic> unsorted, Func<dynamic, dynamic, int> comparer,
            int capacity = 10000, int mergeCount = 10)
        {
            ChoTextFileExternalSorter<dynamic> sorter = new ChoTextFileExternalSorter<dynamic>(new ChoLamdaComparer<dynamic>(comparer), capacity, mergeCount);
            return sorter.Sort(unsorted);
        }

        // Convenience overloads are not included only most general one
        public static IEnumerable<T> OrderedMerge<T>(
          this IEnumerable<IEnumerable<T>> sources,
          IComparer<T> comparer)
        {
            // Make sure sequence of ordered sequences is not null
            ChoGuard.ArgumentNotNull(sources, "Sources");
            //Contract.Requires<ArgumentNullException>(sources != null);
            // and it doesn't contain nulls
            //Contract.Requires(Contract.ForAll(sources, s => s != null));
            ChoGuard.ArgumentNotNull(comparer, "Comparer");
            //Contract.Requires<ArgumentNullException>(comparer != null);
            // Precondition checking is done outside of iterator because
            // of its lazy nature
            return OrderedMergeHelper(sources, comparer);
        }

        private static IEnumerable<T> OrderedMergeHelper<T>(
          IEnumerable<IEnumerable<T>> sources,
          IComparer<T> elementComparer)
        {
            // Each sequence is expected to be ordered according to
            // the same comparison logic as elementComparer provides
            var enumerators = sources.Select(e => e.GetEnumerator());
            // Disposing sequence of lazily acquired resources as
            // a single resource
            using (var disposableEnumerators = enumerators.AsDisposable())
            {
                // The code below holds the following loop invariant:
                // - Priority queue contains enumerators that positioned at
                // sequence element
                // - The queue at the top has enumerator that positioned at
                // the smallest element of the remaining elements of all
                // sequences

                // Ensures that only non empty sequences participate  in merge
                var nonEmpty = disposableEnumerators.Where(e => e.MoveNext());
                // Current value of enumerator is its priority
                var comparer = new EnumeratorComparer<T>(elementComparer);
                // Use priority queue to get enumerator with smallest
                // priority (current value)
                var queue = new PriorityQueue<IEnumerator<T>>(nonEmpty, comparer);

                // The queue is empty when all sequences are empty
                while (queue.Count > 0)
                {
                    // Dequeue enumerator that positioned at element that
                    // is next in the merged sequence
                    var min = queue.Dequeue();
                    yield return min.Current;
                    // Advance enumerator to next value
                    if (min.MoveNext())
                    {
                        // If it has value that can be merged into resulting
                        // sequence put it into the queue
                        queue.Enqueue(min);
                    }
                }
            }
        }

        // Provides comparison functionality for enumerators
        private class EnumeratorComparer<T> : Comparer<IEnumerator<T>>
        {
            private readonly IComparer<T> m_comparer;

            public EnumeratorComparer(IComparer<T> comparer)
            {
                m_comparer = comparer;
            }

            public override int Compare(
               IEnumerator<T> x, IEnumerator<T> y)
            {
                return m_comparer.Compare(x.Current, y.Current);
            }
        }
    }

    public static class ChoNullNSXmlSerializerFactory
    {
        private static readonly object _xmlSerializersLock = new object();
        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializers = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<Type, XmlSerializer> _xmlNSSerializers = new Dictionary<Type, XmlSerializer>();
        public static bool HasXmlSerializer(Type type)
        {
            lock (_xmlSerializersLock)
            {
                var contains = _xmlSerializers.ContainsKey(type);
                if (contains)
                    return true;

                return _xmlNSSerializers.ContainsKey(type);
            }
        }
        public static XmlSerializer GetXmlSerializer<TProxyType, TInstanceType>(XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
            where TProxyType : IChoXmlSerializerProxy<TInstanceType>
            where TInstanceType : class
        {
            return GetXmlSerializer(typeof(TProxyType), typeof(TInstanceType), overrides, unknownNode);
        }

        public static XmlSerializer GetXmlProxySerializer(Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(type, nameof(type));

            var proxyType = typeof(ChoXmlSerializerProxy<>).MakeGenericType(type);

            if (_xmlSerializers.ContainsKey(proxyType))
                return _xmlSerializers[proxyType];

            return GetXmlSerializer(proxyType, overrides == null ? GetXmlOverrides(type, proxyType) : overrides, unknownNode);
        }

        public static XmlSerializer GetXmlSerializer(Type proxyType, Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(type, nameof(type));
            ChoGuard.ArgumentNotNull(type, nameof(proxyType));

            if (_xmlSerializers.ContainsKey(proxyType))
                return _xmlSerializers[proxyType];

            return GetXmlSerializer(proxyType, overrides == null ? GetXmlOverrides(type, proxyType) : overrides, unknownNode);
        }

        public static XmlSerializer GetXmlSerializer<T>(XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            return GetXmlSerializer(typeof(T), overrides, unknownNode);
        }

        public static XmlSerializer GetNSXmlSerializer(Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(type, nameof(type));
            if (_xmlNSSerializers.ContainsKey(type))
                return _xmlNSSerializers[type];

            lock (_xmlSerializersLock)
            {
                if (!_xmlNSSerializers.ContainsKey(type))
                {
                    if (overrides == null)
                        overrides = GetXmlOverrides(type);

                    XmlSerializer serializer = overrides != null ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
                    if (unknownNode != null)
                        serializer.UnknownNode += unknownNode;
                    _xmlNSSerializers.Add(type, serializer);
                }

                return _xmlNSSerializers[type];
            }
        }

        public static XmlSerializer GetXmlSerializer(Type type, XmlAttributeOverrides overrides = null, XmlNodeEventHandler unknownNode = null)
        {
            ChoGuard.ArgumentNotNull(type, nameof(type));
            if (_xmlSerializers.ContainsKey(type))
                return _xmlSerializers[type];

            lock (_xmlSerializersLock)
            {
                if (!_xmlSerializers.ContainsKey(type))
                {
                    if (overrides == null)
                        overrides = GetXmlOverrides(type);

                    ChoNullNSXmlSerializer serializer = overrides != null ? new ChoNullNSXmlSerializer(type, overrides) : new ChoNullNSXmlSerializer(type);
                    if (unknownNode != null)
                        serializer.UnknownNode += unknownNode;
                    _xmlSerializers.Add(type, serializer);
                }

                return _xmlSerializers[type];
            }
        }
        public static XmlAttributeOverrides GetXmlOverrides<TProxyType, TInstanceType>()
            where TProxyType : IChoXmlSerializerProxy<TInstanceType>
            where TInstanceType : class
        {
            return GetXmlOverrides(typeof(TInstanceType), typeof(TProxyType));
        }


        public static XmlAttributeOverrides GetXmlOverrides(Type type, Type proxyType = null)
        {
            if (type == null) return null;

            if (proxyType == null)
            {
                var ra = type.GetCustomAttribute(typeof(XmlRootAttribute)) as XmlRootAttribute;
                if (ra == null)
                    return null;

                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                var xattribs = new XmlAttributes();
                xattribs.XmlRoot = ra;

                //XmlElementAttribute attr = new XmlElementAttribute();
                //attr.ElementName = ra.ElementName;
                //attr.Type = type;
                //xattribs.XmlElements.Add(attr);

                //XmlTypeAttribute xmlTypeAttribute = new XmlTypeAttribute();
                //xmlTypeAttribute.TypeName = type.Name;
                //xattribs.XmlType = xmlTypeAttribute;

                overrides.Add(proxyType == null ? type : proxyType, xattribs);

                return overrides;
            }
            else
            {
                var ra = type.GetCustomAttribute(typeof(XmlRootAttribute)) as XmlRootAttribute;
                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                var xattribs = new XmlAttributes();
                xattribs.XmlRoot = ra == null ? new XmlRootAttribute() { ElementName = type.Name } : ra;
                overrides.Add(proxyType == null ? type : proxyType, xattribs);
                return overrides;
            }
        }
    }

    public class ChoNullNSXmlSerializer : XmlSerializer
    {
        #region Shared Data Members (Private)

        private static XmlSerializerNamespaces _xmlnsEmpty = new XmlSerializerNamespaces();

        #endregion

        public Type Type { get; private set; }

        #region Constructors

        static ChoNullNSXmlSerializer()
        {
            _xmlnsEmpty.Add("", "");
        }

        public ChoNullNSXmlSerializer(Type type)
            : base(type)
        {
            Type = type;
        }

        public ChoNullNSXmlSerializer(XmlTypeMapping xmlTypeMapping)
            : base(xmlTypeMapping)
        {
            Type = Type.GetType(xmlTypeMapping.TypeName);
        }

        public ChoNullNSXmlSerializer(Type type, string defaultNamespace)
            : base(type, defaultNamespace)
        {
            Type = type;
        }

        public ChoNullNSXmlSerializer(Type type, Type[] extraTypes)
            : base(type, extraTypes)
        {
            Type = type;
        }

        public ChoNullNSXmlSerializer(Type type, XmlAttributeOverrides overrides)
            : base(type, overrides)
        {
            Type = type;
        }

        public ChoNullNSXmlSerializer(Type type, XmlRootAttribute root)
            : base(type, root)
        {
            Type = type;
        }

        public ChoNullNSXmlSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
            : base(type, overrides, extraTypes, root, defaultNamespace)
        {
        }

        #endregion

        #region XmlSerialier Overrides (Public)

        public new void Serialize(Stream stream, object o)
        {
            base.Serialize(stream, o, _xmlnsEmpty);
        }
        public new void Serialize(Stream stream, object o, XmlSerializerNamespaces namespaces)
        {
            base.Serialize(stream, o, _xmlnsEmpty);
        }
        public new void Serialize(TextWriter textWriter, object o)
        {
            base.Serialize(textWriter, o, _xmlnsEmpty);
        }
        public new void Serialize(TextWriter textWriter, object o, XmlSerializerNamespaces namespaces)
        {
            base.Serialize(textWriter, o, _xmlnsEmpty);
        }
        public new void Serialize(XmlWriter xmlWriter, object o)
        {
            base.Serialize(xmlWriter, o, _xmlnsEmpty);
        }
        public new void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces)
        {
            base.Serialize(xmlWriter, o, _xmlnsEmpty);
        }
        public new void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
        {
            base.Serialize(xmlWriter, o, _xmlnsEmpty, encodingStyle, id);
        }
        public new void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle)
        {
            base.Serialize(xmlWriter, o, _xmlnsEmpty, encodingStyle);
        }
        #endregion
    }
}
