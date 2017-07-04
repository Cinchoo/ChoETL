using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoExpandoObjectEx
    {
        public static ExpandoObject ToExpandoObject(this IEnumerable list)
        {
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;
            int index = 0;
            if (list != null)
            {
                foreach (var o in list)
                    expandoDic.Add(String.Format("Field{0}", ++index), o);
            }

            return expando;
        }
        public static ExpandoObject ToExpandoObject<TValue>(this IDictionary<string, TValue> dict)
        {
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;

            if (dict != null)
            {
                foreach (var kvp in dict)
                    expandoDic.Add(kvp.Key, kvp.Value);
            }

            return expando;
        }


        public static ExpandoObject ToExpandoObject(this DynamicObject dobj)
        {
            var expando = new ExpandoObject();
            if (dobj != null)
            {
                var expandoDic = (IDictionary<string, object>)expando;
                foreach (var propName in dobj.GetDynamicMemberNames())
                {
                    expandoDic.Add(propName, GetDynamicMember(dobj, propName));
                }
            }

            return expando;
        }

        private static object GetDynamicMember(object obj, string memberName)
        {
            var binder = Binder.GetMember(CSharpBinderFlags.None, memberName, obj.GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);
            return callsite.Target(callsite, obj);
        }
    }
}
