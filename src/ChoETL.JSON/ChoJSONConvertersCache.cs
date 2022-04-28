using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONConvertersCache
    {
        //public static bool IsInitialized { get; private set; } = false;
        private static readonly object _padLock = new object();
        private static readonly Dictionary<string, JsonConverter> _convertersCache = new Dictionary<string, JsonConverter>();
        private static bool _isInit = false;

        public static void Init()
        {
            if (_isInit)
                return;

            lock (_padLock)
            {
                if (_isInit)
                    return;

                try
                {
                    //IsInitialized = true;

                    Dictionary<string, JsonConverter> dict = _convertersCache;

                    var convs = ChoType.GetAllTypes().Where(t => typeof(JsonConverter).IsAssignableFrom(t) && !t.IsGenericType && ChoType.HasDefaultConstructor(t))
                        .Distinct().ToArray();

                    foreach (var c in convs)
                    {
                        var dad = ChoTypeDescriptor.GetTypeAttribute<ChoDisableAutoDiscoverabilityAttribute>(c);
                        if (dad != null && dad.Flag)
                            continue;

                        if (dict.ContainsKey(c.Name))
                            continue;
                        try
                        {
                            dict.Add(c.Name, Activator.CreateInstance(c) as JsonConverter);

                            var dna = ChoTypeDescriptor.GetTypeAttribute<DisplayNameAttribute>(c);
                            if (dna != null && !dna.DisplayName.IsNullOrWhiteSpace())
                            {
                                if (!dict.ContainsKey(dna.DisplayName))
                                    dict.Add(dna.DisplayName, Activator.CreateInstance(c) as JsonConverter);
                            }
                        }
                        catch { }
                    }
                }
                catch
                {
                }

                _isInit = true;
            }
        }

        public static void Add(JsonConverter converter)
        {
            if (converter == null)
                return;

            Add(converter.GetType().Name, converter);
        }

        public static void Add(string name, JsonConverter converter)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, nameof(name));
            if (converter == null)
                return;

            lock (_padLock)
            {
                if (!_convertersCache.ContainsKey(name))
                    _convertersCache.Add(name, converter);
                else
                    _convertersCache[name] = converter;

                var dna = ChoTypeDescriptor.GetTypeAttribute<DisplayNameAttribute>(converter.GetType());
                if (dna != null && !dna.DisplayName.IsNullOrWhiteSpace())
                {
                    if (!_convertersCache.ContainsKey(dna.DisplayName))
                        _convertersCache.Add(dna.DisplayName, converter);
                    else
                        _convertersCache[dna.DisplayName] = converter;
                }

            }
        }

        public static void Remove(JsonConverter converter)
        {
            if (converter == null)
                return;

            Remove(converter.GetType().Name);
        }

        public static void Remove(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, nameof(name));

            lock (_padLock)
            {
                if (_convertersCache.ContainsKey(name))
                    _convertersCache.Remove(name);
            }
        }

        public static bool Contains(JsonConverter converter)
        {
            if (converter == null)
                return false;

            return Contains(converter.GetType().Name);
        }

        public static bool Contains(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, nameof(name));

            lock (_padLock)
            {
                return _convertersCache.ContainsKey(name);
            }
        }

        public static JsonConverter Get(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, nameof(name));

            lock (_padLock)
            {
                if (_convertersCache.ContainsKey(name))
                    return _convertersCache[name];
                else
                    return null;
            }
        }

        public static bool Contains(Type type)
        {
            ChoGuard.ArgumentNotNullOrEmpty(type, nameof(type));

            var contains = Contains(type.Name);
            if (contains)
                return true;

            lock (_padLock)
            {
                return Get(type) != null;
            }
        }

        public static JsonConverter Get(Type type)
        {
            ChoGuard.ArgumentNotNullOrEmpty(type, nameof(type));

            var conv = Get(type.Name);
            if (conv != null)
                return conv;

            lock (_padLock)
            {
                var convs = _convertersCache.Values.ToArray();
                return convs.Where(c => c.CanConvert(type)).FirstOrDefault();
            }
        }

        public static KeyValuePair<string, JsonConverter>[] GetAll()
        {
            lock (_padLock)
            {
                return new List<KeyValuePair<string, JsonConverter>>(_convertersCache).ToArray();
            }
        }
    }
}
