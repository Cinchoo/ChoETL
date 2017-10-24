using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    public class ChoLamdaEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambdaComparer;
        private readonly Func<T, int> _lambdaHash;

        public ChoLamdaEqualityComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        {
        }

        public ChoLamdaEqualityComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");
            if (lambdaHash == null)
                throw new ArgumentNullException("lambdaHash");

            _lambdaComparer = lambdaComparer;
            _lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return _lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _lambdaHash(obj);
        }
    }

    public class ChoLamdaComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _lambdaComparer;

        public ChoLamdaComparer(Func<T, T, int> lambdaComparer)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");

            _lambdaComparer = lambdaComparer;
        }

        public int Compare(T x, T y)
        {
            return _lambdaComparer(x, y);
        }
    }

    public class ChoComparer<T, TKey> : IComparer<T>, IEqualityComparer<T>
    {
        private readonly Expression<Func<T, TKey>> _KeyExpr;
        private readonly Func<T, TKey> _CompiledFunc;
        // Constructor
        public ChoComparer(Expression<Func<T, TKey>> getKey)
        {
            _KeyExpr = getKey;
            _CompiledFunc = _KeyExpr.Compile();
        }

        public int Compare(T obj1, T obj2)
        {
            return Comparer<TKey>.Default.Compare(_CompiledFunc(obj1), _CompiledFunc(obj2));
        }

        public bool Equals(T obj1, T obj2)
        {
            return EqualityComparer<TKey>.Default.Equals(_CompiledFunc(obj1), _CompiledFunc(obj2));
        }

        public int GetHashCode(T obj)
        {
            return EqualityComparer<TKey>.Default.GetHashCode(_CompiledFunc(obj));
        }
    }
}
