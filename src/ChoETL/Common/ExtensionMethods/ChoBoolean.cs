using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoBoolean
    {
        public static bool TryParse(string value, out bool result)
        {
            result = false;

            if (value.IsNullOrWhiteSpace())
                return false;

            if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.Any)
            {
                if (value.Length == 1)
                {
                    if (value[0] == 'Y' || value[0] == 'y') { result = true; return true; }
                    if (value[0] == 'N' || value[0] == 'n') { result = false; return true; }
                    if (value[0] == '1') { result = true; return true; }
                    if (value[0] == '0') { result = false; return true; }
                }
                else
                {
                    if (String.Compare(value, "true", true) == 0) { result = true; return true; }
                    if (String.Compare(value, "false", true) == 0) { result = false; return true; }
                    if (String.Compare(value, "yes", true) == 0) { result = true; return true; }
                    if (String.Compare(value, "no", true) == 0) { result = false; return true; }
                }
            }
            else if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.TOrF)
            {
                if (value.Length == 1)
                {
                    if (value[0] == 'T' || value[0] == 't') { result = true; return true; }
                    if (value[0] == 'F' || value[0] == 'f') { result = false; return true; }
                }
            }
            else if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.YOrN)
            {
                if (value.Length == 1)
                {
                    if (value[0] == 'Y' || value[0] == 'y') { result = true; return true; }
                    if (value[0] == 'N' || value[0] == 'n') { result = false; return true; }
                }
            }
            else if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.ZeroOrOne)
            {
                if (value.Length == 1)
                {
                    if (value[0] == '1') { result = true; return true; }
                    if (value[0] == '0') { result = false; return true; }
                }
            }
            else if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.TrueOrFalse)
            {
                if (String.Compare(value, "true", true) == 0) { result = true; return true; }
                if (String.Compare(value, "false", true) == 0) { result = false; return true; }
            }
            else if (ChoTypeConverterFormatSpec.Instance.BooleanFormat == ChoBooleanFormatSpec.YesOrNo)
            {
                if (String.Compare(value, "yes", true) == 0) { result = true; return true; }
                if (String.Compare(value, "no", true) == 0) { result = false; return true; }
            }

            return false;
        }
    }
}
