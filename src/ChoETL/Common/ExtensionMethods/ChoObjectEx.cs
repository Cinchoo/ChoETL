using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoObjectEx
    {
        public static Dictionary<string, object> ToDictionary(this object target)
        {
            ChoGuard.ArgumentNotNull(target, "Target");

            if (target is IDictionary<string, object>)
                return (Dictionary<string, object>)target;

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
