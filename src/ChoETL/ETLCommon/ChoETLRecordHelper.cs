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
    internal static class ChoETLRecordHelper
    {
        public static void DoObjectLevelValidation(this object recObject, ChoRecordConfiguration configuration, ChoRecordFieldConfiguration[] fieldConfigurations)
        {
            if ((configuration.ObjectValidationMode & ChoObjectValidationMode.ObjectLevel) == ChoObjectValidationMode.ObjectLevel)
            {
                bool hasConfigValidators = (from fc in fieldConfigurations
                                            where !fc.Validators.IsNullOrEmpty()
                                            select fc).Any();

                if (hasConfigValidators)
                {
                    Dictionary<string, ValidationAttribute[]> valDict = (from fc in fieldConfigurations
                                                                         select new KeyValuePair<string, ValidationAttribute[]>(fc.Name, fc.Validators)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    IDictionary<string, Object> dict = null;
                    if (recObject is ExpandoObject)
                        dict = recObject as IDictionary<string, Object>;
                    else
                        dict = recObject.ToDictionary();

                    ChoValidator.Validate(dict, valDict);
                }
                else
                {
                    if (!(recObject is ExpandoObject))
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
            if (rec is ExpandoObject)
            {
                ((IDictionary<string, object>)rec).DoMemberLevelValidation(fn, fieldConfig, vm);
            }
            else
            {
                if ((vm & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
                {
                    if (fieldConfig.Validators.IsNullOrEmpty())
                        ChoValidator.ValidateFor(rec, fn);
                    else
                        ChoValidator.ValidateFor(ChoType.GetMemberValue(rec, fn), fn, fieldConfig.Validators);
                }
            }
        }

        public static void DoMemberLevelValidation(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, ChoObjectValidationMode vm, object fieldValue)
        {
            if ((vm & ChoObjectValidationMode.MemberLevel) == ChoObjectValidationMode.MemberLevel)
            {
                if (fieldConfig.Validators.IsNullOrEmpty())
                {
                    if (!(rec is ExpandoObject))
                        ChoValidator.ValidateFor(fieldValue, fn, ChoTypeDescriptor.GetPropetyAttributes<ValidationAttribute>(ChoTypeDescriptor.GetProperty<ValidationAttribute>(rec.GetType(), fn)).ToArray());
                }
                else
                    ChoValidator.ValidateFor(fieldValue, fn, fieldConfig.Validators);
            }
        }

        public static void DoObjectLevelValidatation(this object record, ChoRecordFieldConfiguration[] fieldConfigs)
        {
            bool hasConfigValidators = (from fc in fieldConfigs
                                        where !fc.Validators.IsNullOrEmpty()
                                        select fc).Any();

            if (hasConfigValidators)
            {
                Dictionary<string, ValidationAttribute[]> valDict = (from fc in fieldConfigs
                                                                     select new KeyValuePair<string, ValidationAttribute[]>(fc.Name, fc.Validators)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                IDictionary<string, Object> dict = null;
                if (record is ExpandoObject)
                    dict = record as IDictionary<string, Object>;
                else
                    dict = record.ToDictionary();

                ChoValidator.Validate(dict, valDict);
            }
            else
            {
                if (!(record is ExpandoObject))
                    ChoValidator.Validate(record);
            }
        }

        public static void SetDefaultValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            if (rec is ExpandoObject)
            {
                ((IDictionary<string, object>)rec).SetDefaultValue(fn, fieldConfig, culture);
                return;
            }

            object fieldValue = null;

            //Set default value to member
            try
            {
                bool defaultValueExists = true;
                object defaultValue = null;
                if (fieldConfig.IsDefaultValueSpecified)
                    defaultValue = fieldConfig.DefaultValue;
                else if (ChoType.HasDefaultValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn)))
                    defaultValue = ChoType.GetRawDefaultValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn));
                else
                    defaultValueExists = false;

                if (defaultValueExists)
                {
                    if (fieldConfig.Converters.IsNullOrEmpty())
                        fieldValue = ChoConvert.ConvertFrom(defaultValue, ChoType.GetMemberInfo(rec.GetType(), fn), null, culture);
                    else
                        fieldValue = ChoConvert.ConvertFrom(defaultValue, ChoType.GetMemberType(rec.GetType(), fn), null, fieldConfig.Converters.ToArray(), null, culture);

                    ChoType.SetMemberValue(rec, fn, fieldValue);
                }
            }
            catch { }
        }

        public static void SetDefaultValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            object fieldValue = null;
            //Set default value to member
            try
            {
                if (fieldConfig.IsDefaultValueSpecified)
                {
                    if (fieldConfig.Converters.IsNullOrEmpty())
                        fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, null, ChoTypeDescriptor.GetTypeConvertersForType(fieldConfig.FieldType), null, culture);
                    else
                        fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

                    dict.AddOrUpdate(fn, fieldValue);
                }
            }
            catch { }
        }

        public static object GetDefaultValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig)
        {
            if (fieldConfig.IsDefaultValueSpecified)
            {
                return fieldConfig.DefaultValue;
            }
            else
            {
                if (rec is ExpandoObject)
                {
                }
                else if (ChoType.HasProperty(rec.GetType(), fn))
                {
                    return ChoType.GetRawDefaultValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn));
                }
            }

            return null;
        }

        public static bool SetFallbackValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
            if (rec is ExpandoObject)
            {
                return ((IDictionary<string, object>)rec).SetFallbackValue(fn, fieldConfig, culture);
            }

            object fieldValue = null;

            //Set Fallback value to member
            bool FallbackValueExists = true;
            object FallbackValue = null;
            if (fieldConfig.IsFallbackValueSpecified)
                FallbackValue = fieldConfig.FallbackValue;
            else if (ChoType.HasFallbackValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn)))
                FallbackValue = ChoType.GetRawFallbackValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn));
            else
                FallbackValueExists = false;

            if (FallbackValueExists)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertFrom(FallbackValue, ChoType.GetMemberInfo(rec.GetType(), fn), null, culture);
                else
                    fieldValue = ChoConvert.ConvertFrom(FallbackValue, ChoType.GetMemberType(rec.GetType(), fn), null, fieldConfig.Converters.ToArray(), null, culture);

                ChoType.SetMemberValue(rec, fn, fieldValue);
            }

            return FallbackValueExists;
        }

        public static bool SetFallbackValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fallbackValue)
        {
            //Set Fallback value to member
            if (fieldConfig.IsFallbackValueSpecified)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fallbackValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, ChoTypeDescriptor.GetTypeConvertersForType(fieldConfig.FieldType), null, culture);
                else
                    fallbackValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

                dict.AddOrUpdate(fn, fallbackValue);
            }

            return fieldConfig.IsFallbackValueSpecified;
        }

        public static bool GetFallbackValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fieldValue)
        {
            if (rec is ExpandoObject)
            {
                return ((IDictionary<string, object>)rec).GetFallbackValue(fn, fieldConfig, culture, ref fieldValue);
            }

            //Set Fallback value to member
            bool FallbackValueExists = true;
            object FallbackValue = null;
            if (fieldConfig.IsFallbackValueSpecified)
                FallbackValue = fieldConfig.FallbackValue;
            else if (ChoType.HasFallbackValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn)))
                FallbackValue = ChoType.GetRawFallbackValue(ChoTypeDescriptor.GetProperty(rec.GetType(), fn));
            else
                FallbackValueExists = false;

            if (FallbackValueExists)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertTo(FallbackValue, ChoType.GetMemberInfo(rec.GetType(), fn), null, culture);
                else
                    fieldValue = ChoConvert.ConvertTo(FallbackValue, ChoType.GetMemberType(rec.GetType(), fn), null, fieldConfig.Converters.ToArray(), null, culture);
            }

            return FallbackValueExists;
        }

        public static bool GetFallbackValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, ref object fieldValue)
        {
            //Set Fallback value to member
            if (fieldConfig.IsFallbackValueSpecified)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, ChoTypeDescriptor.GetTypeConvertersForType(fieldConfig.FieldType), null, culture);
                else
                    fieldValue = ChoConvert.ConvertFrom(fieldConfig.FallbackValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);
            }

            return fieldConfig.IsFallbackValueSpecified;
        }

        public static void ConvertNSetMemberValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, ref object fieldValue, CultureInfo culture)
        {
            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, ChoTypeDescriptor.GetTypeConvertersForType(fieldConfig.FieldType), null, culture);
            else
                fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

            dict.AddOrUpdate(fn, fieldValue);
        }

        public static void ConvertNSetMemberValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, ref object fieldValue, CultureInfo culture)
        {
            if (fieldConfig.Converters.IsNullOrEmpty())
            {
                ChoType.ConvertNSetMemberValue(rec, fn, fieldValue, culture);
                fieldValue = ChoType.GetMemberValue(rec, fn);
            }
            else
            {
                fieldValue = ChoConvert.ConvertFrom(fieldValue, ChoType.GetMemberType(rec.GetType(), fn), null, fieldConfig.Converters.ToArray(), null, culture);
                ChoType.SetMemberValue(rec, fn, fieldValue);
            }
        }

        public static object GetNConvertMemberValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture, object fieldValue)
        {
            if (rec is ExpandoObject)
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertTo(fieldValue, typeof(string), culture);
                else
                    fieldValue = ChoConvert.ConvertTo(fieldValue, typeof(string), null, fieldConfig.Converters.ToArray(), null, culture);
            }
            else
            {
                if (fieldConfig.Converters.IsNullOrEmpty())
                    fieldValue = ChoConvert.ConvertTo(fieldValue, ChoType.GetMemberInfo(rec.GetType(), fn), typeof(string), null, culture);
                else
                    fieldValue = ChoConvert.ConvertTo(fieldValue, typeof(string), null, fieldConfig.Converters.ToArray(), null, culture);
            }

            return fieldValue;
        }
    }
}
