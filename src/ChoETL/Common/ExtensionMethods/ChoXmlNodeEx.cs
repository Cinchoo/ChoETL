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
using System.Xml.Linq;

namespace ChoETL
{
    public enum ChoEmptyXmlNodeValueHandling { Null, Ignore, Empty }

    public static class ChoXmlSettings
    {
        private static string _XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        public static string XmlSchemaInstanceNamespace
        {
            get { return _XmlSchemaInstanceNamespace; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;

                _XmlSchemaInstanceNamespace = value;

            }
        }
        private static string _XmlNamespace = "http://www.w3.org/2000/xmlns/";
        public static string XmlNamespace
        {
            get { return _XmlNamespace; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;

                _XmlNamespace = value;
            }
        }
        private static string _XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public static string XmlSchemaNamespace
        {
            get { return _XmlSchemaNamespace; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;

                _XmlSchemaNamespace = value;
            }
        }
        private static string _JSONSchemaNamespace = "http://james.newtonking.com/projects/json";
        public static string JSONSchemaNamespace
        {
            get { return _JSONSchemaNamespace; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;

                _JSONSchemaNamespace = value;
            }
        }
    }

    public static class ChoXmlNodeEx
    {
        #region Instance Members (Public)

        public static IEnumerable<XElement> GetXmlElements(this XmlReader xmlReader, string xPath)
        {
            //if (xPath.IsNullOrWhiteSpace()) yield break;
            if (!xPath.IsNullOrWhiteSpace() && (xPath == "/" || xPath == "//"))
            {
                string rootNodeName = null;
                if (xmlReader.MoveToContent() == XmlNodeType.Element)
                    rootNodeName = xmlReader.Name;
                yield return XElement.ReadFrom(xmlReader)
                      as XElement;

            }
            else if (xPath.IsNullOrWhiteSpace() || xPath == "//*" || xPath == "./*")
            {
                while (xmlReader.Read())
                {
                    // first element is the root element
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        break;
                    }
                }
                bool isEmpty;
                // Empty element?
                isEmpty = xmlReader.IsEmptyElement;

                // Decode elements
                if (isEmpty == false)
                {
                    // Read the root start element
                    xmlReader.ReadStartElement();

                    do
                    {
                        // Read document till next element
                        xmlReader.MoveToContent();

                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            string elementName = xmlReader.LocalName;

                            // Empty element?
                            isEmpty = xmlReader.IsEmptyElement;

                            // Decode child element
                            XElement el = XElement.ReadFrom(xmlReader)
                                                  as XElement;
                            if (el != null)
                                yield return el;
                            xmlReader.MoveToContent();
                        }
                        else if (xmlReader.NodeType == XmlNodeType.Text)
                        {
                            xmlReader.Skip();   // Skip text
                        }
                        else
                            break;
                    } while (xmlReader.NodeType != XmlNodeType.EndElement);
                }
                else
                {
                    // Decode child element
                    XElement el = XElement.ReadFrom(xmlReader)
                                          as XElement;
                    if (el != null)
                        yield return el;

                }
            }
            else
            {
                //string rootName = null;
                string[] matchNames = xPath.SplitNTrim("/").Where(i => !i.IsNullOrWhiteSpace() && i.NTrim() != "." && i.NTrim() != "..").ToArray();
                if (matchNames.Length == 0) yield break;
                foreach (var ele in StreamElements(xmlReader, matchNames))
                    yield return ele;
            }
        }

        public static IEnumerable<XElement> StreamElements(XmlReader reader, string[] elementNames)
        {
            if (elementNames.Length == 1)
            {
                string elementName = elementNames[0];
                if (elementName == "*")
                {
                    bool isEmpty = reader.IsEmptyElement;
                    reader.ReadStartElement();
                    if (isEmpty == false)
                    {
                        do
                        {
                            // Read document till next element
                            reader.MoveToContent();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                // Empty element?
                                isEmpty = reader.IsEmptyElement;

                                // Decode child element
                                XElement el = XElement.ReadFrom(reader)
                                                      as XElement;
                                if (el != null)
                                    yield return el;

                                reader.MoveToContent();
                            }
                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                reader.Skip();   // Skip text
                            }
                        } while (reader.NodeType != XmlNodeType.EndElement);
                    }
                }
                else
                {
                    while (reader.ReadToFollowing(elementName))
                        yield return (XElement)XNode.ReadFrom(reader);
                }
            }
            else
            {
                string elementName = elementNames[0];
                if (elementName == "*")
                {
                    bool isEmpty = reader.IsEmptyElement;
                    reader.ReadStartElement();
                    if (isEmpty == false)
                    {
                        do
                        {
                            // Read document till next element
                            reader.MoveToContent();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                // Empty element?
                                isEmpty = reader.IsEmptyElement;

                                foreach (var i in StreamElements(reader, elementNames.Skip(1).ToArray()))
                                    yield return i;

                                reader.MoveToContent();
                            }
                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                reader.Skip();   // Skip text
                            }
                        } while (reader.NodeType != XmlNodeType.EndElement);
                    }
                }
                else
                {
                    while (reader.ReadToDescendant(elementName))
                    {
                        foreach (var i in StreamElements(reader, elementNames.Skip(1).ToArray()))
                            yield return i;
                    }
                }
            }
        }

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
        public static string GetInnerXml(this XNode node)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            var reader = node.CreateReader();
            reader.MoveToContent();

            return reader.ReadInnerXml().Trim();
        }
        public static string GetOuterXml(this XNode node)
        {
            if (node == null)
                throw new ArgumentNullException("XmlNode");

            var reader = node.CreateReader();
            reader.MoveToContent();

            return reader.ReadOuterXml().Trim();
        }

        public static bool IsNilElement(this XElement element, string xmlSchemaNS = null)
        {
            XNamespace ns = xmlSchemaNS.IsNullOrWhiteSpace() ? ChoXmlSettings.XmlSchemaInstanceNamespace : xmlSchemaNS;

            XAttribute nil = element.Attribute(ns + "nil");
            return nil != null;
        }

        public static string NilAwareValue(this XElement element, string xmlSchemaNS = null)
        {
            XNamespace ns = xmlSchemaNS.IsNullOrWhiteSpace() ? ChoXmlSettings.XmlSchemaInstanceNamespace : xmlSchemaNS;

            XAttribute nil = element.Attribute(ns + "nil");
            return nil != null && (bool)nil ? null : element.IsEmpty ? null : element.Value;
        }

        public static bool IsJsonArray(this XElement element, string jsonSchemaNS = null)
        {
            XNamespace ns = jsonSchemaNS.IsNullOrWhiteSpace() ? ChoXmlSettings.JSONSchemaNamespace : jsonSchemaNS;

            XAttribute nil = element.Attribute(ns + "Array");
            return nil != null && (bool)nil;
        }

        private static bool HasAttributes(this XElement element, string xmlSchemaNS = null, string jsonSchemaNS = null)
        {
            bool hasAttr = false;
            foreach (var attribute in element.Attributes())
            {
                string ns = attribute.Name.Namespace.ToString();
                if (xmlSchemaNS != null && ns.StartsWith(xmlSchemaNS, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (jsonSchemaNS != null && ns.StartsWith(jsonSchemaNS, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (ns.StartsWith(ChoXmlSettings.XmlSchemaInstanceNamespace, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (ns.StartsWith(ChoXmlSettings.JSONSchemaNamespace, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                if (ns.StartsWith(ChoXmlSettings.XmlNamespace, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                hasAttr = true;
            }

            return hasAttr;
        }

        public static dynamic ToDynamic(this XElement element, string xmlSchemaNS = null, string jsonSchemaNS = null, ChoEmptyXmlNodeValueHandling emptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Null,
            bool retainXmlAttributesAsNative = true)
        {
            // loop through child elements
            // define an Expando Dynamic
            dynamic obj = new ChoDynamicObject(element.Name.LocalName);

            bool hasAttr = false;
            // cater for attributes as properties
            if (element.HasAttributes(xmlSchemaNS, jsonSchemaNS))
            {
                foreach (var attribute in element.Attributes())
                {
					if (attribute.Name.ToString().StartsWith("xmlns"))
						continue;

                    string ns = attribute.Name.Namespace.ToString();
                    if (xmlSchemaNS != null && ns.StartsWith(xmlSchemaNS, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (jsonSchemaNS != null && ns.StartsWith(jsonSchemaNS, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (ns.StartsWith(ChoXmlSettings.XmlSchemaInstanceNamespace, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (ns.StartsWith(ChoXmlSettings.JSONSchemaNamespace, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (ns.StartsWith(ChoXmlSettings.XmlNamespace, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    hasAttr = true;
                    if (retainXmlAttributesAsNative)
                        ((IDictionary<string, object>)obj).Add("@{0}".FormatString(attribute.Name.LocalName), attribute.Value);
                    else
                        ((IDictionary<string, object>)obj).Add("{0}".FormatString(attribute.Name.LocalName), attribute.Value);
                }
            }

            // cater for child nodes as properties, or child objects
            if (element.HasElements)
            {
                foreach (var kvp in element.Elements().GroupBy(e => e.Name.LocalName).Select(g => new { Name = g.Key, Value = g.ToArray() }))
                {
                    if (kvp.Value.Length == 1 && !kvp.Value.First().IsJsonArray(jsonSchemaNS))
                    {
                        XElement subElement = kvp.Value.First();
                        if (subElement.HasAttributes(xmlSchemaNS, jsonSchemaNS) || subElement.HasElements)
                        {
                            string keyName = null;
                            object dobj = ToDynamic(subElement, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative);
                            if (dobj != null || (dobj == null && emptyXmlNodeValueHandling != ChoEmptyXmlNodeValueHandling.Ignore))
                            {
                                keyName = subElement.Name.LocalName;
                                ((IDictionary<string, object>)obj).Add(keyName, dobj);
                            }
                        }
                        else
                        {
                            if (subElement.IsNilElement())
                            {
                                ((IDictionary<string, object>)obj).Add(subElement.Name.LocalName, subElement.NilAwareValue(xmlSchemaNS));
                            }
                            else
                            {
                                string value = subElement.NilAwareValue(xmlSchemaNS);
                                if (value.IsNullOrEmpty() && !subElement.HasAttributes())
                                {
                                    switch (emptyXmlNodeValueHandling)
                                    {
                                        case ChoEmptyXmlNodeValueHandling.Empty:
                                            ((IDictionary<string, object>)obj).Add(subElement.Name.LocalName, value);
                                            break;
                                        case ChoEmptyXmlNodeValueHandling.Null:
                                            value = value.IsNullOrEmpty() ? null : value;
                                            ((IDictionary<string, object>)obj).Add(subElement.Name.LocalName, value);
                                            break;
                                    }
                                }
                                else
                                    ((IDictionary<string, object>)obj).Add(subElement.Name.LocalName, value);
                            }
                        }
                    }
                    else
                    {
                        if (kvp.Value.Length == 1)
                        {
                            XElement subElement2 = kvp.Value.First();
                            List<object> subDynamic = new List<object>();
                            foreach (XElement subsubElement in subElement2.Elements())
                            {
                                var sd = ToDynamic(subsubElement, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative);
                                if (sd != null || (sd == null && emptyXmlNodeValueHandling != ChoEmptyXmlNodeValueHandling.Ignore))
                                    subDynamic.Add(sd);
                            }
                            ((IDictionary<string, object>)obj).Add(subElement2.Name.LocalName, subDynamic.ToArray());
                        }
                        else
                        {
                            List<object> list = new List<object>();
                            string keyName = null;
                            foreach (var subElement in kvp.Value)
                            {
                                if (subElement == null)
                                    continue;

                                object dobj = ToDynamic(subElement, xmlSchemaNS, jsonSchemaNS, emptyXmlNodeValueHandling, retainXmlAttributesAsNative);
                                if (dobj != null || (dobj == null && emptyXmlNodeValueHandling != ChoEmptyXmlNodeValueHandling.Ignore))
                                    list.Add(dobj);

                                keyName = subElement.Name.LocalName + "s";
                            }
                                return list.ToArray();
                            //else
                            //    ((IDictionary<string, object>)obj).Add(keyName, list.ToArray());
                        }
                    }
                }
            }
            else
            {
                if (element.IsNilElement())
                {
                    obj.SetText(element.NilAwareValue(xmlSchemaNS));
                }
                else
                {
                    string value = element.NilAwareValue(xmlSchemaNS);

                    if (value.IsNullOrEmpty() && !element.HasAttributes())
                    {
                        switch (emptyXmlNodeValueHandling)
                        {
                            case ChoEmptyXmlNodeValueHandling.Empty:
                                obj.SetText(value);
                                break;
                            case ChoEmptyXmlNodeValueHandling.Null:
                                obj.SetText(value.IsNullOrEmpty() ? null : value);
                                break;
                            default:
                                return null;
                        }
                    }
                    else if (value != null)
                        obj.SetText(value);
                }
            }

            return obj;
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