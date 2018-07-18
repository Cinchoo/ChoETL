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

        public static object ConvertToObject<T>(this object source)
            where T : class, new()
        {
            return ConvertToObject(source, typeof(T));
        }

        public static object ConvertToObject(this object source, Type type)
        {
            if (source == null) return source;

            if (source is IDictionary<string, object>)
                return ((IDictionary<string, object>)source).ToObject(type);
            else
            {
                Type sourceType = source.GetType();
                object target = Activator.CreateInstance(type);
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
                    bool ret = ChoFuncEx.RunWithIgnoreError(() => callbackRecord.RecordFieldLoadError(target, index, pd.Name, fv, ex), false);
                    if (!ret)
                    {
                        if (ex is ValidationException)
                            throw;

                        throw new ChoReaderException($"Failed to parse '{fv}' value for '{pd.Name}' field in '{target.GetType().Name}' object.", ex);
                    }
                }
            }
        }

        public static Dictionary<string, object> ToDictionary(this object target)
        {
            ChoGuard.ArgumentNotNull(target, "Target");

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
            if (target is IList)
                return ((IList)(target)).OfType<object>().Select((item, index) => new KeyValuePair<string, object>("Column_{0}".FormatString(index), item)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (target is IEnumerable<KeyValuePair<string, object>>)
                return new List<KeyValuePair<string, object>>(target as IEnumerable<KeyValuePair<string, object>>).ToDictionary(x => x.Key, x => x.Value);
            if (target is IEnumerable<Tuple<string, object>>)
                return new List<Tuple<string, object>>(target as IEnumerable<Tuple<string, object>>).ToDictionary(x => x.Item1, x => x.Item2);

            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
            {
                dict.Add(pd.Name, ChoType.GetPropertyValue(target, pd.Name));
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
