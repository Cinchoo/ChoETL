using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using SharpYaml.Serialization;

namespace ChoETL
{
    public static class ChoYamlExtensions
    {
        public static IEnumerable<YamlNode> SelectTokens(this YamlNode yamlNode, string yamlPath)
        {
            yield return yamlNode;
        }

        public static bool TryGetValue(this YamlNode yamlNode, string propName, StringComparison comparison, out YamlNode yamlOutNode)
        {
            yamlOutNode = null;

            if (yamlNode is YamlMappingNode)
            {
                YamlMappingNode mn = yamlNode as YamlMappingNode;
                var node = mn.Children.Where(kvp => String.Compare(kvp.Key.ToString(), propName, comparison) == 0).Select(kvp => kvp.Value).FirstOrDefault();
                if (node != null)
                {
                    yamlOutNode = node;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a YAML string to an <code>ExpandoObject</code>.
        /// </summary>
        /// <param name="yaml">The YAML string to convert.</param>
        /// <returns>Converted object.</returns>
        public static dynamic ToExpando(this string yaml, StringComparer comparer)
        {
            using (var sr = new StringReader(yaml))
            {
                var stream = new YamlStream();
                stream.Load(sr);
                var firstDocument = stream.Documents[0].RootNode;
                dynamic exp = ToExpando(firstDocument, comparer);
                return exp;
            }
        }

        /// <summary>
        /// Converts a YAML node to an <code>ExpandoObject</code>.
        /// </summary>
        /// <param name="node">The node to convert.</param>
        /// <returns>Converted object.</returns>
        public static dynamic ToExpando(this YamlNode node, StringComparer comparer)
        {
            if (node == null)
                return new ExpandoObject();

            object exp = (ExpandoObject)ToExpandoImpl(node, comparer);
            if (exp is ExpandoObject)
                return exp as ExpandoObject;
            else
            {
                dynamic exp1 = new ExpandoObject();
                exp1.Value = exp;

                return exp1;
            }
        }

        public static object Deserialize(this YamlNode node, StringComparer comparer)
        {
            return ToExpandoImpl(node, comparer);
        }

        private static bool TrySelectValue(this IList target, StringComparer comparer, string yamlToken, out object value)
        {
            value = null;

            object item1 = null;
            List<object> ret = new List<object>();

            foreach (var item in target)
            {
                item1 = item;
                if (item1 is IDictionary<object, object>)
                    item1 = ((IDictionary<object, object>)item).ToDictionary(kvp1 => kvp1.Key.ToNString(), kvp1 => kvp1.Value, comparer);

                if (item1 is IDictionary<string, object>)
                {
                    if (((IDictionary<string, object>)item1).ContainsKey(yamlToken))
                        ret.Add(((IDictionary<string, object>)item1)[yamlToken]);
                }
            }
            value = ret.Count == 0 ? null : ret;
            return ret.Count != 0;
        }

        public static bool TrySelectValue(this IDictionary<string, object> target, StringComparer comparer, string yamlPath, out object value, out bool iterateAllItems)
        {
            iterateAllItems = false;
            value = null;
            bool dictKey = false;
            bool dictValue = false;
            List<KeyValuePair<string, string>> tokens = new List<KeyValuePair<string, string>>();

            if (IsSimpleYamlPath(yamlPath, tokens, out dictKey, out dictValue))
            {
                if (dictKey)
                {
                    value = target.Keys.ToArray();
                    return true;
                }
                else if (dictValue)
                {
                    value = target.Values.ToArray();
                    return true;
                }
                else
                {
                    object t = target;
                    foreach (var kvp in tokens)
                    {
                        if (t is IDictionary<object, object>)
                            t = ((IDictionary<object, object>)t).ToDictionary(kvp1 => kvp1.Key.ToNString(), kvp1 => kvp1.Value, comparer);
                        else if (t is IList)
                        {
                            if (!TrySelectValue((IList)t, comparer, kvp.Key, out t))
                                return false;
                            else
                                continue;
                        }

                        if (t is IDictionary<string, object> && !((IDictionary<string, object>)t).ContainsKey(kvp.Key))
                            return false;
                        else
                        {
                            t = ((IDictionary<string, object>)t)[kvp.Key];

                            if (kvp.Value != null)
                            {
                                if (!(t is IList))
                                    return false;
                                else
                                {
                                    int index = -1;
                                    if (kvp.Value == "*")
                                    {
                                        t = ((IList)t).OfType<object>().ToArray();
                                        iterateAllItems = true;
                                    }
                                    else if (Int32.TryParse(kvp.Value, out index) && index < ((IList)t).Count)
                                    {
                                        t = ((IList)t)[index];
                                    }
                                    else
                                        return false;
                                }
                            }
                        }
                    }
                    value = t;
                    return true;
                }
            }
            else
            {
                throw new ChoParserException("Complex Yaml path not supported.");
            }
        }

        public static bool IsSimpleYamlPath(this string yamlPath, List<KeyValuePair<string, string>> tokens, out bool dictKey, out bool dictValue)
        {
            dictKey = false;
            dictValue = false;

            if (yamlPath.StartsWith("$"))
                yamlPath = yamlPath.Substring(1);
            while (yamlPath.StartsWith("."))
                yamlPath = yamlPath.Substring(1);
            if (yamlPath.Length == 0)
                return false;

            var tokens1 = yamlPath.SplitNTrim(".");
            if (String.Join("/", tokens1) == "~")
            {
                dictKey = true;
                return true;
            }
            else if (String.Join("/", tokens1) == "^")
            {
                dictValue = true;
                return true;
            }

            var outTokens = tokens;
            foreach (var token in tokens1.Select(t => t.NTrim()))
            {
                if (token.IsNullOrWhiteSpace())
                    return false;
                else if (token.EndsWith("]"))
                {
                    if (token.Contains("[") && token.IndexOf("[") < token.IndexOf("]"))
                    {
                        var t = token.Substring(0, token.IndexOf("["));
                        var i = token.Substring(token.IndexOf("[") + 1, token.Length - token.IndexOf("[") - 2);
                        outTokens.Add(new KeyValuePair<string, string>(t, i));
                        continue;
                    }
                    else
                        return false;
                }
                else if (!token.IsValidIdentifierEx())
                    return false;

                outTokens.Add(new KeyValuePair<string, string>(token, null));
            }

            return true;
        }

        private static void SetProperty(this IDictionary<string, object> target, string name, object thing)
        {
            target[name] = thing;
        }

        private static object ToExpandoImpl(YamlNode node, StringComparer comparer, ExpandoObject exp = null)
        {
            YamlScalarNode scalar = node as YamlScalarNode;
            YamlMappingNode mapping = node as YamlMappingNode;
            YamlSequenceNode sequence = node as YamlSequenceNode;

            if (scalar != null)
                return scalar.Value;
            else if (mapping != null)
            {
                exp = new ExpandoObject();
                foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
                {
                    YamlScalarNode keyNode = (YamlScalarNode)child.Key;
                    string keyName = keyNode.Value;
                    object val = ToExpandoImpl(child.Value, comparer);
                    exp.SetProperty(keyName, val);
                }
                return exp;
            }
            else if (sequence != null)
            {
                var childNodes = new List<object>();
                foreach (YamlNode child in sequence.Children)
                {
                    var childExp = new ExpandoObject();
                    object childVal = ToExpandoImpl(child, comparer, childExp);
                    childNodes.Add(childVal);
                }
                return childNodes;
            }

            return exp;
        }
    }

}
