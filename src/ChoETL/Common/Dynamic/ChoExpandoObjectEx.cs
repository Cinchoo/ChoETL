using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoExpandoObjectEx
    {
        public static void Print(this ExpandoObject expando)
        {
            ((IDictionary<string, object>)expando).Print();
        }

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

        private static bool IsKeyArrayValue(string token, string valueNamePrefix)
        {
            int index = -1;
            return IsKeyArrayValue(token, valueNamePrefix, out index);
        }
        private static bool IsKeyArrayValue(string token, string valueNamePrefix,
            out int index)
        {
            index = -1;
         
            if (token.IsNullOrWhiteSpace())
                return false;

            if (!token.StartsWith(valueNamePrefix))
                return false;
            token = token.Substring(valueNamePrefix.Length);

            if (Int32.TryParse(token, out index))
                return true;

            return false;
        }

        private static bool IsArrayItem(IDictionary<string, object> dict, string keyPrefix, int index, char separator = '/')
        {
            keyPrefix = $"{keyPrefix}{separator}";

            return dict.Keys
                .Where(k => k.StartsWith(keyPrefix))
                .Select(k => k.SplitNTrim(separator).Where(e => !e.IsNullOrWhiteSpace()).ToArray())
                .Where(tokens => tokens.Length >= index)
                .Select(tokens => tokens[index])
                .All(k => Int32.TryParse(k, out int i));
        }

        private static bool IsSimpleArrayItem(IDictionary<string, object> dict, string keyPrefix, int index, char separator = '/')
        {
            keyPrefix = $"{keyPrefix}{separator}";

            var result = dict.Keys
                .Where(k => k.StartsWith(keyPrefix))
                .Select(k => k.SplitNTrim(separator).Where(e => !e.IsNullOrWhiteSpace()).ToArray())
                .Where(tokens => tokens.Length == index + 1)
                .Select(tokens => tokens[index]).ToArray();
            
            return result.Length > 0 && result.All(k => Int32.TryParse(k, out int i));
        }

        public static dynamic ConvertToNestedObject(this object @this, char separator = '/', char? arrayIndexSeparator = null, 
            char? arrayEndIndexSeparator = null, bool allowNestedConversion = true,
            int maxArraySize = 100, string valueNamePrefix = null, int? valueNameStartIndex = null)
        {
            if (valueNamePrefix.IsNullOrWhiteSpace())
                valueNamePrefix = ChoETLSettings.ValueNamePrefix;
            //if (valueNamePrefix.IsNullOrWhiteSpace())
            //    valueNamePrefix = "Value";
            if (valueNameStartIndex == null)
                valueNameStartIndex = ChoETLSettings.ValueNameStartIndex;

            if (separator == ChoCharEx.NUL)
                throw new ArgumentException("Invalid separator passed.");
            if (arrayIndexSeparator == null || arrayIndexSeparator.Value == ChoCharEx.NUL)
                arrayIndexSeparator = separator;

            if (maxArraySize <= 0)
                maxArraySize = 100;

            if (@this == null)
                return @this;
            if (!(@this is ExpandoObject || @this is ChoDynamicObject || @this is IDictionary<string, object>))
                return @this;

            IDictionary<string, object> expandoDic = (IDictionary<string, object>)@this;
            IDictionary<string, object> root = new ChoDynamicObject(keySeparator: separator);

            foreach (var kvp in expandoDic)
            {
                if (kvp.Key.IndexOf(separator) >= 0)
                {
                    var tokens = kvp.Key.SplitNTrim(separator).Where(e => !e.IsNullOrWhiteSpace()).ToArray();
                    IDictionary<string, object> current = root;
                    List<ChoDynamicObject> currentArr = null;
                    string nextToken = null;
                    string nextNextToken = null;
                    string token = null;
                    bool handled = false;

                    int length = tokens.Length - 1;
                    int index = 0;
                    for (int i = 0; i < length; i++)
                    {
                        handled = false;
                        nextToken = null;
                        token = tokens[i];

                        if (i + 1 < tokens.Length)
                            nextToken = tokens[i + 1];
                        if (i + 2 < tokens.Length)
                            nextNextToken = tokens[i + 2];

                        if (token.IsNullOrWhiteSpace())
                            continue;

                        if (!valueNamePrefix.IsNullOrWhiteSpace() && nextNextToken == null && IsKeyArrayValue(nextToken, valueNamePrefix, out index))
                        {
                            if (index == valueNameStartIndex)
                                current.Add(token, new List<object>());

                            List<object> currentList = current[token] as List<object>;
                            currentList.Add(ConvertToNestedObject(kvp.Value, separator, arrayIndexSeparator, arrayEndIndexSeparator,
                                allowNestedConversion, maxArraySize, valueNamePrefix, valueNameStartIndex));
                            handled = true;
                            break;
                        }
                        else if (Int32.TryParse(nextToken, out index)
                            && index >= 0 && index < maxArraySize
                            && IsArrayItem(expandoDic, token, i + 1, separator)
                            )
                        {
                            if (IsSimpleArrayItem(expandoDic, token, i + 1, separator))
                            {
                                if (!current.ContainsKey(token))
                                    current.Add(token, new List<object>());

                                List<object> currentList = current[token] as List<object>;
                                currentList.Add(ConvertToNestedObject(kvp.Value, separator, arrayIndexSeparator, arrayEndIndexSeparator,
                                    allowNestedConversion, maxArraySize, valueNamePrefix, valueNameStartIndex));
                                handled = true;
                            }
                            else
                            {
                                if (!current.ContainsKey(token))
                                    current.Add(token, new List<ChoDynamicObject>());

                                currentArr = current[token] as List<ChoDynamicObject>;
                            }
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
                    if (!handled && current != null)
                        current.AddOrUpdate(tokens[tokens.Length - 1], kvp.Value);
                }
                else
                    root.Add(kvp.Key, kvp.Value);
            }

            var retVal = root.ConvertMembersToArrayIfAny(valueNamePrefix, valueNameStartIndex, maxArraySize);

            return root.ConvertMembersToArrayIfAny(arrayIndexSeparator, arrayEndIndexSeparator, allowNestedConversion);

            //return root as ChoDynamicObject;
        }

        private static dynamic ConvertMembersToArrayIfAny(this object @this, string valueNamePrefix = null, 
            int? valueNameStartIndex = null, int maxArraySize = 100)
        {
            bool allowNestedConversion = true;
            if (!(@this is IDictionary<string, object>))
                return @this;

            IDictionary<string, object> expandoDic = (IDictionary<string, object>)@this;
            string keySeparator = null;
            if (@this is ChoDynamicObject dobj)
                keySeparator = dobj.GetKeySeparator();

            IDictionary<string, object> root = new ChoDynamicObject(keySeparator: keySeparator.FirstOrDefault());

            if (!expandoDic.Keys.All(k => IsKeyArrayValue(k, valueNamePrefix)))
            {
                if (!allowNestedConversion)
                    return @this;
                else
                {
                    foreach (var kvp in expandoDic.ToArray())
                    {
                        if (kvp.Value is IDictionary<string, object>)
                        {
                            expandoDic[kvp.Key] = ConvertMembersToArrayIfAny(kvp.Value, valueNamePrefix, valueNameStartIndex, maxArraySize);
                        }
                    }
                    return expandoDic;
                }
            }

            List<object> newValue = new List<object>();
            foreach (var kvp in expandoDic)
            {
                //if (kvp.Value is IDictionary<string, object>)
                //    value = allowNestedConversion ? ConvertMembersToArrayIfAny(kvp.Value, valueNamePrefix, valueNameStartIndex, maxArraySize) 
                //        : kvp.Value;

                int index = -1;
                if (IsKeyArrayValue(kvp.Key, valueNamePrefix, out index))
                {
                    if (index >= 0)
                    {
                        newValue.Add(ConvertMembersToArrayIfAny(kvp.Value, valueNamePrefix, valueNameStartIndex, maxArraySize));
                    }
                }
            }

            return newValue;
        }

        public static dynamic ConvertMembersToArrayIfAny(this object @this, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null, bool allowNestedConversion = true)
        {
            char? startSeparator = null;
            char? endSeparator = null;
            if (arrayIndexSeparator == null)
            {
                switch (ChoETLSettings.ArrayBracketNotation)
                {
                    case ChoArrayBracketNotation.None:
                        startSeparator = arrayIndexSeparator.Value;
                        endSeparator = null;
                        break;
                    case ChoArrayBracketNotation.Parenthesis:
                        startSeparator = '(';
                        endSeparator = ')';
                        break;
                    case ChoArrayBracketNotation.Square:
                        startSeparator = '[';
                        endSeparator = ']';
                        break;
                }
            }
            else
            {
                startSeparator = arrayIndexSeparator;
                if (arrayEndIndexSeparator != null)
                    endSeparator = arrayEndIndexSeparator;
            }

            if (@this == null)
                return @this;
            if (!(@this is ExpandoObject || @this is ChoDynamicObject || @this is IDictionary<string, object>))
                return @this;

            IDictionary<string, object> expandoDic = (IDictionary<string, object>)@this;
            string keySeparator = null;
            if (@this is ChoDynamicObject dobj)
                keySeparator = dobj.GetKeySeparator();

            IDictionary<string, object> root = new ChoDynamicObject(keySeparator: keySeparator.FirstOrDefault());

            object value = null;
            foreach (var kvp in expandoDic)
            {
                value = allowNestedConversion ? ConvertMembersToArrayIfAny(kvp.Value, arrayIndexSeparator, arrayEndIndexSeparator, allowNestedConversion) : kvp.Value;

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

        public static dynamic ConvertToFlattenObject(this object @this, bool ignoreDictionaryFieldPrefix)
        {
            return ConvertToFlattenObject(@this, null, null, null, ignoreDictionaryFieldPrefix);
        }

        public static dynamic ConvertToFlattenObject(this object @this, char? nestedKeySeparator = null, char? arrayIndexSeparator = null,
            char? arrayEndIndexSeparator = null, bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null)
        {
            //if (@this == null || !@this.GetType().IsDynamicType())
            //    return @this;

            IDictionary<string, object> dict = @this as IDictionary<string, object>;
            if (dict == null)
                return @this;
            else
                return new ChoDynamicObject(dict.Flatten(nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix)
                    .ToDictionary(valueNamePrefix: valueNamePrefix));
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
