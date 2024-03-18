using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ChoETL
{
    [DataContract]
    public class ChoXmlRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        public string ArrayNodeNamespace
        {
            get;
            set;
        }
        public string ArrayNodeName
        {
            get;
            set;
        }
        public string NodeNamespace
        {
            get;
            set;
        }

        public bool? UseProxy
        {
            get;
            set;
        }

        internal bool IsXPathSet
        {
            get;
            set;
        }

        private string _XPath;
        public string XPath
        {
            get { return _XPath; }
            set
            {
                IsXPathSet = !value.IsNullOrWhiteSpace();
                _XPath = value;
            }
        }

        private Func<object, List<object>> _customNodeSelecter = null;
        public Func<object, List<object>> CustomNodeSelector
        {
            get { return _customNodeSelecter; }
            set { if (value == null) return; _customNodeSelecter = value; }
        }

        private string _defaultXPath;
        internal string GetXPath(string nsPrefix)
        {
            if (XPath.IsNullOrWhiteSpace())
            {
                if (_defaultXPath.IsNullOrWhiteSpace())
                {
                    if (nsPrefix.IsNullOrWhiteSpace())
                    {
                        if (!FieldName.IsNullOrWhiteSpace())
                            _defaultXPath = $"{FieldName}|@{FieldName}";
                        else
                            _defaultXPath = $"{Name}|@{Name}";

                    }
                    else
                    {
                        if (!FieldName.IsNullOrWhiteSpace())
                        {
                            if (FieldName.Contains(":"))
                                _defaultXPath = $"{FieldName}|@{FieldName}";
                            else
                                _defaultXPath = $"{FieldName}|@{FieldName}|{nsPrefix}:{FieldName}";
                        }
                        else
                        {
                            if (Name.Contains(":"))
                                _defaultXPath = $"{Name}|@{Name}";
                            else
                                _defaultXPath = $"{Name}|@{Name}|{nsPrefix}:{Name}";
                        }
                    }
                }
                return _defaultXPath;
            }
            else
                return XPath;
        }

        [DataMember]
        public bool IsAnyXmlNode
        {
            get;
            set;
        }
        [DataMember]
        public bool IsXmlAttribute
        {
            get;
            set;
        }
        [DataMember]
        public bool IsXmlCDATA
        {
            get;
            set;
        }
        [DataMember]
        public bool? EncodeValue
        {
            get;
            set;
        }
        [DataMember]
        public bool UseXmlSerialization
        {
            get;
            set;
        }
        public bool? TurnOffPluralization
        {
            get;
            set;
        }
        public bool? IsArray
        {
            get;
            set;
        }
        //internal bool UseCache
        //{
        //    get;
        //    set;
        //}
        private Func<XElement, Type> _fieldTypeSelector = null;
        public Func<XElement, Type> FieldTypeSelector
        {
            get { return _fieldTypeSelector; }
            set { if (value == null) return; _fieldTypeSelector = value; }
        }

        public ChoXmlRecordFieldConfiguration(string name, string xPath = null) : this(name, (ChoXmlNodeRecordFieldAttribute)null)
        {
            XPath = xPath;
        }

        internal ChoXmlRecordFieldConfiguration(string name, ChoXmlNodeRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            EncodeValue = true;
            FieldName = name;
            if (attr != null)
            {
                XPath = attr.XPath;
                EncodeValue = attr.EncodeValue;
                UseXmlSerialization = attr.UseXmlSerialization;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name.NTrim() : attr.FieldName.NTrim();
                IsXmlAttribute = attr is ChoXmlAttributeRecordFieldAttribute;
            }
            if (otherAttrs != null)
            {
                ChoXPathAttribute xpAttr = otherAttrs.OfType<ChoXPathAttribute>().FirstOrDefault();
                if (xpAttr != null && !xpAttr.XPath.IsNullOrWhiteSpace())
                    XPath = xpAttr.XPath;
            }
        }

        bool init = false;
        XPathExpression query = null;
        internal XPathExpression GetXPathExpr(XPathNavigator navigator)
        {
            if (init)
                return query;

            init = true;
            if (!XPath.IsNullOrWhiteSpace())
            {
                query = navigator.Compile(XPath);
            }
            return query;
        }

        internal void Validate(ChoXmlRecordConfiguration config)
        {
            try
            {
                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;

                //if (XPath.IsNullOrWhiteSpace())
                //    throw new ChoRecordConfigurationException("Missing XPath.");
                if (FillChar != null)
                {
                    if (FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));
                }

                if (Size != null && Size.Value <= 0)
                    throw new ChoRecordConfigurationException("Size must be > 0.");
                if (ErrorMode == null)
                    ErrorMode = config.ErrorMode; // config.ErrorMode;
                if (IgnoreFieldValueMode == null)
                    IgnoreFieldValueMode = config.IgnoreFieldValueMode;
                //if (QuoteField == null)
                //    QuoteField = config.QuoteAllFields;
            }
            catch (Exception ex)
            {
                throw new ChoRecordConfigurationException("Invalid configuration found at '{0}' field.".FormatString(Name), ex);
            }
        }

        internal bool IgnoreFieldValue(object fieldValue)
        {
            if (IgnoreFieldValueMode == null)
                return false; // fieldValue == null;

            if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null && fieldValue == null)
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.DBNull) == ChoIgnoreFieldValueMode.DBNull && fieldValue == DBNull.Value)
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.Empty) == ChoIgnoreFieldValueMode.Empty && fieldValue is string && ((string)fieldValue).IsEmpty())
                return true;
            else if ((IgnoreFieldValueMode & ChoIgnoreFieldValueMode.WhiteSpace) == ChoIgnoreFieldValueMode.WhiteSpace && fieldValue is string && ((string)fieldValue).IsNullOrWhiteSpace())
                return true;

            return false;
        }

    }
}
