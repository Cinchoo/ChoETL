using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

                return ChoType.GetAllTypes().Where(t => typeof(JsonConverter).IsAssignableFrom(t) && !t.IsGenericType && ChoType.HasDefaultConstructor(t))
                    .GroupBy(t => t)
                    .ToDictionary(kvp => kvp.Key.Name, kvp => Activator.CreateInstance(kvp.First()) as JsonConverter);
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
                if (_convertersCache.Value.ContainsKey(name))
                    _convertersCache.Value.Add(name, converter);
                else
                    _convertersCache.Value[name] = converter;
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

        public static KeyValuePair<string, JsonConverter>[] GetAll()
        {
            lock (_padLock)
            {
                return new List<KeyValuePair<string, JsonConverter>>(_convertersCache.Value).ToArray();
            }
        }
    }
}
