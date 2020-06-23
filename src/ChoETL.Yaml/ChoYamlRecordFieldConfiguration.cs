using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoYamlRecordFieldConfiguration : ChoFileRecordFieldConfiguration
    {
        [DataMember]
        public string YamlPath
        {
            get;
            set;
        }

        public int Order
        {
            get;
            set;
        }

        public bool IsArray
        {
            get;
            set;
        }
        internal bool ComplexYamlPathUsed
        {
            get;
            set;
        }

        public bool? UseYamlSerialization
        {
            get;
            set;
        }

        internal string DeclaringMember
        {
            get;
            set;
        }
        internal PropertyDescriptor PropertyDescriptor
        {
            get;
            set;
        }
        private Func<IDictionary<string, object>, Type> _fieldTypeSelector = null;
        public Func<IDictionary<string, object>, Type> FieldTypeSelector
        {
            get { return _fieldTypeSelector; }
            set { if (value == null) return; _fieldTypeSelector = value; }
        }

        internal PropertyInfo PI;
        internal object[] PropConverters;
        internal object[] PropConverterParams;

        public ChoYamlRecordFieldConfiguration(string name, string yamlPath = null) : this(name, (ChoYamlRecordFieldAttribute)null)
        {
            YamlPath = yamlPath;
        }

        internal ChoYamlRecordFieldConfiguration(string name, ChoYamlRecordFieldAttribute attr = null, Attribute[] otherAttrs = null) : base(name, attr, otherAttrs)
        {
            IsArray = false;
            FieldName = name;
            if (attr != null)
            {
                Order = attr.Order;
                YamlPath = attr.YamlPath;
                UseYamlSerialization = attr.UseYamlSerializationInternal;
                FieldName = attr.FieldName.IsNullOrWhiteSpace() ? Name.NTrim() : attr.FieldName.NTrim();
            }
        }

        internal void Validate(ChoYamlRecordConfiguration config)
        {
            try
            {
                if (FieldName.IsNullOrWhiteSpace())
                    FieldName = Name;

                //if (YamlPath.IsNullOrWhiteSpace())
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
                if (QuoteField == null)
                    QuoteField = config.QuoteAllFields;
            }
            catch (Exception ex)
            {
                throw new ChoRecordConfigurationException("Invalid configuration found at '{0}' field.".FormatString(Name), ex);
            }
        }

        internal bool IgnoreFieldValue(object fieldValue)
        {
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
