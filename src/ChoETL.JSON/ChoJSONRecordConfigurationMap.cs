using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoJSONRecordFieldConfigurationMap
    {
        private readonly ChoJSONRecordFieldConfiguration _config;

        public ChoJSONRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoJSONRecordFieldConfigurationMap(ChoJSONRecordFieldConfiguration config)
        {
            _config = config;
        }

        public ChoJSONRecordFieldConfigurationMap JSONPath(string value)
        {
            _config.JSONPath = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap IsArray(bool value = true)
        {
            _config.IsArray = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap UseJSONSerialization(bool value = true)
        {
            _config.UseJSONSerialization = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        //public ChoJSONRecordFieldConfigurationMap Quote(bool? value)
        //{
        //    _config.QuoteField = value;
        //    return this;
        //}

        public ChoJSONRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap ValueConverterBack(Func<object, object> value)
        {
            _config.ValueConverterBack = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap PropertyConverter(IChoValueConverter converter)
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
        public ChoJSONRecordFieldConfigurationMap PropertyConverter(System.Windows.Data.IValueConverter converter)
        {
            if (_config.PropConvertersInternal.IsNullOrEmpty())
                _config.PropConvertersInternal = new object[] { converter };
            else
            {
                _config.PropConvertersInternal = ChoArray.Combine<object>(_config.PropConvertersInternal, new object[] { converter });
            }
            return this;
        }
#endif
        public ChoJSONRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoJSONRecordFieldConfigurationMap Configure(Action<ChoJSONRecordFieldConfiguration> action)
        {
            if (action != null)
                action(_config);

            return this;
        }
    }
}
