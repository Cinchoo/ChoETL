using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoBSONRecordFieldConfigurationMap
    {
        private readonly ChoBSONRecordFieldConfiguration _config;

        public ChoBSONRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoBSONRecordFieldConfigurationMap(ChoBSONRecordFieldConfiguration config)
        {
            ChoGuard.ArgumentNotNull(config, nameof(config));
            _config = config;
        }

        public ChoBSONRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoBSONRecordFieldConfigurationMap Configure(Action<ChoBSONRecordFieldConfiguration> action)
        {
            action?.Invoke(_config);

            return this;
        }
    }
}
