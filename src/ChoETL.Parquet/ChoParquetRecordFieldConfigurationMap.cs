using System;
using System.ComponentModel.DataAnnotations;

namespace ChoETL
{
    public class ChoParquetRecordFieldConfigurationMap
    {
        private readonly ChoParquetRecordFieldConfiguration _config;

        public ChoParquetRecordFieldConfiguration Value
        {
            get { return _config; }
        }

        internal ChoParquetRecordFieldConfigurationMap(ChoParquetRecordFieldConfiguration config)
        {
            ChoGuard.ArgumentNotNull(config, nameof(config));
            _config = config;
        }

        public ChoParquetRecordFieldConfigurationMap Position(int pos)
        {
            _config.FieldPosition = pos;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap AltNames(params string[] fns)
        {
            _config.AltFieldNames = String.Join(",", fns);
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap FieldName(string name)
        {
            if (!name.IsNullOrWhiteSpace())
                _config.FieldName = name;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap FillChar(char? value)
        {
            _config.FillChar = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Justification(ChoFieldValueJustification? value)
        {
            _config.FieldValueJustification = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap TrimOption(ChoFieldValueTrimOption? value)
        {
            _config.FieldValueTrimOption = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Truncate(bool value)
        {
            _config.Truncate = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Size(int? value)
        {
            _config.Size = value;
            return this;
        }

        //public ChoParquetRecordFieldConfigurationMap Quote(bool? value)
        //{
        //    _config.QuoteField = value;
        //    return this;
        //}

        public ChoParquetRecordFieldConfigurationMap NullValue(string value)
        {
            _config.NullValue = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap ErrorMode(ChoErrorMode? value)
        {
            _config.ErrorMode = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap IgnoreFieldValueMode(ChoIgnoreFieldValueMode? value)
        {
            _config.IgnoreFieldValueMode = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap FieldType(Type value)
        {
            _config.FieldType = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Nullable(bool value)
        {
            _config.IsNullable = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap FormatText(string value)
        {
            _config.FormatText = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Validators(params ValidationAttribute[] values)
        {
            _config.Validators = values;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap ValueConverter(Func<object, object> value)
        {
            _config.ValueConverter = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap ValueConverterBack(Func<object, object> value)
        {
            _config.ValueConverterBack = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap PropertyConverter(IChoValueConverter converter)
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
        //public ChoParquetRecordFieldConfigurationMap PropertyConverter(System.Windows.Data.IValueConverter converter)
        //{
        //    if (_config.PropConverters.IsNullOrEmpty())
        //        _config.PropConverters = new object[] { converter };
        //    else
        //    {
        //        _config.PropConverters = ChoArray.Combine<object>(_config.PropConverters, new object[] { converter });
        //    }
        //    return this;
        //}
#endif

        public ChoParquetRecordFieldConfigurationMap ValueSelector(Func<dynamic, object> value)
        {
            _config.ValueSelector = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap CustomSerializer(Func<object, object> value)
        {
            _config.CustomSerializer = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap ItemConverter(Func<object, object> value)
        {
            _config.ItemConverter = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap DefaultValue(object value)
        {
            _config.DefaultValue = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap FallbackValue(object value)
        {
            _config.FallbackValue = value;
            return this;
        }

        public ChoParquetRecordFieldConfigurationMap Configure(Action<ChoParquetRecordFieldConfiguration> action)
        {
            action?.Invoke(_config);

            return this;
        }
    }
}
