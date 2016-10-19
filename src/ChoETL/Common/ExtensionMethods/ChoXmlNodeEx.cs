using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace ChoETL
{
    public static class ChoXmlNodeEx
    {
        #region Instance Members (Public)

        public static string ToIndentedXml(this XmlNode xmlNode)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Create a XMLTextWriter that will send its output to a memory stream (file)
                using (XmlTextWriter xtw = new XmlTextWriter(ms, Encoding.Unicode))
                {
                    // Set the formatting property of the XML Text Writer to indented
                    // the text writer is where the indenting will be performed
                    xtw.Formatting = Formatting.Indented;

                    // write dom xml to the xmltextwriter
                    xmlNode.WriteTo(xtw);
                    // Flush the contents of the text writer
                    // to the memory stream, which is simply a memory file
                    xtw.Flush();

                    // set to start of the memory stream (file)
                    ms.Seek(0, SeekOrigin.Begin);
                    // create a reader to read the contents of 
                    // the memory stream (file)
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        // return the formatted string to caller
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        public static string GetXPathToNode(this XmlNode node, int level)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            int currentLevel = 0;
            StringBuilder msg = new StringBuilder();
            AppendXPathToNode(node, msg, level, ref currentLevel);

            return msg.ToString();
        }

        public static string GetXPathToNode(this XmlNode node)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            if (node.NodeType == XmlNodeType.Attribute)
            {
                // attributes have an OwnerElement, not a ParentNode; also they have
                // to be matched by name, not found by position
                return String.Format(
                    "{0}/@{1}",
                    GetXPathToNode(((XmlAttribute)node).OwnerElement),
                    node.Name
                    );
            }
            if (node.ParentNode == null)
            {
                // the only node with no parent is the root node, which has no path
                return "";
            }
            //get the index
            int iIndex = 1;
            XmlNode xnIndex = node;
            while (xnIndex.PreviousSibling != null) { iIndex++; xnIndex = xnIndex.PreviousSibling; }
            // the path to a node is the path to its parent, plus "/node()[n]", where 
            // n is its position among its siblings.
            return String.Format(
                "{0}/node()[{1}]",
                GetXPathToNode(node.ParentNode),
                iIndex
                );
        }

        public static void SetOuterXml(this XmlNode node, string outerXml)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            if (outerXml.IsNullOrEmpty())
                throw new ArgumentNullException("OuterXml");

            //Remove all attributes and elements
            node.RemoveAll();

            XmlDocument newDoc = new XmlDocument();
            using (XmlTextReader reader = new XmlTextReader(new StringReader(outerXml)))
                newDoc.Load(reader);

            foreach (XmlAttribute attribute in newDoc.DocumentElement.Attributes)
                node.Attributes.Append(node.OwnerDocument.CreateAttribute(attribute.Name)).Value = attribute.Value;

            node.InnerXml = newDoc.DocumentElement.InnerXml;
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

        public static IDictionary ToDictionary(this XmlNode region)
        {
            return GetDictionary(null, region, "key", "value");
        }

        public static IDictionary ToDictionary(this XmlNode region, string keyElementName, string valueElementName)
        {
            return GetDictionary(null, region, keyElementName, valueElementName);
        }

        public static NameValueCollection ToNameValues(this XmlNode region)
        {
            return GetNameValues(null, region, "key", "value");
        }

        public static NameValueCollection ToNameValues(this XmlNode region, string keyElementName, string valueElementName)
        {
            return GetNameValues(null, region, keyElementName, valueElementName);
        }

        public static NameValueCollection ToNameValuesFromAttributes(this XmlNode region)
        {
            return GetNameValuesFromAttributes(null, region);
        }

        public static XmlNode MakeXPath(this XmlNode node, string xpath)
        {
            return MakeXPath(node, node, xpath);
        }

        public static XmlNode MakeXPath(this XmlNode node, XmlNode parent, string xpath)
        {
            //// grab the next node name in the xpath; or return parent if empty
            //string[] partsOfXPath = xpath.Trim('/').Split('/');
            //string nextNodeInXPath = partsOfXPath.First();
            //if (string.IsNullOrEmpty(nextNodeInXPath))
            //    return parent;

            //// get or create the node from the name
            //XmlNode node = parent.SelectSingleNode(nextNodeInXPath);
            //if (node == null)
            //    node = parent.AppendChild(doc.CreateElement(nextNodeInXPath));

            //// rejoin the remainder of the array as an xpath expression and recurse
            //string rest = String.Join("/", partsOfXPath.Skip(1).ToArray());
            //return MakeXPath(doc, node, rest);
            XmlNode selectNode = parent;
            foreach (string part in xpath.Trim('/').Split('/'))
            {
                XmlNodeList nodes = selectNode.SelectNodes(part);
                if (nodes.Count > 1)
                    throw new ApplicationException("Xpath '" + xpath + "' was found multiple times!");
                else if (nodes.Count == 1)
                {
                    selectNode = nodes[0];
                    continue;
                }

                if (part.StartsWith("@"))
                {
                    var anode = node.OwnerDocument.CreateAttribute(part.Substring(1));
                    selectNode.Attributes.Append(anode);
                    selectNode = anode;
                }
                else
                {
                    string elName, attrib = null;
                    if (part.Contains("["))
                    {
                        part.SplitOnce("[", out elName, out attrib);
                        if (!attrib.EndsWith("]"))
                            throw new ApplicationException("Unsupported XPath (missing ]): " + part);
                        attrib = attrib.Substring(0, attrib.Length - 1);
                    }
                    else
                        elName = part;

                    XmlNode next = node.OwnerDocument.CreateElement(elName);
                    selectNode.AppendChild(next);
                    selectNode = next;

                    if (attrib != null)
                    {
                        foreach (string token in attrib.SplitNTrim(' '))
                        {
                            if (token.IsNullOrEmpty())
                                continue;

                            string[] keyValuePair = token.SplitNTrim('=');
                            if (keyValuePair == null || keyValuePair.Length != 2)
                                continue;

                            string name = keyValuePair[0];
                            string value = keyValuePair[1];

                            if (!name.StartsWith("@"))
                                throw new ApplicationException("Unsupported XPath attrib (missing @): " + part);
                            name = name.Substring(1);

                            if (!value.StartsWith("'") && !value.EndsWith("'"))
                                throw new ApplicationException("Unsupported XPath attrib: " + part);
                            value = value.Substring(1, value.Length - 2);

                            var anode = node.OwnerDocument.CreateAttribute(name);
                            anode.Value = value;
                            selectNode.Attributes.Append(anode);
                        }
                        //if (!attrib.StartsWith("@"))
                        //    throw new ApplicationException("Unsupported XPath attrib (missing @): " + part);
                        //string name, value;
                        //attrib.Substring(1).SplitOnce("='", out name, out value);
                        //if (string.IsNullOrEmpty(value) || !value.EndsWith("'"))
                        //    throw new ApplicationException("Unsupported XPath attrib: " + part);
                        //value = value.Substring(0, value.Length - 1);
                        //var anode = doc.CreateAttribute(name);
                        //anode.Value = value;
                        //node.Attributes.Append(anode);
                    }
                }
            }
            return selectNode;
        }
        private static void SplitOnce(this string value, string separator, out string part1, out string part2)
        {
            if (value != null)
            {
                int idx = value.IndexOf(separator);
                if (idx >= 0)
                {
                    part1 = value.Substring(0, idx);
                    part2 = value.Substring(idx + separator.Length);
                }
                else
                {
                    part1 = value;
                    part2 = null;
                }
            }
            else
            {
                part1 = "";
                part2 = null;
            }
        }

        #endregion Instance Members (Public)

        #region Instance Members (Private)

        private static NameValueCollection GetNameValuesFromAttributes(NameValueCollection prev,
                                        XmlNode region)
        {
            NameValueCollection coll =
                    new NameValueCollection();

            if (prev != null)
                coll.Add(prev);

            ChoCollectionWrapper result = new ChoCollectionWrapper(coll);
            if (region != null)
            {
                result = ReadAttributes(result, region);
                if (result == null)
                    return null;
            }

            return result.UnWrap() as NameValueCollection;
        }

        private static NameValueCollection GetNameValues(NameValueCollection prev,
                                        XmlNode region,
                                        string nameAtt,
                                        string valueAtt)
        {
            //ChoGuard.ArgumentNotNull(region, "region");
            NameValueCollection coll = new NameValueCollection();

            if (prev != null)
                coll.Add(prev);

            ChoCollectionWrapper result = new ChoCollectionWrapper(coll);
            if (region != null)
            {

                result = Read(result, region, nameAtt, valueAtt);
                if (result == null)
                    return null;
            }

            return result.UnWrap() as NameValueCollection;
        }

        private static ChoCollectionWrapper ReadAttributes(ChoCollectionWrapper result,
                                XmlNode region)
        {
            if (region.Attributes != null && region.Attributes.Count != 0)
            {
                foreach (XmlAttribute attribute in region.Attributes)
                {
                    if (attribute == null)
                        continue;
                    result[attribute.Name] = attribute.Value;
                }
            }

            return result;
        }

        private static void AppendXPathToNode(XmlNode node, StringBuilder nodePath, int maxLevel, ref int currentLevel)
        {
            if (node == null)
                return;

            if (nodePath.Length == 0)
                nodePath.Append(node.Name);
            else
                nodePath.Insert(0, "{0}\\".FormatString(node.Name));

            currentLevel++;
            if (currentLevel == maxLevel)
                return;

            AppendXPathToNode(node.ParentNode, nodePath, maxLevel, ref currentLevel);
        }

        private static IDictionary GetDictionary(IDictionary prev,
                               XmlNode region,
                               string nameAtt,
                               string valueAtt)
        {
            Hashtable hashtable;
            if (prev == null)
                hashtable = new Hashtable();
            else
            {
                Hashtable aux = (Hashtable)prev;
                hashtable = (Hashtable)aux.Clone();
            }

            ChoCollectionWrapper result = new ChoCollectionWrapper(hashtable);
            if (region != null)
            {
                result = Read(result, region, nameAtt, valueAtt);
                if (result == null)
                    return null;
            }

            return result.UnWrap() as IDictionary;
        }

        private static ChoCollectionWrapper Read(ChoCollectionWrapper result,
                                XmlNode region,
                                string nameAtt,
                                string valueAtt)
        {
            //if (region.Attributes != null && region.Attributes.Count != 0)
            //    throw new ChoConfigurationException("Unknown attribute", region);

            XmlNode keyNode;
            XmlNode valueNode;
            XmlNodeList childs = region.ChildNodes;
            foreach (XmlNode node in childs)
            {
                XmlNodeType ntype = node.NodeType;
                if (ntype == XmlNodeType.Whitespace || ntype == XmlNodeType.Comment)
                    continue;

                if (ntype != XmlNodeType.Element)
                    throw new XmlException("Only XmlElement allowed");

                string nodeName = node.Name;
                if (nodeName == "clear")
                {
                    if (node.Attributes != null && node.Attributes.Count != 0)
                        throw new XmlException("Unknown attribute");

                    result.Clear();
                }
                else if (nodeName == "remove")
                {
                    keyNode = null;
                    if (node.Attributes != null)
                        keyNode = node.Attributes[nameAtt]; //.RemoveNamedItem(nameAtt);

                    if (keyNode == null)
                        throw new XmlException("Required attribute not found");
                    if (keyNode.Value == String.Empty)
                        throw new XmlException("Required attribute is empty");

                    //if (node.Attributes.Count != 0)
                    //    throw new ChoConfigurationException("Unknown attribute", node);

                    result.Remove(keyNode.Value);
                }
                else if (nodeName == "add")
                {
                    keyNode = null;
                    if (node.Attributes != null)
                        keyNode = node.Attributes[nameAtt]; //.RemoveNamedItem(nameAtt);

                    if (keyNode == null)
                        throw new XmlException("Required attribute not found");
                    if (keyNode.Value == String.Empty)
                        throw new XmlException("Required attribute is empty");

                    valueNode = node.Attributes[valueAtt]; //.RemoveNamedItem(valueAtt);
                    if (valueNode == null)
                    {
                        //has value element
                        string xpath = "//add[@{0}='{1}']/value".FormatString(nameAtt, keyNode.Value);
                        XmlElement valueElement = region.SelectSingleNode(xpath) as XmlElement;
                        if (valueElement == null)
                            throw new XmlException("Required attribute not found");
                        else
                        {
                            result[keyNode.Value] = valueElement.InnerXml;
                        }
                    }
                    else
                        result[keyNode.Value] = valueNode.Value;
                    //if (node.Attributes.Count != 0)
                    //    throw new ChoConfigurationException("Unknown attribute", node);

                }
                else
                {
                    //throw new ChoConfigurationException("Unknown element", node);
                }
            }

            return result;
        }
        #endregion Instance Members (Private)

        #region CollectionWrapper Class

        private class ChoCollectionWrapper
        {
            IDictionary _dictionary;
            NameValueCollection _collection;
            bool _isDictionary;

            public ChoCollectionWrapper(IDictionary dictionary)
            {
                this._dictionary = dictionary;
                _isDictionary = true;
            }

            public ChoCollectionWrapper(NameValueCollection collection)
            {
                this._collection = collection;
                _isDictionary = false;
            }

            public void Remove(string s)
            {
                if (_isDictionary)
                    _dictionary.Remove(s);
                else
                    _collection.Remove(s);
            }

            public void Clear()
            {
                if (_isDictionary)
                    _dictionary.Clear();
                else
                    _collection.Clear();
            }

            public string this[string key]
            {
                set
                {
                    if (_isDictionary)
                        _dictionary[key] = value;
                    else
                        _collection[key] = value;
                }
            }

            public object UnWrap()
            {
                if (_isDictionary)
                    return _dictionary;
                else
                    return _collection;
            }
        }

        #endregion
    }
}