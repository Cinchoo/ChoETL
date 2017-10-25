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
        public static bool IsPropertyExist(this object target, string name)
        {
            if (target is IDictionary<string, object>)
                return ((IDictionary<string, object>)target).ContainsKey(name);

            return target.GetType().GetProperty(name) != null;
        }

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

        public static dynamic ConvertToNestedObject(this object @this, char separator = '/')
        {
            if (separator == ChoCharEx.NUL)
                throw new ArgumentException("Invalid separator passed.");

            if (@this == null || !@this.GetType().IsDynamicType())
                return @this;

            IDictionary<string, object> expandoDic = null;
            expandoDic = @this is ExpandoObject || @this is ChoDynamicObject ? (IDictionary<string, object>)@this : ToExpandoObject(@this as DynamicObject);
            IDictionary<string, object> root = new ChoDynamicObject();

            foreach (var kvp in expandoDic)
            {
                if (kvp.Key.IndexOf(separator) >= 0)
                {
                    var tokens = kvp.Key.SplitNTrim(separator).Where(e => !e.IsNullOrWhiteSpace()).ToArray();
                    IDictionary<string, object> current = root;
                    foreach (var token in tokens.Take(tokens.Length - 1))
                    {
                        if (token.IsNullOrWhiteSpace())
                            continue;

                        if (!current.ContainsKey(token))
                            current.Add(token, new ChoDynamicObject());

                        current = current[token] as IDictionary<string, object>;
                    }
                    current.AddOrUpdate(tokens[tokens.Length - 1], kvp.Value);
                }
                else
                    root.Add(kvp.Key, kvp.Value);
            }

            return root as ChoDynamicObject;
        }

        public static dynamic ConvertToFlattenObject(this object @this, char separator = '/')
        {
            if (separator == ChoCharEx.NUL)
                throw new ArgumentException("Invalid separator passed.");

            if (@this == null || !@this.GetType().IsDynamicType())
                return @this;

            throw new NotImplementedException();
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
