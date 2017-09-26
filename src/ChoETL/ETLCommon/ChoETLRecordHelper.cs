using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoETLRecordHelper
    {
        public static void ConvertNSetMemberValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, ref object fieldValue, CultureInfo culture)
        {
            if (fieldConfig.ValueConverter != null)
                fieldValue = fieldConfig.ValueConverter(fieldValue);
            else
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
                else
                    fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);
            }

            dict.AddOrUpdate(fn, fieldValue);
        }

        public static void ConvertNSetMemberValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, ref object fieldValue, CultureInfo culture)
        {
            if (fieldConfig.ValueConverter != null)
                fieldValue = fieldConfig.ValueConverter(fieldValue);
            else
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                {
                    fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.PI.PropertyType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
                }
                else
                {
                    fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.PI.PropertyType, null, fieldConfig.Converters.ToArray(), null, culture);
                }
            }
            ChoType.SetPropertyValue(rec, fieldConfig.PI, fieldValue);
        }

        public static bool SetFallbackValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            if (!fieldConfig.IsFallbackValueSpecified)
                return false;

            if (rec is IDictionary<string, object>)
            {
                return ((IDictionary<string, object>)rec).SetFallbackValue(fn, fieldConfig, culture);
            }

            //Set fallback value to member
            object fieldValue = null;
            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.PI.PropertyType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
            else
                fieldValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.PI.PropertyType, null, fieldConfig.Converters.ToArray(), null, culture);

            ChoType.SetPropertyValue(rec, fieldConfig.PI, fieldValue);
            return true;
        }

        public static bool SetDefaultValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            if (!fieldConfig.IsDefaultValueSpecified)
                return false;

            if (rec is IDictionary<string, object>)
            {
                return ((IDictionary<string, object>)rec).SetDefaultValue(fn, fieldConfig, culture);
            }

            //Set default value to member
            object fieldValue = null;
            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.PI.PropertyType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
            else
                fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.PI.PropertyType, null, fieldConfig.Converters.ToArray(), null, culture);

            ChoType.SetPropertyValue(rec, fieldConfig.PI, fieldValue);
            return true;
        }

        public static bool SetFallbackValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fallbackValue)
        {
            //Set Fallback value to member
            if (fieldConfig.IsFallbackValueSpecified)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fallbackValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
                else
                    fallbackValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

                dict.AddOrUpdate(fn, fallbackValue);
            }

            return fieldConfig.IsFallbackValueSpecified;
        }

        public static bool SetDefaultValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            object fieldValue = null;
            //Set default value to member
            if (fieldConfig.IsDefaultValueSpecified)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
                else
                    fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

                dict.AddOrUpdate(fn, fieldValue);
            }
            else
            {
                dict.AddOrUpdate(fn, fieldConfig.FieldType.Default());
            }

            return fieldConfig.IsDefaultValueSpecified;
        }

        public static void DoObjectLevelValidation(this object recObject, ChoRecordConfiguration configuration, IEnumerable<ChoRecordFieldConfiguration> fieldConfigurations)
        {
            if ((configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
            {
                if (configuration.HasConfigValidators)
                {
                    IDictionary<string, Object> dict = null;
                    if (recObject is IDictionary<string, object>)
                        dict = recObject as IDictionary<string, Object>;
                    else
                    {
                        dict = new Dictionary<string, object>();

                        foreach (var pd in configuration.PIDict.Values)
                        {
                            dict.Add(pd.Name, ChoType.GetPropertyValue(recObject, pd));
                        }
                    }

                    ChoValidator.Validate(dict, configuration.ValDict);
                }
                else
                {
                    if (!configuration.IsDynamicObject)
                        ChoValidator.Validate(recObject);
                }
            }
        }

        public static void DoMemberLevelValidation(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, ChoObjectValidationMode vm)
        {
            if (!fieldConfig.Validators.IsNullOrEmpty() && (vm & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                ChoValidator.ValidateFor(dict[fn], fn, fieldConfig.Validators);
        }

        public static void DoMemberLevelValidation(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, ChoObjectValidationMode vm)
        {
            if (!((vm & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel))
                return;

            if (rec is IDictionary<string, object>)
            {
                ((IDictionary<string, object>)rec).DoMemberLevelValidation(fn, fieldConfig, vm);
            }
            else
            {
                if (fieldConfig.Validators.IsNullOrEmpty())
                    ChoValidator.ValidateFor(rec, fieldConfig.PI);
                else
                    ChoValidator.ValidateFor(ChoType.GetPropertyValue(rec, fieldConfig.PI), fn, fieldConfig.Validators);
            }
        }

        public static void DoMemberLevelValidation(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, ChoObjectValidationMode vm, object fieldValue)
        {
            if ((vm & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
            {
                if (fieldConfig.Validators.IsNullOrEmpty())
                {
                    ChoValidator.ValidateFor(fieldValue, fn, fieldConfig.Validators);
                }
            }
        }

        //*****
        public static bool GetDefaultValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fieldValue)
        {
            if (!fieldConfig.IsDefaultValueSpecified)
                return false;

            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertTo(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
            else
                fieldValue = ChoConvert.ConvertTo(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

            return true;
        }

        public static bool GetFallbackValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fieldValue)
        {
            if (!fieldConfig.IsFallbackValueSpecified)
                return false;

            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertTo(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
            else
                fieldValue = ChoConvert.ConvertTo(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

            return true;
        }

        public static void GetNConvertMemberValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fieldValue, bool nativeType = false)
        {
            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertTo(fieldValue, nativeType ? fieldConfig.FieldType : typeof(string), null, fieldConfig.PropConverters, fieldConfig.PropConverterParams, culture);
            else
                fieldValue = ChoConvert.ConvertTo(fieldValue, nativeType ? fieldConfig.FieldType : typeof(string), null, fieldConfig.Converters.ToArray(), null, culture);
        }

        //public static void DoObjectLevelValidatation(this object record, ChoRecordConfiguration rc,  ChoRecordFieldConfiguration[] fieldConfigs)
        //{
        //    if (rc.HasConfigValidators)
        //    {
        //        Dictionary<string, ValidationAttribute[]> valDict = rc.ValDict;
        //        IDictionary<string, Object> dict = null;
        //        if (record is ExpandoObject)
        //            dict = record as IDictionary<string, Object>;
        //        else
        //            dict = record.ToDictionary();

        //        ChoValidator.Validate(dict, valDict);
        //    }
        //    else
        //    {
        //        if (!(record is ExpandoObject))
        //            ChoValidator.Validate(record);
        //    }
        //}

    }
}
