using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoTypePropertyInfo
    {
        public object[] PropConverters;
        public object[] PropConverterParams;
        public string FormatText;
        public int? Size;
        public string NullValueText;
    }

    public static class ChoObjectEx
    {
        private static readonly object _padLock = new object();
        private static readonly Dictionary<Type, Dictionary<PropertyInfo, ChoTypePropertyInfo>> _typeCache = new Dictionary<Type, Dictionary<PropertyInfo, ChoTypePropertyInfo>>();

        public static bool IsObjectNullOrEmpty(this object target)
        {
            if (target == null)
                return true;

            if (target is string)
                return String.IsNullOrEmpty(((string)target));

            return false;
        }
        public static bool GetNestedMember(this object target, string propName, ref object parent, ref string memberName,
            bool createSubKeysIfNotExists = false, Func<object> factory = null)
        {
            if (target == null)
                return false;
            if (propName.IsNullOrWhiteSpace())
                return false;

            parent = target;
            if (!propName.Contains("."))
            {
                memberName = propName;
                return true;
            }
            else
            {
                int pos = propName.IndexOf(".");
                if (pos < 0)
                    return false;

                string spropName = propName.Substring(0, pos);
                if (spropName.IsNullOrWhiteSpace())
                    return false;

                if (pos + 1 >= propName.Length)
                    return false;

                string remPropName = propName.Substring(pos + 1);

                if (target is IDictionary)
                {
                    IDictionary dict = target as IDictionary;
                    if (dict.Contains(spropName))
                        return GetNestedMember(dict[spropName], remPropName, ref parent, ref memberName, createSubKeysIfNotExists, factory);
                    else
                    {
                        if (!createSubKeysIfNotExists)
                            return false;
                        else
                        {
                            memberName = spropName;
                            var child = factory != null ? factory() : null;
                            if (child == null)
                                return false;

                            dict.Add(spropName, child);
                            return GetNestedMember(dict[spropName], remPropName, ref parent, ref memberName, createSubKeysIfNotExists, factory);
                        }
                    }
                }
                else if (target is IDictionary<string, object>)
                {
                    IDictionary<string, object> dict = target as IDictionary<string, object>;
                    if (dict.ContainsKey(spropName))
                        return GetNestedMember(dict[spropName], remPropName, ref parent, ref memberName, createSubKeysIfNotExists, factory);
                    else
                        return false;
                }
                else if (target is IList)
                {
                    int index = 0;
                    IList list = (IList)target;
                    if (int.TryParse(spropName, out index))
                    {
                        if (index < list.Count)
                            return GetNestedMember(list[index], remPropName, ref parent, ref memberName, createSubKeysIfNotExists, factory);
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                {
                    return GetNestedMember(ChoType.GetPropertyValue(target, spropName), remPropName, ref parent, ref memberName, createSubKeysIfNotExists, factory);
                }
            }
        }

        public static bool RemoveNestedPropertyValue(this object target, string propName)
        {
            object parent = null;
            string memberName = null;
            if (GetNestedMember(target, propName, ref parent, ref memberName))
            {
                if (parent is IDictionary)
                {
                    IDictionary dict = parent as IDictionary;
                    if (dict.Contains(memberName))
                    {
                        dict.Remove(memberName);
                        return true;
                    }
                }
                else if (parent is IDictionary<string, object>)
                {
                    IDictionary<string, object> dict = parent as IDictionary<string, object>;
                    if (dict.ContainsKey(memberName))
                    {
                        dict.Remove(memberName);
                        return true;
                    }
                }
                //else
                //{
                //    return ChoType.GetPropertyValue(parent, memberName);
                //}
            }

            return false;
        }
        public static object GetNestedPropertyValue(this object target, string propName)
        {
            object parent = null;
            string memberName = null;
            if (GetNestedMember(target, propName, ref parent, ref memberName))
            {
                if (parent is IDictionary)
                {
                    IDictionary dict = parent as IDictionary;
                    if (dict.Contains(memberName))
                        return dict[memberName];
                    else
                        return null;
                }
                else if (parent is IDictionary<string, object>)
                {
                    IDictionary<string, object> dict = parent as IDictionary<string, object>;
                    if (dict.ContainsKey(memberName))
                        return dict[memberName];
                    else
                        return null;
                }
                else
                {
                    return ChoType.GetPropertyValue(parent, memberName);
                }
            }
            else
                return null;
        }

        public static void SetNestedPropertyValue(this object target, string propName, object propValue,
            bool createSubKeysIfNotExists = false, Func<object> factory = null)
        {
            object parent = null;
            string memberName = null;
            if (GetNestedMember(target, propName, ref parent, ref memberName, createSubKeysIfNotExists, factory))
            {
                if (parent is IDictionary)
                {
                    IDictionary dict = parent as IDictionary;
                    if (dict.Contains(memberName))
                        dict[memberName] = propValue;
                    else if (createSubKeysIfNotExists)
                    {
                        dict.Add(memberName, propValue);
                        return;
                    }
                    else
                        return;
                }
                else if (parent is IDictionary<string, object>)
                {
                    IDictionary<string, object> dict = parent as IDictionary<string, object>;
                    if (dict.ContainsKey(memberName))
                        dict[memberName] = propValue;
                    else if (createSubKeysIfNotExists)
                    {
                        dict.Add(memberName, propValue);
                        return;
                    }
                    else
                        return;
                }
                else
                {
                    ChoType.SetPropertyValue(parent, memberName, propValue);
                }
            }
            else
            {
                var child = new ChoDynamicObject();
                return;
            }
        }

        private static ChoTypePropertyInfo GetTypePropertyInfo(Type type, PropertyInfo pi)
        {
            if (_typeCache.ContainsKey(type))
                return _typeCache[type][pi];

            lock (_padLock)
            {
                if (_typeCache.ContainsKey(type))
                    return _typeCache[type][pi];

                Dictionary<PropertyInfo, ChoTypePropertyInfo> dict = new Dictionary<PropertyInfo, ChoTypePropertyInfo>();
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(type))
                {
                    PropertyInfo lpi = pd.ComponentType.GetProperty(pd.Name);
                    object[] propConverters = ChoTypeDescriptor.GetTypeConverters(pi);
                    object[] propConverterParams = ChoTypeDescriptor.GetTypeConverterParams(pi);
                    int? size = null;
                    string formatText = null;
                    string nullValueText = null;

                    if (pd.Attributes.OfType<ChoFileRecordFieldAttribute>().Any())
                    {
                        var fa = pd.Attributes.OfType<ChoFileRecordFieldAttribute>().First();
                        size = fa.SizeInternal;
                        formatText = fa.FormatText;
                        nullValueText = fa.NullValue;
                    }
                    else
                    {
                        StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                        if (slAttr != null && slAttr.MaximumLength > 0)
                            size = slAttr.MaximumLength;

                        DisplayFormatAttribute dfAttr = pd.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
                        if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                            formatText = dfAttr.DataFormatString;
                        if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                            nullValueText = dfAttr.NullDisplayText;
                        if (formatText.IsNullOrWhiteSpace())
                            propConverterParams = new object[] { new object[] { formatText } };
                    }

                    dict.Add(lpi, new ChoTypePropertyInfo
                    {
                        FormatText = formatText,
                        PropConverterParams = propConverterParams,
                        PropConverters = propConverters,
                        Size = size,
                        NullValueText = nullValueText
                    });
                }
                _typeCache.Add(type, dict);

                return _typeCache[type][pi];
            }
        }

        public static T ConvertToObject<T>(this object source)
            //where T : class, new()
        {
            return (T)ConvertToObject(source, typeof(T));
        }

        public static object ConvertToObject(this object source, Type type, string valueNamePrefix = null)
        {
            if (source == null) return source;

            if (source is IDictionary<string, object>)
            {
                if (type == typeof(object))
                    return source;
                else if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                    return source;
                else if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    return source;
                }
                else
                    return ((IDictionary<string, object>)source).ToObject(type);
            }
            else
            {
                if (source is IDictionary)
                {
                    var dict = ((IDictionary)source).ToDictionary(valueNamePrefix: valueNamePrefix);
                    return dict.ToObject(type);
                }
                else
                {
                    Type sourceType = source.GetType();
                    object target = ChoActivator.CreateInstance(type);
                    string key = null;
                    object value = null;

                    foreach (var p in ChoType.GetProperties(type))
                    {
                        if (p.GetCustomAttribute<ChoIgnoreMemberAttribute>() != null)
                            continue;

                        key = p.Name;
                        var attr = p.GetCustomAttribute<ChoPropertyAttribute>();
                        if (attr != null && !attr.Name.IsNullOrWhiteSpace())
                            key = attr.Name.NTrim();

                        if (!ChoType.HasProperty(sourceType, key))
                            continue;
                        value = ChoType.GetPropertyValue(source, key);

                        p.SetValue(target, value.CastObjectTo(p.PropertyType));
                    }

                    return target;
                }
            }
        }

        public static void ConvertNSetValue(this object target, PropertyDescriptor pd, object fv, CultureInfo culture, long index = 0)
        {
            PropertyInfo pi = pd.ComponentType.GetProperty(pd.Name);
            IChoNotifyRecordFieldRead callbackRecord = target as IChoNotifyRecordFieldRead;
            if (callbackRecord != null)
            {
                object state = fv;
                bool retValue = ChoFuncEx.RunWithIgnoreError(() => callbackRecord.BeforeRecordFieldLoad(target, index, pd.Name, ref state), true);

                if (retValue)
                    fv = state;
            }

            try
            {
                var tpi = GetTypePropertyInfo(pd.ComponentType, pi);

                object[] propConverters = tpi.PropConverters;
                object[] propConverterParams = tpi.PropConverterParams;

                if (propConverters.IsNullOrEmpty())
                {
                    fv = ChoConvert.ConvertFrom(fv, pi.PropertyType, null, propConverters, propConverterParams, culture);
                }
                else
                {
                    fv = ChoConvert.ConvertFrom(fv, pi.PropertyType, null, propConverters, propConverterParams, culture);
                }
                pd.SetValue(target, fv);

                if (callbackRecord != null)
                    ChoFuncEx.RunWithIgnoreError(() => callbackRecord.AfterRecordFieldLoad(target, index, pd.Name, fv), true);
            }
            catch (Exception ex)
            {
                if (callbackRecord != null)
                {
                    bool ret = ChoFuncEx.RunWithIgnoreError(() => callbackRecord.RecordFieldLoadError(target, index, pd.Name, ref fv, ex), false);
                    if (!ret)
                    {
                        if (ex is ValidationException)
                            throw;

                        throw new ChoReaderException($"Failed to parse '{fv}' value for '{pd.Name}' field in '{target.GetType().Name}' object.", ex);
                    }
                    else
                        pd.SetValue(target, fv);
                }
            }
        }


        public static Dictionary<TKey, T> ToDictionaryFromObject<T, TKey>(this T target, Func<T, TKey> keySelector)
        {
            if (target == null || keySelector == null)
                return null;

            Dictionary<TKey, T> dict = new Dictionary<TKey, T>();
            TKey key = keySelector(target);
            dict.Add(key, target);

            return dict;
        }

        public static Dictionary<TKey, TValue> ToDictionaryFromObject<T, TKey, TValue>(this T target, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            if (target == null || keySelector == null || valueSelector == null)
                return null;

            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            TKey key = keySelector(target);
            TValue value = valueSelector(target);
            dict.Add(key, value);

            return dict;
        }

        private static object ToDictionaryInternal(this object target, string propName = null, string valueNamePrefix = null)
        {
            if (target == null)
                return null;
            else if (target.GetType() == typeof(object))
                return target;
            else if (target.GetType().GetUnderlyingType().IsSimple())
                return target;
            else if (target.GetType().GetUnderlyingType().IsSimpleSpecial())
                return target.ToNString();
            else
                return target.ToDictionary(propName, valueNamePrefix);
        }
        public static Dictionary<string, object> ToDictionary(this object target, string propName = null, string valueNamePrefix = null)
        {
            if (target == null || target is IChoReader || target is IChoWriter)
                return null;

            //ChoGuard.ArgumentNotNull(target, "Target");

            //if (target is IDictionary<string, object>)
            //    return (Dictionary<string, object>)target;
            if (target is IDictionary)
            {
                Dictionary<string, object> dict1 = new Dictionary<string, object>();
                foreach (var kvp in ((IDictionary)target).Keys)
                {
                    dict1.Add(kvp.ToNString(), ToDictionaryInternal(((IDictionary)target)[kvp], kvp.ToNString(), valueNamePrefix));
                }
                return dict1;
            }
            if (target is IEnumerable<KeyValuePair<string, object>>)
            {
                var list = new List<KeyValuePair<string, object>>(target as IEnumerable<KeyValuePair<string, object>>);
                return list.ToDictionary(x => x.Key, x => x.Value.ToDictionaryInternal(valueNamePrefix: valueNamePrefix));
            }
            if (target is IEnumerable<Tuple<string, object>>)
                return new List<Tuple<string, object>>(target as IEnumerable<Tuple<string, object>>).ToDictionary(x => x.Item1, x => x.Item2.ToDictionaryInternal(valueNamePrefix: valueNamePrefix));
            if (target is IList)
                return ((IList)(target)).OfType<object>().Select((item, index) =>
                {
                    return new KeyValuePair<string, object>($"{ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix)}{index + ChoETLSettings.ValueNameStartIndex}", item);
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionaryInternal(kvp.Key, valueNamePrefix: valueNamePrefix));

            string propNamex = null;
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
            {
                propNamex = pd.GetDisplayName(pd.Name);
                var value = ChoType.GetPropertyValue(target, pd.Name);
                if (value == null)
                    dict.Add(propNamex, value);
                else if (value.GetType().GetUnderlyingType().IsSimple())
                    dict.Add(propNamex, value);
                else if (value.GetType().GetUnderlyingType().IsSimpleSpecial())
                    dict.Add(propNamex, value.ToNString());
                else
                    dict.Add(propNamex, value.ToDictionaryInternal(propNamex, valueNamePrefix: valueNamePrefix));
            }

            return dict;
        }
        private static void ZipToDictionary(object target, Dictionary<string, object> dict, Action<Dictionary<string, object>, string, object> collisionResolver = null,
            string valueNamePrefix = null)
        {
            if (target == null)
                return;
            else if (target.GetType().GetUnderlyingType().IsSimple())
                return;
            else if (target.GetType().GetUnderlyingType().IsSimpleSpecial())
                return;

            if (target is IDictionary)
            {
                foreach (var kvp in ((IDictionary)target).Keys)
                {
                    if (!dict.ContainsKey(kvp.ToNString()))
                        dict.Add(kvp.ToNString(), ((IDictionary)target)[kvp]);
                    else if (collisionResolver != null)
                        collisionResolver(dict, kvp.ToNString(), ((IDictionary)target)[kvp]);
                }
            }
            if (target is IEnumerable<KeyValuePair<string, object>>)
            {
                foreach (var kvp in (IEnumerable<KeyValuePair<string, object>>)target)
                {
                    if (!dict.ContainsKey(kvp.Key))
                        dict.Add(kvp.Key, kvp.Value);
                    else if (collisionResolver != null)
                        collisionResolver(dict, kvp.Key, kvp.Value);
                }
            }
            if (target is IEnumerable<Tuple<string, object>>)
            {
                foreach (var kvp in (IEnumerable<Tuple<string, object>>)target)
                {
                    if (!dict.ContainsKey(kvp.Item1))
                        dict.Add(kvp.Item1, kvp.Item2);
                    else if (collisionResolver != null)
                        collisionResolver(dict, kvp.Item1, kvp.Item2);
                }
            }
            if (target is IList)
            {
                foreach (var kvp in ((IList)(target)).OfType<object>().Select((item, index) =>
                {
                    return new KeyValuePair<string, object>($"{ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix)}{index + ChoETLSettings.ValueNameStartIndex}", item);
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionaryInternal(kvp.Key)))
                {
                    if (!dict.ContainsKey(kvp.Key))
                        dict.Add(kvp.Key, kvp.Value);
                    else if (collisionResolver != null)
                        collisionResolver(dict, kvp.Key, kvp.Value);
                }
            }

            string propNamex = null;
            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
            {
                propNamex = pd.GetDisplayName(pd.Name);
                var value = ChoType.GetPropertyValue(target, pd.Name);
                if (!dict.ContainsKey(propNamex))
                    dict.Add(propNamex, value);
                else if (collisionResolver != null)
                    collisionResolver(dict, propNamex, value);
            }
        }

        public static Dictionary<string, object> ZipToDictionary(this IList target, Action<Dictionary<string, object>, string, object> collisionResolver = null)
        {

            if (target == null || target is IChoReader || target is IChoWriter)
                return null;

            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var item in target)
            {
                ZipToDictionary(item, dict, collisionResolver);
            }


            return dict;

        }
        public static Dictionary<string, object> ToSimpleDictionary(this object target, string valueNamePrefix = null)
        {
            if (target == null)
                return null;

            //ChoGuard.ArgumentNotNull(target, "Target");

            if (target is IDictionary<string, object>)
                return (Dictionary<string, object>)target;
            if (target is IDictionary)
            {
                Dictionary<string, object> dict1 = new Dictionary<string, object>();
                foreach (var kvp in ((IDictionary)target).Keys)
                {
                    dict1.Add(kvp.ToNString(), ((IDictionary)target)[kvp]);
                }
                return dict1;
            }
            if (target is IEnumerable<KeyValuePair<string, object>>)
                return new List<KeyValuePair<string, object>>(target as IEnumerable<KeyValuePair<string, object>>).ToDictionary(x => x.Key, x => x.Value);
            if (target is IEnumerable<Tuple<string, object>>)
                return new List<Tuple<string, object>>(target as IEnumerable<Tuple<string, object>>).ToDictionary(x => x.Item1, x => x.Item2);
            if (target is IList)
                return ((IList)(target)).OfType<object>().Select((item, index) =>
                {
                    return new KeyValuePair<string, object>($"{ChoETLSettings.GetValueNamePrefixOrDefault(valueNamePrefix)}{index + ChoETLSettings.ValueNameStartIndex}", item);
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            string propNamex = null;
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
            {
                propNamex = pd.GetDisplayName(pd.Name);
                var value = ChoType.GetPropertyValue(target, pd.Name);
                if (value == null)
                    dict.Add(propNamex, value);
                else if (value.GetType().GetUnderlyingType().IsSimple())
                    dict.Add(propNamex, value);
                else if (value.GetType().GetUnderlyingType().IsSimpleSpecial())
                    dict.Add(propNamex, value.ToNString());
                else
                    dict.Add(propNamex, value);
            }

            return dict;
        }

        public static bool IsNullOrEmpty(this ICollection @this)
        {
            return @this == null || @this.Count == 0;
        }

        public static bool IsNull(this object target)
        {
            return target == null;
        }

        public static bool IsNull(this char target)
        {
            return target == '\0';
        }

        public static bool IsNullOrDbNull(this object target)
        {
            return target == null || target == DBNull.Value;
        }

        public static object ToDbValue(this object target)
        {
            return target == null ? DBNull.Value : target;
        }

        public static object ToDbValue<T>(this T target)
        {
            if (typeof(T) == typeof(string))
                return (target as string).IsNullOrWhiteSpace() ? DBNull.Value : (object)target;
            else
                return EqualityComparer<T>.Default.Equals(target, default(T)) ? DBNull.Value : (object)target;
        }

        public static object ToDbValue<T>(this T target, T defaultValue)
        {
            return EqualityComparer<T>.Default.Equals(target, defaultValue) ? DBNull.Value : (object)target;
        }

        //public static bool SetFieldErrorMsg(this object target, string fieldName, string msg)
        //{
        //    PropertyInfo pi = null;
        //    if (ChoType.HasProperty(target.GetType(), "FieldErrorMsg", out pi)
        //        && !ChoType.IsReadOnlyMember(pi))
        //        ChoType.SetPropertyValue(target, pi, msg);
        //    else
        //        return false;

        //    return true;
        //}

        //public static bool SetErrorMsg(this object target, string msg)
        //{
        //    //if (target is ChoRecord)
        //    //    ((ChoRecord)target).SetErrorMsg(msg);
        //    //else
        //    //{
        //        MethodInfo mi = null;
        //        if (ChoType.HasMethod(target.GetType(), "SetErrorMsg", new Type[] { typeof(string) }, out mi))
        //            ChoType.SetPropertyValue(target, pi, msg);
        //        else
        //            return false;
        //    //}
        //    return true;
        //}

        //public static string GetErrorMsg(this object target)
        //{
        //    //if (target is ChoRecord)
        //    //    return ((ChoRecord)target).GetErrorMsg();
        //    //else
        //    //{
        //        PropertyInfo pi = null;
        //        if (ChoType.HasProperty(target.GetType(), "ErrorMsg", out pi))
        //            return ChoType.GetPropertyValue(target, pi).CastTo<string>();
        //        else
        //            return null;
        //    //}
        //}

        public static string GetXml(this object target, string tag = null)
        {
            if (target is ChoDynamicObject)
                return ((ChoDynamicObject)target).GetXml();
            else
                return ChoUtility.XmlSerialize(target);
        }

        #region JsonSerialize Overloads

        public static readonly DataContractJsonSerializerSettings jsonSettings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };

        public static MemoryStream AsStream(string path)
        {
            using (FileStream fileStream = File.OpenRead(path))
            {
                MemoryStream memStream = new MemoryStream();
                memStream.SetLength(fileStream.Length);
                fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);
                return memStream;
            }
        }


        public static void JsonSerialize(Stream sr, object target)
        {
            ChoGuard.ArgumentNotNull(sr, "Stream");
            ChoGuard.ArgumentNotNull(target, "Target");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(target.GetType(), jsonSettings);
            serializer.WriteObject(sr, target);
        }


        public static string JsonSerialize(object target, Encoding encoding = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");

            encoding = encoding == null ? Encoding.UTF8 : encoding;
            StringBuilder JsonString = new StringBuilder();
            using (MemoryStream ms = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                     ms, encoding, true, true, "  "))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(target.GetType(), jsonSettings);
                    serializer.WriteObject(writer, target);
                    writer.Flush();
                    byte[] json = ms.ToArray();
                    return encoding.GetString(json, 0, json.Length);
                }
            }
        }

        public static void JsonSerialize(string path, object target, Encoding encoding = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(path, "Path");
            ChoGuard.ArgumentNotNull(target, "Target");

            Directory.CreateDirectory(Path.GetDirectoryName(ChoPath.GetFullPath(path)));

            File.WriteAllText(path, JsonSerialize(target, encoding));
        }

        #endregion JsonSerialize Overloads

        #region JsonDeserialize Overloads

        public static T JsonDeserialize<T>(Stream sr)
        {
            return (T)JsonDeserialize(sr, typeof(T));
        }


        public static object JsonDeserialize(Stream sr, Type type)
        {
            ChoGuard.ArgumentNotNullOrEmpty(sr, "Stream");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            return serializer.ReadObject(sr);
        }


        public static T JsonDeserialize<T>(string JsonString, Encoding encoding = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(JsonString, "JsonString");

            return (T)JsonDeserialize(JsonString, typeof(T), encoding);
        }


        public static object JsonDeserialize(string JsonString, Type type, Encoding encoding = null)
        {
            ChoGuard.ArgumentNotNullOrEmpty(JsonString, "JsonString");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");
            encoding = encoding == null ? Encoding.UTF8 : encoding;

            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(JsonString)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
                return serializer.ReadObject(ms);
            }
        }


        public static T JsonDeserializeFromFile<T>(string path)
        {
            return (T)JsonDeserializeFromFile(path, typeof(T));
        }


        public static object JsonDeserializeFromFile(string path, Type type)
        {
            ChoGuard.ArgumentNotNullOrEmpty(path, "Path");
            ChoGuard.ArgumentNotNullOrEmpty(type, "Type");

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            return serializer.ReadObject(AsStream(path));
        }

        #endregion JsonDeserialize Overloads
    }
}
