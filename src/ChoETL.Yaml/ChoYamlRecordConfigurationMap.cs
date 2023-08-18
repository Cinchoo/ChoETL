using SharpYaml.Serialization;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoYamlRecordFieldConfigurationMap
    {
        private readonly ChoYamlRecordFieldConfiguration _config;

        public ChoYamlRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoYamlRecordFieldConfigurationMap(ChoYamlRecordFieldConfiguration config)
        {
            _config = config;
        }

        public ChoYamlRecordFieldConfigurationMap YamlPath(string value)
        {
            _config.YamlPath = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap IsArray(bool value = true)
        {
            _config.IsArray = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap UseYamlSerialization(bool value = true)
        {
            _config.UseYamlSerialization = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        //public ChoYamlRecordFieldConfigurationMap Quote(bool? value)
        //{
        //    _config.QuoteField = value;
        //    return this;
        //}

        public ChoYamlRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap ValueConverterBack(Func<object, object> value)
        {
            _config.ValueConverterBack = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap PropertyConverter(IChoValueConverter converter)
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
        public ChoYamlRecordFieldConfigurationMap PropertyConverter(System.Windows.Data.IValueConverter converter)
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

        public ChoYamlRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoYamlRecordFieldConfigurationMap Configure(Action<ChoYamlRecordFieldConfiguration> action)
        {
            if (action != null)
                action(_config);

            return this;
        }
    }
}
