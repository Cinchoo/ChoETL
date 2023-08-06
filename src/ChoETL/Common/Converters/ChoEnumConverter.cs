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
    [ChoTypeConverter(typeof(Enum))]
#if !NETSTANDARD2_0
    public class ChoEnumConverter : IValueConverter
#else
    public class ChoEnumConverter : IChoValueConverter
#endif
    {
        private ChoEnumFormatSpec GetTypeFormat(object parameter)
        {
            ChoTypeConverterFormatSpec ts = parameter.GetValueAt<ChoTypeConverterFormatSpec>(0);
            if (ts != null)
                return ts.EnumFormat;

            return parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.EnumFormat);
        }
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string && targetType.IsEnum)
            {
                string txt = value as string;
                txt = txt.NTrim();

                if (txt.IsNull())
                    return Activator.CreateInstance(targetType);

                ChoEnumFormatSpec EnumFormat = GetTypeFormat(parameter); //.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.EnumFormat);
                switch (EnumFormat)
                {
                    case ChoEnumFormatSpec.Name:
                        return Enum.Parse(targetType, txt);
                    case ChoEnumFormatSpec.Description:
                        return txt.ToEnum(targetType);
                    default:
                        return Enum.Parse(targetType, txt);
                }
            }
            else
                return value;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((value != null && value.GetType().IsEnum))
            {
                ChoEnumFormatSpec EnumFormat = GetTypeFormat(parameter); //.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.EnumFormat);
                switch (EnumFormat)
                {
                    case ChoEnumFormatSpec.Name:
                        return value.ToString();
                    case ChoEnumFormatSpec.Description:
                        return ChoEnum.ToDescription((Enum)value);
                    default:
                        string ft = parameter.GetValueFor<string>("Format", 1);
                        if (ft.IsNullOrWhiteSpace())
                        {
                            return (int)value; // ((Enum)value).ToString("D");
                        }
                        else
                        {
                            return ((Enum)value).ToString(ft);
                        }
                }
            }
            else if (value == DBNull.Value)
                return null;

            return value;
        }
    }

    [ChoTypeConverter(typeof(Enum))]
    public class ChoEnumNameConverter : ChoEnumConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return base.Convert(value, targetType, new string[] { "Name" }, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, new string[] { "Name" }, culture);
        }
    }

    [ChoTypeConverter(typeof(Enum))]
    public class ChoEnumDescriptionConverter : ChoEnumConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return base.Convert(value, targetType, new string[] { "Description" }, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return base.ConvertBack(value, targetType, new string[] { "Description" }, culture);
        }
    }
}
