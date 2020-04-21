using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoKVPRecordFieldConfigurationMap
    {
        private readonly ChoKVPRecordFieldConfiguration _config;

        public ChoKVPRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoKVPRecordFieldConfigurationMap(ChoKVPRecordFieldConfiguration config)
        {
            _config = config;
        }

        public ChoKVPRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Quote(bool? value)
        {
            _config.QuoteField = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoKVPRecordFieldConfigurationMap Configure(Action<ChoKVPRecordFieldConfiguration> action)
        {
            if (action != null)
                action(_config);

            return this;
        }
    }
}
