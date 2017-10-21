using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoDynamicObjectEqualityComparer : IEqualityComparer<ChoDynamicObject>
    {
        public static readonly ChoDynamicObjectEqualityComparer Default = new ChoDynamicObjectEqualityComparer((string)null);

        private string[] _fields;
        public ChoDynamicObjectEqualityComparer(string csvFieldNames)
        {
            _fields = csvFieldNames.SplitNTrim();
        }

        public ChoDynamicObjectEqualityComparer(string[] fields)
        {
            _fields = fields;
        }

        public bool Equals(ChoDynamicObject x, ChoDynamicObject y)
        {
            IDictionary<string, object> x1 = x as IDictionary<string, object>;
            IDictionary<string, object> y1 = y as IDictionary<string, object>;

            if (_fields.IsNullOrEmpty())
                return x1.Count == y1.Count && !x1.Except(y1).Any();
            else
            {
                foreach (var field in _fields)
                {
                    if (!x1.ContainsKey(field) || !y1.ContainsKey(field))
                        return false;

                    if (Object.Equals(x1[field], y1[field]))
                        continue;
                    else
                        return false;
                }

                return true;
            }
        }

        public int GetHashCode(ChoDynamicObject obj)
        {
            IDictionary<string, object> obj1 = obj as IDictionary<string, object>;
            int hashCode = 0;
            if (_fields.IsNullOrEmpty())
            {
                foreach (var value in obj1.Values)
                {
                    if (value == null)
                        continue;

                    hashCode ^= value.GetHashCode();
                }
            }
            else
            {
                foreach (var field in _fields)
                {
                    if (!obj1.ContainsKey(field))
                        continue;

                    if (obj1[field] == null)
                        continue;

                    hashCode ^= obj1[field].GetHashCode();
                }

            }
            return hashCode;
        }
    }

}
