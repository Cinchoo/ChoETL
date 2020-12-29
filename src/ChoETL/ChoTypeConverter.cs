using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    public class ChoTypeConverter
    {
        public static readonly ChoTypeConverter Global = new ChoTypeConverter();

        private readonly object _padLock = new object();
        private readonly Dictionary<Type, object> _defaultTypeConverters = new Dictionary<Type, object>();

        static ChoTypeConverter()
        {
            ChoTypeConverter.Global.Add(typeof(Boolean), new ChoBooleanConverter());
        }

        public void Clear()
        {
            lock (_padLock)
            {
                _defaultTypeConverters.Clear();
            }
        }

        public void Remove(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            lock (_padLock)
            {
                if (!_defaultTypeConverters.ContainsKey(type))
                    return;

                _defaultTypeConverters.Remove(type);
            }
        }

        public void Add(Type type, TypeConverter converter)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(converter, "Converter");

            lock (_padLock)
            {
                Remove(type);
                _defaultTypeConverters.Add(type, converter);
            }
        }

#if !NETSTANDARD2_0
        public void Add(Type type, IValueConverter converter)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(converter, "Converter");

            lock (_padLock)
            {
                Remove(type);
                _defaultTypeConverters.Add(type, converter);
            }
        }
#endif

        public void Add(Type type, IChoValueConverter converter)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            ChoGuard.ArgumentNotNull(converter, "Converter");

            lock (_padLock)
            {
                Remove(type);
                _defaultTypeConverters.Add(type, converter);
            }
        }

        public KeyValuePair<Type, object>[] GetAll()
        {
            lock (_padLock)
            {
                return _defaultTypeConverters.ToArray();
            }
        }

        public bool Contains(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");
            lock (_padLock)
            {
                return _defaultTypeConverters.ContainsKey(type);
            }
        }

        public object GetConverter(Type type)
        {
            if (Contains(type))
                return _defaultTypeConverters[type];
            else
                return null;
        }
    }
}
