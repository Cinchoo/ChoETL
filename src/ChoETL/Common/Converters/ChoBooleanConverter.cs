using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [ChoTypeConverter(typeof(Boolean))]
#if !NETSTANDARD2_0
    public class ChoBooleanConverter : IValueConverter
#else
    public class ChoBooleanConverter : IChoValueConverter
#endif
    {
        public ChoBooleanConverter()
        {

        }
        private ChoBooleanFormatSpec GetTypeFormat(object parameter, out string customBoolFormatText)
        {
            customBoolFormatText = null;
            var typeFormat = parameter.GetValueAt<object>(0);

            if (typeFormat is ChoTypeConverterFormatSpec ts)
            {
                customBoolFormatText = ts.CustomBooleanFormatText;
                return ts.BooleanFormat;
            }
            else if (typeFormat is string tf)
                return ChoBooleanFormatSpec.Custom;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.BooleanFormat);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string)
            {
                string txt = value as string;
                txt = txt.NTrim();

                if (txt.IsNull())
                    throw new ChoParserException($"Invalid data `{txt}` found.");

                string customBoolFormatText = null;
                ChoBooleanFormatSpec booleanFormat = GetTypeFormat(parameter, out customBoolFormatText); //.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.BooleanFormat);
                switch (booleanFormat)
                {
                    case ChoBooleanFormatSpec.YOrN:
                        if (txt.Length == 1)
                        {
                            if (txt[0] == 'Y' || txt[0] == 'y')
                                return true;
                            else if (txt[0] == 'N' || txt[0] == 'n')
                                return false;
                        }

                        throw new ChoParserException($"Invalid data `{txt}` found.");
                    case ChoBooleanFormatSpec.TOrF:
                        if (txt.Length == 1)
                        {
                            if (txt[0] == 'T' || txt[0] == 't')
                                return true;
                            else if (txt[0] == 'F' || txt[0] == 'f')
                                return false;
                        }

                        throw new ChoParserException($"Invalid data `{txt}` found.");
                    case ChoBooleanFormatSpec.TrueOrFalse:
                        if (String.Compare(txt, "true", true) == 0)
                            return true;
                        else if (String.Compare(txt, "false", true) == 0)
                            return false;

                        throw new ChoParserException($"Invalid data `{txt}` found.");
                    case ChoBooleanFormatSpec.YesOrNo:
                        if (String.Compare(txt, "yes", true) == 0)
                            return true;
                        else if (String.Compare(txt, "no", true) == 0)
                            return false;

                        throw new ChoParserException($"Invalid data `{txt}` found.");
                    case ChoBooleanFormatSpec.ZeroOrOne:
                        if (txt.Length == 1)
                        {
                            if (txt[0] == '1')
                                return true;
                            else if (txt[0] == '0')
                                return false;
                        }

                        throw new ChoParserException($"Invalid data `{txt}` found.");
                    default:
                        string boolTxt = parameter.GetValueAt<string>(0);
                        if (boolTxt.IsNullOrWhiteSpace())
                            boolTxt = customBoolFormatText;

                        string trueBoolTxt = boolTxt.SplitNTrim().FirstOrDefault();
                        string falseBoolTxt = boolTxt.SplitNTrim().Skip(1).FirstOrDefault();
                        if (trueBoolTxt.IsNullOrWhiteSpace()
                            || falseBoolTxt.IsNullOrWhiteSpace())
                        {
                            if (txt.Length == 1)
                            {
                                if (txt[0] == 'Y' || txt[0] == 'y'
                                    || txt[0] == 'T' || txt[0] == 't'
                                    || txt[0] == '1')
                                    return true;
                                else if (txt[0] == 'N' || txt[0] == 'n'
                                    || txt[0] == 'F' || txt[0] == 'f'
                                    || txt[0] == '0')
                                    return false;

                                throw new ChoParserException($"Invalid data `{txt}` found.");
                            }
                            else
                            {
                                if (String.Compare(txt, "true", true) == 0
                                    || String.Compare(txt, "yes", true) == 0)
                                    return true;
                                else if (String.Compare(txt, "false", true) == 0
                                    || String.Compare(txt, "no", true) == 0)
                                    return false;

                                throw new ChoParserException($"Invalid data `{txt}` found.");
                            }
                        }
                        else
                        {
                            if (String.Compare(txt, trueBoolTxt, true) == 0)
                                return true;
                            else if (String.Compare(txt, falseBoolTxt, true) == 0)
                                return false;

                            throw new ChoParserException($"Invalid data `{txt}` found.");
                        }
                }
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                if (value == null)
                    value = false;

                if (value is bool)
                {
                    bool boolValue = (bool)value;

                    string customBoolFormatText = null;
                    ChoBooleanFormatSpec booleanFormat = GetTypeFormat(parameter, out customBoolFormatText); //.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.BooleanFormat);
                    switch (booleanFormat)
                    {
                        case ChoBooleanFormatSpec.TOrF:
                            return boolValue ? "T" : "F";
                        case ChoBooleanFormatSpec.YOrN:
                            return boolValue ? "Y" : "N";
                        case ChoBooleanFormatSpec.TrueOrFalse:
                            return boolValue ? "True" : "False";
                        case ChoBooleanFormatSpec.YesOrNo:
                            return boolValue ? "Yes" : "No";
                        default:
                            if (customBoolFormatText == null)
                                return boolValue ? "1" : "0";
                            else
                            {
                                string boolTxt = parameter.GetValueAt<string>(0);
                                if (boolTxt.IsNullOrWhiteSpace())
                                    boolTxt = customBoolFormatText;

                                string trueBoolTxt = boolTxt.SplitNTrim().FirstOrDefault();
                                string falseBoolTxt = boolTxt.SplitNTrim().Skip(1).FirstOrDefault();

                                return boolValue ? trueBoolTxt : falseBoolTxt;
                            }
                    }
                }
            }
            else if (value == DBNull.Value)
                return null;

            return value;
        }
    }
}
