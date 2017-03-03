using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace ChoETL
{
    public static class ChoUtility
    {
        private const char StartSeparator = '%';
        private const char EndSeparator = '%';
        private const char FormatSeparator = '^';

        static ChoUtility()
        {
            ChoMetadataTypesRegister.Init();
            TypeDescriptor.AddProvider(new ChoExpandoObjectTypeDescriptionProvider(), typeof(ExpandoObject));
        }

        public static void Init() 
        {

        }

        public static T FirstOrDefault<T>(this object value, T defaultValue = default(T))
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

        public static dynamic ToDynamicObject(this object src)
        {
            if (src == null) return null;

            IDictionary<string, object> expando = new ExpandoObject();
            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(src.GetType()))
            {
                if (pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any()) continue;

                try
                {
                    expando.Add(pd.Name, pd.GetValue(src));
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "ToDynamicObject: Error assinging value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(pd), ex.Message));
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

            return expando as ExpandoObject;
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

            if (src is ExpandoObject)
            {
                var srcDict = (IDictionary<string, object>)src;
                foreach (MemberInfo memberInfo in ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null))
                {
                    try
                    {
                        if (!srcDict.ContainsKey(memberInfo.Name)) continue;
                        ChoType.ConvertNSetMemberValue(dest, memberInfo, srcDict[memberInfo.Name]);
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
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

            if (src is ExpandoObject)
            {
                var srcDict = (IDictionary<string, object>)src;
                foreach (MemberInfo memberInfo in ChoType.GetMembers(dest.GetType()).Where(m => ChoType.GetAttribute<ChoMemberAttribute>(m) != null))
                {
                    try
                    {
                        if (!srcDict.ContainsKey(memberInfo.Name)) continue;
                        ChoType.ConvertNSetMemberValue(dest, memberInfo, srcDict[memberInfo.Name]);
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.TraceSwitch.TraceError, "Clone: Error assigning value for '{0}' member. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
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

        public static T GetValueAt<T>(this object array, int index, T defaultValue = default(T))
        {
            Type type = typeof(T).GetUnderlyingType();
            if (array is IList && index < ((IList)array).Count)
            {
                if (type.IsEnum)
                    return (T)Enum.Parse(type, ((IList)array)[index].ToNString());

                return (T)Convert.ChangeType(((IList)array)[index], type);
            }
            else
                return defaultValue;
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

        public static T ToObject<T>(this XmlNode node)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new XmlNodeReader(node));
        }

        public static object ToObject(this XmlNode node, Type type)
        {
            return ToObject(node, type, null);
        }

        public static object ToObject(this XmlNode node, Type type, XmlAttributeOverrides overrides)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            if (type == null)
                throw new ArgumentException("Type");

            XmlSerializer serializer = overrides != null ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
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

        public static string Serialize<T>(this T obj)
        {
            var serializer = new DataContractSerializer(obj.GetType());
            using (var writer = new StringWriter())
            using (var stm = new XmlTextWriter(writer))
            {
                serializer.WriteObject(stm, obj);
                return writer.ToString();
            }
        }
        public static T Deserialize<T>(this string serialized)
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
                yield return (T)System.Convert.ChangeType(item, typeof(T));
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
                            return (T)Convert.ChangeType(Type.GetType(@this as string), typeof(T));
                        else
                            return (T)Convert.ChangeType(@this, typeof(T));
                    }
                    else
                        return (T)Convert.ChangeType(@this, typeof(T));
                }
                catch
                {
                    if (defaultValue != null)
                        return defaultValue;

                    throw;
                }
            }
        }

        public static Type GetItemType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.IsArray)
                return type.GetElementType();

            foreach (Type @interface in type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                }
            }
            throw new ArgumentNullException("Invald '{0}' collection type passed.".FormatString(type.Name));
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

        public static void Loop(this IEnumerable e)
        {
            if (e == null) return;

            foreach (var x in e)
            { }
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

        public static void Bcp(this DataTable table, SqlConnection conn, string tableName = null, int batchSize = 0, Dictionary<string, string> columnMappings = null)
        {
            SqlBulkCopy bc = new SqlBulkCopy(conn);

            table.AcceptChanges();

            bc.DestinationTableName = !tableName.IsNullOrWhiteSpace() ? tableName : table.TableName;

            if (batchSize > 0)
                bc.BatchSize = batchSize;

            if (columnMappings != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in columnMappings)
                    bc.ColumnMappings.Add(keyValuePair.Key, keyValuePair.Value);
            }

            bc.WriteToServer(table);
        }

        internal static string Format(string format, object value)
        {
            if (value == null) return null;
            return format.IsNullOrWhiteSpace() ? value.ToString() : String.Format("{{0:{0}}}".FormatString(format), value);
        }

        public static string ExpandProperties(this string inString, ChoPropertyReplacer propertyReplacer = null)
        {
            return ExpandProperties(inString, StartSeparator, EndSeparator, FormatSeparator, propertyReplacer);
        }

        public static string ExpandProperties(string inString, char startSeparator, char endSeparator, char formatSeparator,
            ChoPropertyReplacer propertyReplacer = null /*IChoPropertyReplacer[] propertyReplacers */)
        {
            if (propertyReplacer == null)
                propertyReplacer = ChoPropertyReplacer.Default;

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
                                                    propertyNameNFormat.Length == 2 ? propertyNameNFormat[1] : null)))
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

        private static bool ReplaceToken(ChoPropertyReplacer propertyReplacer, StringBuilder message,
            string propertyName, string format)
        {
            string propertyValue;
            bool retValue = propertyReplacer.RaisePropertyReolve(propertyName, format, out propertyValue);
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
        public static void Seek(this StreamReader sr, int position, SeekOrigin origin)
        {
            sr.BaseStream.Seek(position, origin);
            sr.DiscardBufferedData();
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

        public static readonly DataContractJsonSerializerSettings jsonSettings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };

        public static string DumpAsJson(this object target, Encoding encoding = null)
        {
            encoding = encoding == null ? Encoding.UTF8 : encoding;

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

        public static string ToStringEx(this object target)
        {
            if (target == null) return String.Empty;

            if (target.GetType().IsSimple())
                return target.ToString();
            else if (target is IEnumerable)
            {
                StringBuilder arrMsg = new StringBuilder();

                int count = 0;
                foreach (object item in (IEnumerable)target)
                {
                    Type valueType = item.GetType();
                    if (valueType.IsGenericType)
                    {
                        Type baseType = valueType.GetGenericTypeDefinition();
                        if (baseType == typeof(KeyValuePair<,>))
                        {
                            object kvpKey = valueType.GetProperty("Key").GetValue(item, null);
                            object kvpValue = valueType.GetProperty("Value").GetValue(item, null);
                            arrMsg.AppendFormat("Key: {0} [Type: {2}]{1}", ToStringEx(kvpKey), Environment.NewLine, kvpValue == null ? "UNKNOWN" : kvpValue.GetType().Name);
                            arrMsg.AppendFormat("Value: {0}{1}", ToStringEx(kvpValue), Environment.NewLine);
                            count++;
                            continue;
                        }
                    }
                    count++;
                    arrMsg.AppendFormat("{0}{1}", ToStringEx(item), Environment.NewLine);
                }

                return "[Count: {0}]{1}{2}".FormatString(count, Environment.NewLine, arrMsg.ToString());
            }
            else
            {
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                ChoStringMsgBuilder msg = new ChoStringMsgBuilder(String.Format("{0} State", target.GetType().FullName));

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

                        if (!type.IsSimple() && type != typeof(Type))
                        {
                            memberText = value != null ? ChoUtility.ToStringEx(value) : "[NULL]";
                            if (memberText.ContainsMultiLines())
                                memberText = Environment.NewLine + memberText.Indent();
                        }
                        else
                            memberText = value.ToNString();

                        msg.AppendFormatLine("{0}: {1}", memberInfo.Name, memberText);
                    }
                }
                msg.AppendNewLine();

                return msg.ToString();
            }
        }

        public static Type GetElementType(this Type type)
        {
            if (typeof(Array).IsAssignableFrom(type))
                type = type.GetElementType();
            else if (typeof(IList).IsAssignableFrom(type))
                type = type.GetGenericArguments()[0];
            else
                return null;

            return type;
        }

        public static Array Cast(this Array array, Type elementType)
        {
            // assume there is at least one element in list
            Array arr = Array.CreateInstance(elementType, array.Length);
            Array.Copy(array, arr, array.Length);
            return arr;
        }

        public static IList Cast(this IList list, Type elementType)
        {
            Type listType = typeof(List<>).MakeGenericType(new Type[] { elementType });
            IList list1 = (IList)Activator.CreateInstance(listType);

            foreach (object t in list)
                list1.Add(t);

            return list1;
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
                return new Attribute[] {};

            if (property.GetCustomAttributes().Any(a =>  attributeType.IsAssignableFrom(a.GetType())))
                return (from x in property.GetCustomAttributes()
                        where attributeType.IsAssignableFrom(x.GetType())
                       select x).ToArray();

            if (property.DeclaringType != null)
            {
                var interfaces = property.DeclaringType.GetInterfaces();

                for (int i = 0; i < interfaces.Length; i++)
                {
                    Attribute[] attr = GetCustomAttributesEx(interfaces[i].GetProperty(property.Name), attributeType);
                    if (attr != null && attr.Length > 0)
                        return attr;
                }
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
}
