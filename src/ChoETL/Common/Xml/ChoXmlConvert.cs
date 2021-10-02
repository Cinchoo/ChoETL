using System;
using System.Xml;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoXmlConvert
    {
        private static readonly ConcurrentDictionary<Type, Tuple<Func<object, object>, Func<object, object>>> _xmlConverters = new ConcurrentDictionary<Type, Tuple<Func<object, object>, Func<object, object>>>();

        static ChoXmlConvert()
        {
            _xmlConverters.TryAdd(typeof(TimeSpan), new Tuple<Func<object, object>, Func<object, object>>(
                o => XmlConvert.ToString((TimeSpan)o), o => XmlConvert.ToTimeSpan((string)o)));
        }

        public static void AddConverters(Type type, Func<object, object> serializeConvert, Func<object, object> desrializeConvert)
        {
            if (type == null)
                return;

            _xmlConverters.AddOrUpdate(type, new Tuple<Func<object, object>, Func<object, object>>(serializeConvert, desrializeConvert));
        }

        public static object ToString(object o)
        {
            if (o == null)
                return o;

            Tuple<Func<object, object>, Func<object, object>> tuple = null;
            Type objType = o.GetType();
            if (_xmlConverters.TryGetValue(objType, out tuple))
            {
                if (tuple != null && tuple.Item1 != null)
                {
                    return tuple.Item1(o);
                }
            }

            return o;
        }

        public static bool HasConverters(Type type)
        {
            Tuple<Func<object, object>, Func<object, object>> tuple = null;
            if (_xmlConverters.TryGetValue(type, out tuple))
            {
                if (tuple != null)
                    return true;
            }    
            return false;
        }

        public static object ToObject(Type type, object o)
        {
            if (type == null || o == null)
                return o;

            Tuple<Func<object, object>, Func<object, object>> tuple = null;
            Type objType = type;
            if (_xmlConverters.TryGetValue(objType, out tuple))
            {
                if (tuple != null && tuple.Item2 != null)
                {
                    return tuple.Item2(o);
                }
            }

            return o;
        }
    }
}
