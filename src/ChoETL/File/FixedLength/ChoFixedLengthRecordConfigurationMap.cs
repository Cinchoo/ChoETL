using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoFixedLengthRecordFieldConfigurationMap
    {
        private readonly ChoFixedLengthRecordFieldConfiguration _config;

        public ChoFixedLengthRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoFixedLengthRecordFieldConfigurationMap(ChoFixedLengthRecordFieldConfiguration config)
        {
            _config = config;
        }

        public ChoFixedLengthRecordFieldConfigurationMap StartIndex(int value)
        {
            _config.StartIndex = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap AltNames(params string[] fns)
        {
            _config.AltFieldNames = String.Join(",", fns);
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Quote(bool? value)
        {
            _config.QuoteField = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoFixedLengthRecordFieldConfigurationMap Configure(Action<ChoFixedLengthRecordFieldConfiguration> action)
        {
            if (action != null)
                action(_config);

            return this;
        }
    }
}
