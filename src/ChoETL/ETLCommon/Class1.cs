using System;
using System.Collections.Generic;
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

        public static void SetDefaultValue(this object rec, string fn, ChoRecordFieldConfiguration fieldConfig, CultureInfo culture)
        {
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
                        fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, culture);
                    else
                        fieldValue = ChoConvert.ConvertFrom(fieldConfig.DefaultValue, fieldConfig.FieldType, null, fieldConfig.Converters.ToArray(), null, culture);

                    dict.AddOrUpdate(fn, fieldValue);
                }
            }
            catch { }
        }

        public static void ConvertNSetMemberValue(this IDictionary<string, object> dict, string fn, ChoRecordFieldConfiguration fieldConfig, ref object fieldValue, CultureInfo culture)
        {
            if (fieldConfig.Converters.IsNullOrEmpty())
                fieldValue = ChoConvert.ConvertFrom(fieldValue, fieldConfig.FieldType, culture);
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
    }
}
