using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONExtensions
    {
        public static object ToObject(this IDictionary<string, object> dict, Type type)
        {
            object target = Activator.CreateInstance(type);
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

        public static T ToObject<T>(this IDictionary<string, object> dict)
        {
            return (T)ToObject(dict, typeof(T));
        }
    }
}
