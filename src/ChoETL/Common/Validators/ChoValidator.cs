using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoValidator
    {
        public static void ValididateFor(this object @this, string mn)
        {
            Exception aggEx = null;
            IsValidFor(@this, mn, out aggEx);
            if (aggEx != null)
                throw aggEx;
        }
        
        public static void ValididateFor(this object @this, MemberInfo mi)
        {
            Exception aggEx = null;
            IsValidFor(@this, mi, out aggEx);
            if (aggEx != null)
                throw aggEx;
        }

        public static bool IsValidFor(this object @this, string mn, out Exception aggEx)
        {
            ChoGuard.ArgumentNotNullOrEmpty(@this, "Target");
            ChoGuard.ArgumentNotNullOrEmpty(mn, "MemberName");

            aggEx = null;
            MemberInfo mi = ChoType.GetMemberInfo(@this.GetType(), mn);
            if (mi != null)
                return IsValidFor(@this, mi, out aggEx);
            else
                return true;
        }

        public static bool IsValidFor(this object @this, MemberInfo mi, out Exception aggEx)
        {
            aggEx = null;
            ChoGuard.ArgumentNotNullOrEmpty(@this, "Target");

            if (@this == null)
                return true;

            var results = new List<ValidationResult>();

            if (@this is IChoValidatable)
            {
                ((IChoValidatable)@this).TryValidateFor(mi.Name, results);
            }
            else
            {
                //if (ChoObjectMemberMetaDataCache.Default.IsRequired(mi) && ChoType.GetMemberValue(@this, mi) == null)
                //    results.Add(new ValidationResult("Null value found for {0} member.".FormatString(mi.Name)));

                var context = new ValidationContext(@this, null, null);
                context.MemberName = mi.Name;

                Validator.TryValidateValue(ChoType.GetMemberValue(@this, mi), context, results, ChoTypeDescriptor.GetPropetyAttributes<ValidationAttribute>(ChoTypeDescriptor.GetProperty<ValidationAttribute>(@this.GetType(), mi.Name)));
            }

            if (results.Count > 0)
            {
                aggEx = new ApplicationException("Failed to validate '{0}' member. {2}{1}".FormatString(mi.Name, ToString(results), Environment.NewLine));
                return false;
            }
            else
                return true;
        }

        public static void Validate(this object @this)
        {
            if (@this == null) return;

            Exception aggEx = null;
            IsValid(@this, out aggEx);
            if (aggEx != null)
                throw aggEx;
        }

        public static bool IsValid(this object @this, out Exception aggEx)
        {
            aggEx = null;

            if (@this == null)
                return true;

            var results = new List<ValidationResult>();
            if (@this is IChoValidatable)
            {
                ((IChoValidatable)@this).TryValidate(results);
            }
            else
            {
                foreach (MemberInfo mi in ChoType.GetMembers(@this.GetType()).Where(m => ChoType.GetMemberAttributeByBaseType<ChoMemberAttribute>(m) != null))
                {
                    //if (ChoObjectMemberMetaDataCache.Default.IsRequired(mi) && ChoType.GetMemberValue(@this, mi) == null)
                    //    results.Add(new ValidationResult("Null value found for {0} member.".FormatString(mi.Name)));
                }

                var context = new ValidationContext(@this, null, null);
                Validator.TryValidateObject(@this, context, results, true);
            }

            if (results.Count > 0)
            {
                aggEx = new ApplicationException("Failed to validate '{0}' object. {2}{1}".FormatString(@this.GetType().FullName, ToString(results), Environment.NewLine));
                return false;
            }
            else
                return true;
        }

        private static string ToString(IEnumerable<ValidationResult> results)
        {
            StringBuilder msg = new StringBuilder();
            foreach (var validationResult in results)
            {
                msg.AppendLine(validationResult.ErrorMessage);

                if (validationResult is CompositeValidationResult)
                    msg.AppendLine(ToString(((CompositeValidationResult)validationResult).Results).Indent());
            }

            return msg.ToString();
        }
    }
}
