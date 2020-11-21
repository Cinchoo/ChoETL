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

        public static dynamic ConvertToNestedObject(this object @this, char separator = '/', char? arrayStartIndexSeparator = null, char? arrayEndIndexSeparator = null, bool allowNestedConversion = true,
            int maxArraySize = 100)
        {
            if (separator == ChoCharEx.NUL)
                throw new ArgumentException("Invalid separator passed.");

            if (@this == null)
                return @this;
            if (!(@this is ExpandoObject || @this is ChoDynamicObject || @this is IDictionary<string, object>))
                return @this;

            IDictionary<string, object> expandoDic = (IDictionary<string, object>)@this;
            IDictionary<string, object> root = new ChoDynamicObject();

            foreach (var kvp in expandoDic)
            {
                if (kvp.Key.IndexOf(separator) >= 0)
                {
                    var tokens = kvp.Key.SplitNTrim(separator).Where(e => !e.IsNullOrWhiteSpace()).ToArray();
                    IDictionary<string, object> current = root;
                    List<ChoDynamicObject> currentArr = null;
                    string nextToken = null;
                    string token = null;

                    int length = tokens.Length - 1;
                    int index = 0;
                    for (int i = 0; i < length; i++)
                    {
                        nextToken = null;
                        token = tokens[i];

                        if (i + 1 < length)
                            nextToken = tokens[i + 1];

                        if (token.IsNullOrWhiteSpace())
                            continue;

                        if (Int32.TryParse(nextToken, out index) && index >= 0 && index < maxArraySize)
                        {
                            if (!current.ContainsKey(token))
                                current.Add(token, new List<ChoDynamicObject>());

                            currentArr = current[token] as List<ChoDynamicObject>;
                        }
                        else
                        {
                            if (Int32.TryParse(token, out index))
                            {
                                if (index >= 0 && index < maxArraySize && currentArr != null)
                                {
                                    if (index < currentArr.Count)
                                        current = currentArr[index] as IDictionary<string, object>;
                                    else
                                    {
                                        int count = index - currentArr.Count + 1;
                                        for (int j = 0; j < count; j++)
                                            currentArr.Add(new ChoDynamicObject());
                                    }
                                    current = currentArr[index];
                                }
                            }
                            else if (current != null)
                            {
                                if (!current.ContainsKey(token))
                                    current.Add(token, new ChoDynamicObject(token));

                                current = current[token] as IDictionary<string, object>;
                                currentArr = null;
                            }
                        }
                    }
                    if (current != null)
                        current.AddOrUpdate(tokens[tokens.Length - 1], kvp.Value);
                }
                else
                    root.Add(kvp.Key, kvp.Value);
            }

            return root.ConvertMembersToArrayIfAny(arrayStartIndexSeparator, arrayEndIndexSeparator, allowNestedConversion);

            //return root as ChoDynamicObject;
        }

        public static dynamic ConvertMembersToArrayIfAny(this object @this, char? startSeparator = null, char? endSeparator = null, bool allowNestedConversion = true)
        {
            if (startSeparator == null || startSeparator.Value == ChoCharEx.NUL)
            {
                startSeparator = '[';
                if (endSeparator == null || endSeparator.Value == ChoCharEx.NUL)
                    endSeparator = ']';
            }

            if (@this == null)
                return @this;
            if (!(@this is ExpandoObject || @this is ChoDynamicObject || @this is IDictionary<string, object>))
                return @this;

            IDictionary<string, object> expandoDic = (IDictionary<string, object>)@this;
            IDictionary<string, object> root = new ChoDynamicObject();

            object value = null;
            foreach (var kvp in expandoDic)
            {
                value = allowNestedConversion ? ConvertMembersToArrayIfAny(kvp.Value, startSeparator, endSeparator, allowNestedConversion) : kvp.Value;

                var pos = kvp.Key.LastIndexOf(startSeparator.Value);
                if (pos <= 0)
                    root.Add(kvp.Key, value);
                else
                {
                    var key = kvp.Key.Substring(0, pos);
                    var indexValue = kvp.Key.Substring(pos + 1);
                    if (endSeparator != null && indexValue.IndexOf(endSeparator.Value) >= 0)
                        indexValue = indexValue.Substring(0, indexValue.IndexOf(endSeparator.Value));

                    int index = 0;

                    if (!int.TryParse(indexValue, out index))
                        root.Add(kvp.Key, value);
                    else
                    {
                        if (!root.ContainsKey(key))
                            root.Add(key, new object[] { });

                        var arrValue = root[key] as object[];
                        if (index + 1 > arrValue.Length)
                        {
                            Array.Resize(ref arrValue, index + 1);
                            root[key] = arrValue;
                        }

                        arrValue[index] = value;
                    }
                }
            }

            return root as ChoDynamicObject;
        }

        public static dynamic ConvertToFlattenObject(this object @this, char? nestedKeySeparator = null, char? arrayIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false)
        {
            //if (@this == null || !@this.GetType().IsDynamicType())
            //    return @this;

            IDictionary<string, object> dict = @this as IDictionary<string, object>;
            if (dict == null)
                return dict;
            else
                return new ChoDynamicObject(dict.Flatten(nestedKeySeparator, arrayIndexSeparator, ignoreDictionaryFieldPrefix).ToDictionary());
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
