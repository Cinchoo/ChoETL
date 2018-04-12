using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    [ChoTypeConverter(typeof(Enum))]
    public class ChoEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string && targetType.IsEnum)
            {
                string txt = value as string;
                txt = txt.NTrim();

                if (txt.IsNull())
                    return Activator.CreateInstance(targetType);

                ChoEnumFormatSpec EnumFormat = parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.EnumFormat);
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((value != null && value.GetType().IsEnum) && targetType == typeof(string))
            {
                ChoEnumFormatSpec EnumFormat = parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.EnumFormat);
                switch (EnumFormat)
                {
                    case ChoEnumFormatSpec.Name:
                        return value.ToString();
                    case ChoEnumFormatSpec.Description:
                        return ChoEnum.ToDescription((Enum)value);
                    default:
                        string ft = parameter.GetValueAt<string>(0);
                        if (ft.IsNullOrWhiteSpace())
                        {
                            return ((Enum)value).ToString("D");
                        }
                        else
                        {
                            return ((Enum)value).ToString(ft);
                        }
                }
            }

            return value;
        }
    }
}
