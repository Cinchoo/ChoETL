using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDictionaryEx
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");

            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        public static bool ContainsKey<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase, CultureInfo culture)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            return dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).Any();
        }

        public static void AddOrUpdateValue<TValue>(this IDictionary<string, TValue> dict, string key, TValue value, bool ignoreCase, CultureInfo culture)
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (cultureSpecificKeyName.IsNullOrWhiteSpace())
                dict.Add(cultureSpecificKeyName, value);
            else
                dict[cultureSpecificKeyName] = value;
        }

        public static TValue GetValue<TValue>(this IDictionary<string, TValue> dict, string key, bool ignoreCase, CultureInfo culture, TValue defaultValue = default(TValue))
        {
            ChoGuard.ArgumentNotNull(dict, "Dictionary");
            ChoGuard.ArgumentNotNull(culture, "Culture");

            string cultureSpecificKeyName = dict.Keys.Where(i => String.Compare(i, key, ignoreCase, culture) == 0).FirstOrDefault();
            if (!cultureSpecificKeyName.IsNullOrWhiteSpace())
                return dict[cultureSpecificKeyName];
            else
                return defaultValue;
        }
    }
}
