using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONExtensions
    {
        public static string DumpAsJson(this DataTable table, Formatting formatting = Formatting.Indented)
        {
            if (table == null)
                return String.Empty;

            return JsonConvert.SerializeObject(table, formatting);
        }

        public static object GetNameAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Name).FirstOrDefault();
        }

        public static object GetValueAt(this JObject @this, int index)
        {
            if (@this == null || index < 0)
                return null;

            return @this.Properties().Skip(index).Select(p => p.Value).FirstOrDefault();
        }

        public static object ToJSONObject(this IDictionary<string, object> dict, Type type)
        {
            object target = ChoActivator.CreateInstance(type);
            string key = null;
            foreach (var p in ChoType.GetProperties(type))
            {
                if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                key = p.Name;
                var attr = p.GetCustomAttribute<JsonPropertyAttribute>();
                if (attr != null && !attr.PropertyName.IsNullOrWhiteSpace())
                    key = attr.PropertyName.NTrim();

                if (!dict.ContainsKey(key))
                    continue;

                p.SetValue(target, dict[key].CastObjectTo(p.PropertyType));
            }

            return target;
        }

        public static T ToJSONObject<T>(this IDictionary<string, object> dict)
            where T : class, new()
        {
            return (T)ToJSONObject(dict, typeof(T));
        }
    }
}
