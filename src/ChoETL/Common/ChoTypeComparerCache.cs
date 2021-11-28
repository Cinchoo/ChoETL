using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoTypeComparerCache
    {
        public static readonly ChoTypeComparerCache Instance = new ChoTypeComparerCache();

        public readonly ConcurrentDictionary<Type, IComparer> Cache = new ConcurrentDictionary<Type, IComparer>();

        public void ScanAndLoad()
        {
            var types = ChoType.GetAllTypes().Where(t => typeof(IComparer).IsAssignableFrom(t)).ToArray();
            foreach (Type compType in types)
            {
                try
                {
                    var comp = ChoActivator.CreateInstance(compType) as IComparer;
                    if (comp != null)
                    {
                        if (compType.IsGenericType)
                        {
                            Type type = comp.GetType().GetGenericArguments()[0];
                            Add(type, comp);
                        }
                        else
                        {
                            Type objType = compType.GetAllInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparer<>))
                                .Select(i => i.GetGenericArguments()[0]).FirstOrDefault();

                            if (objType != null)
                            {
                                Add(objType, comp);
                            }
                            else
                            {
                                var attr = compType.GetCustomAttribute<ChoComparerObjectTypeAttribute>();
                                if (attr != null && attr.Type != null)
                                {
                                    Add(attr.Type, comp);
                                }
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public IComparer GetComparer(Type type)
        {
            IComparer comparer = null;
            if (Cache.TryGetValue(type, out comparer))
                return comparer;
            return null;
        }

        public void Add<T>(IComparer<T> comparer)
        {
            if (comparer == null)
                return;

            Type type = comparer.GetType().GetGenericArguments()[0];
            Cache.AddOrUpdate(type, comparer as IComparer);
        }

        public void Add(Type type, IComparer comparer)
        {
            if (type == null || comparer == null)
                return;

            Cache.AddOrUpdate(type, comparer);
        }

        public bool Remove(Type type)
        {
            if (type == null)
                return false;

            IComparer comparer = null;
            return Cache.TryRemove(type, out comparer);
        }

        public bool Remove<T>(IComparer<T> comparer)
        {
            if (comparer == null)
                return false;

            Type type = comparer.GetType().GetGenericArguments()[0];
            return Remove(type);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ChoComparerObjectTypeAttribute : Attribute
    {
        public Type Type
        {
            get;
            private set;
        }

        public ChoComparerObjectTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
