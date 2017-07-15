using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChoETL
{
    [Serializable]
    [XmlType(TypeName = "KeyValuePair")]
    public struct ChoKeyValuePair<TKey, TValue>
    {
        public ChoKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            Key = kvp.Key;
            Value = kvp.Value;
        }
        public ChoKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; set; }

        public TValue Value { get; set; }
    }
}
