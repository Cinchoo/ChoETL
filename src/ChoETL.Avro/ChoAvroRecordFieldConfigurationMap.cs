using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoAvroRecordFieldConfigurationMap
    {
        private readonly ChoAvroRecordFieldConfiguration _config;

        public ChoAvroRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoAvroRecordFieldConfigurationMap(ChoAvroRecordFieldConfiguration config)
        {
            ChoGuard.ArgumentNotNull(config, nameof(config));
            _config = config;
        }

        public ChoAvroRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap ValueConverterBack(Func<object, object> value)
        {
            _config.ValueConverterBack = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap PropertyConverter(IChoValueConverter converter)
        {
            if (_config.PropConvertersInternal.IsNullOrEmpty())
                _config.PropConvertersInternal = new object[] { converter };
            else
            {
                _config.PropConvertersInternal = ChoArray.Combine<object>(_config.PropConvertersInternal, new object[] { converter });
            }
            return this;
        }

#if !NETSTANDARD2_0
        public ChoAvroRecordFieldConfigurationMap PropertyConverter(System.Windows.Data.IValueConverter converter)
        {
            if (_config.PropConverters.IsNullOrEmpty())
                _config.PropConverters = new object[] { converter };
            else
            {
                _config.PropConverters = ChoArray.Combine<object>(_config.PropConverters, new object[] { converter });
            }
            return this;
        }
#endif

        public ChoAvroRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoAvroRecordFieldConfigurationMap Configure(Action<ChoAvroRecordFieldConfiguration> action)
        {
            action?.Invoke(_config);

            return this;
        }
    }
}
