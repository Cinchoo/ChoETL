using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoXmlRecordFieldConfigurationMap
    {
        private readonly ChoXmlRecordFieldConfiguration _config;

        public ChoXmlRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoXmlRecordFieldConfigurationMap(ChoXmlRecordFieldConfiguration config)
        {
            _config = config;
        }

        public ChoXmlRecordFieldConfigurationMap XPath(string value)
        {
            _config.XPath = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap IsAnyXmlNode(bool value = true)
        {
            _config.IsAnyXmlNode = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap IsXmlAttribute(bool value = true)
        {
            _config.IsXmlAttribute = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap IsXmlCDATA(bool value = true)
        {
            _config.IsXmlCDATA = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap EncodeValue(bool value = true)
        {
            _config.EncodeValue = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap UseXmlSerialization(bool value = true)
        {
            _config.UseXmlSerialization = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap IsArray(bool value = true)
        {
            _config.IsArray = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Quote(bool? value)
        {
            _config.QuoteField = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoXmlRecordFieldConfigurationMap Configure(Action<ChoXmlRecordFieldConfiguration> action)
        {
            if (action != null)
                action(_config);

            return this;
        }
    }
}
