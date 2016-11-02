using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoSurrogateObjectCache
    {
        public static readonly ChoSurrogateObjectCache Default = new ChoSurrogateObjectCache();

        private readonly object _padLock = new object();
        private readonly Dictionary<Type, object> _objectCache = new Dictionary<Type, object>();

        public object GetSurrogateObject(object @this)
        {
            if (@this == null)
                return @this;

            Type type = @this.GetType();

            MetadataTypeAttribute attr = type.GetCustomAttribute<MetadataTypeAttribute>();
            if (attr == null || attr.MetadataClassType == null)
                return @this;
            else
            {
                if (_objectCache.ContainsKey(type))
                    return _objectCache[type] != null ? _objectCache[type] : @this;

                lock (_padLock)
                {
                    if (!_objectCache.ContainsKey(type))
                    {
                        object obj = null;

                        try
                        {
                            obj = ChoActivator.CreateInstance(attr.MetadataClassType);
                        }
                        catch { }

                        _objectCache.Add(type, obj);
                    }

                    return _objectCache[type] != null ? _objectCache[type] : @this;
                }
            }
        }
    }
}
