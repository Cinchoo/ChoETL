using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoCSVRecordFieldConfigurationMap<T> : ChoCSVRecordFieldConfigurationMap
    {
        internal ChoCSVRecordFieldConfigurationMap(ChoCSVRecordFieldConfiguration config) : base(config)
        {
        }
    }

    public class ChoCSVRecordFieldConfigurationMap
    {
        protected readonly ChoCSVRecordFieldConfiguration _config;

        public ChoCSVRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoCSVRecordFieldConfigurationMap(ChoCSVRecordFieldConfiguration config)
        {
            ChoGuard.ArgumentNotNull(config, nameof(config));
            _config = config;
        }

        public ChoCSVRecordFieldConfigurationMap Position(int pos)
        {
            _config.FieldPosition = pos;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap AltNames(params string[] fns)
        {
            _config.AltFieldNames = String.Join(",", fns);
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Quote(bool? value)
        {
            _config.QuoteField = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoCSVRecordFieldConfigurationMap Configure(Action<ChoCSVRecordFieldConfiguration> action)
        {
            action?.Invoke(_config);

            return this;
        }
    }
}
