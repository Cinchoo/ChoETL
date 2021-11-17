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
        public static bool IsInitialized { get; private set; } = false;
        private static readonly object _padLock = new object();
        private static readonly Lazy<Dictionary<string, JsonConverter>> _convertersCache = new Lazy<Dictionary<string, JsonConverter>>(() =>
        {
            try
            {
                IsInitialized = true;

                Dictionary<string, JsonConverter> dict = new Dictionary<string, JsonConverter>();

                if (ChoETLFrxBootstrap.TurnOnAutoDiscoverJsonConverters)
                {
                    var convs = ChoType.GetAllTypes().Where(t => typeof(JsonConverter).IsAssignableFrom(t) && !t.IsGenericType && ChoType.HasDefaultConstructor(t))
                        .Distinct().ToArray();

                    foreach (var c in convs)
                    {
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

                return dict;
            }
            catch
            {
                return null;
            }
        }, false);

        public static void Init()
        {
            var x = _convertersCache.Value;
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
                if (!_convertersCache.Value.ContainsKey(name))
                    _convertersCache.Value.Add(name, converter);
                else
                    _convertersCache.Value[name] = converter;

                var dna = ChoTypeDescriptor.GetTypeAttribute<DisplayNameAttribute>(converter.GetType());
                if (dna != null && !dna.DisplayName.IsNullOrWhiteSpace())
                {
                    if (!_convertersCache.Value.ContainsKey(dna.DisplayName))
                        _convertersCache.Value.Add(dna.DisplayName, converter);
                    else
                        _convertersCache.Value[dna.DisplayName] = converter;
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
                if (_convertersCache.Value.ContainsKey(name))
                    _convertersCache.Value.Remove(name);
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
                return _convertersCache.Value.ContainsKey(name);
            }
        }

        public static JsonConverter Get(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, nameof(name));

            lock (_padLock)
            {
                if (_convertersCache.Value.ContainsKey(name))
                    return _convertersCache.Value[name];
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
                var convs = _convertersCache.Value.Values.ToArray();
                return convs.Where(c => c.CanConvert(type)).FirstOrDefault();
            }
        }

        public static KeyValuePair<string, JsonConverter>[] GetAll()
        {
            lock (_padLock)
            {
                return new List<KeyValuePair<string, JsonConverter>>(_convertersCache.Value).ToArray();
            }
        }
    }
}
