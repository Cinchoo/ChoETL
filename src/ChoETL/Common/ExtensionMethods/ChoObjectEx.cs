using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoObjectEx
    {
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
    }
}
