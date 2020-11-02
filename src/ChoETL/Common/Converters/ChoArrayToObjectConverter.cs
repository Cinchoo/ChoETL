using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    //[ChoTypeConverter(typeof(Array))]
#if !NETSTANDARD2_0
    public class ChoArrayToObjectConverter : IValueConverter
#else
    public class ChoArrayToObjectConverter : IChoValueConverter
#endif
    {
        public static readonly ChoArrayToObjectConverter Instance = new ChoArrayToObjectConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<object> result = new List<object>();
            Type itemType = targetType.GetItemType();
            if (value != null && value.GetType().IsCollectionType())
            {
                foreach (var iv in (IList)value)
                    result.Add(Deserialize(iv, itemType, parameter, culture));
            }

            return result.ToArray();
        }

        protected virtual object Deserialize(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value.GetType().IsCollectionType())
            {
                if (targetType != typeof(object) && !targetType.IsSimple() && !typeof(ICollection).IsAssignableFrom(targetType))
                {
                    IList coll = value as IList;
                    var itemType = value.GetType().GetItemType();
                    if (itemType.IsSimple())
                    {
                        value = ChoActivator.CreateInstance(targetType);
                        foreach (var p in ChoTypeDescriptor.GetProperties<ChoArrayIndexAttribute>(targetType).Select(pd => new { pd, a = ChoTypeDescriptor.GetPropetyAttribute<ChoArrayIndexAttribute>(pd) })
                            .GroupBy(g => g.a.Position).Select(g => g.First()).Where(g => g.a.Position >= 0).OrderBy(g => g.a.Position))
                        {
                            if (p.a.Position < coll.Count)
                            {
                                ChoType.ConvertNSetPropertyValue(value, p.pd.Name, coll[p.a.Position]);
                            }
                        }
                    }
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return value;

            List<object> result = new List<object>();
            Type itemType = targetType.GetItemType();

            if (!value.GetType().IsCollectionType())
            {
                result.Add(Serialize(value, itemType, parameter, culture));
            }
            else
            {
                foreach (var iv in (IList)value)
                    result.Add(Serialize(iv, itemType, parameter, culture));
            }

            return result.ToArray();
        }

        protected virtual object Serialize(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IList result = ChoActivator.CreateInstance(typeof(IList<>).MakeGenericType(targetType)) as IList;

            if (value != null && !value.GetType().IsCollectionType())
            {
                if (targetType == typeof(object) || targetType.IsSimple())
                {
                    foreach (var p in ChoTypeDescriptor.GetProperties(value.GetType()).Where(pd => ChoTypeDescriptor.GetPropetyAttribute<ChoIgnoreMemberAttribute>(pd) == null))
                    {
                        result.Add(ChoConvert.ConvertTo(ChoType.GetPropertyValue(value, p.Name), targetType, culture));
                    }
                }
            }

            return result.OfType<object>().ToArray();
        }
    }
}
